/*
 * Knockbackable.cs
 * Scott Duman
 * Abstract class that makes a grid object able to be knockedback.
 */
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public abstract class Knockbackable : GridObject
{
    #region Variables
    public static readonly int KnockbackDamage = 4;

    [Header("Knockback Variables")]
    [SerializeField, Range(0.25f, 5)]
    protected float knockbackSpeed = 1.5f;

    [HideInInspector]
    public KnockedBackEvent OnKncokback = new KnockedBackEvent(), FinishedKnockback = new KnockedBackEvent(), OnObstacleHit = new KnockedBackEvent();
    protected bool willDieOnKnockback = false, knockedback = false;
    #endregion

    public void KnockbackObject(GridSpace attackerPosition, int distance = 1, bool willKill = false)
    {
        //Find Direction to be knocked back
        Direction pushDirection = attackerPosition.GetDirection(gridSpace);
        //Check if we found a direction and if not return false
        if (pushDirection == Direction.Teleport || !CanBeKnockedBack())
        {
            return;
        }

        //Find Path to be knocked back
        List<GridSpace> path = new List<GridSpace>();
        GridSpace currentSpace = gridSpace;

        bool nextSpaceOccupied = false;
        GridObject obstacle = null;

        knockedback = true;
        willDieOnKnockback = willKill;
        SetUpKnockback();

        for (int i = distance; i > 0; i--)
        {
            //Check if grid space has a neighbor in the desired direction
            if (currentSpace.TryGetNeighbor(out GridSpace nextSpace, pushDirection))
            {
                //Break out of loop if the tile doesn't exist
                if (nextSpace == null)
                {
                    break;
                }
                else
                {
                    if(GetObjectType() == ObjectType.Unit && nextSpace.PathNode.IsLava() && TurnManager.Instance.GetSideOneTurn())
                    {
                        AudienceManager.Instance.UpdateAudienceBar(AudienceManager.Instance.actions["KnockOntoFire"]);
                    }

                    if (nextSpace.CanOccupy() && CanTransverse(nextSpace))
                    {
                        path.Add(nextSpace);
                        currentSpace = nextSpace;
                    }
                    else
                    {
                        nextSpaceOccupied = true;
                        if (nextSpace.HasGridObject())
                        {
                            switch (nextSpace.GridObject.GetObjectType())
                            {
                                case ObjectType.Money: 
                                {
                                    nextSpaceOccupied = false;
                                    break;
                                }
                                default:
                                {
                                    obstacle = nextSpace.GridObject;
                                    break;
                                }
                            }
                        }

                        if (nextSpaceOccupied)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        switch (GetObjectType())
        {
            case ObjectType.Unit:
            {
                FaceTarget(attackerPosition.transform.position, 0.25f);
                break;
            }
        }

        if (path.Count > 0 || obstacle != null)
        {
            OnKncokback?.Invoke(this);

            StartCoroutine(KnockedBack(path, obstacle));
        }
        else
        {
            StartCoroutine(DelayAction());
        }
    }

    private IEnumerator DelayAction()
    {
        yield return null;
        knockedback = false;
        OnKnockbackFinished();
        FinishedKnockback.Invoke(this);
    }

    public override bool WillHitObstacle(GridSpace attackTile, int distance)
    {
        //Find Direction to be knocked back
        Direction pushDirection = attackTile.GetDirection(gridSpace);
        //Check if we found a direction and if not return false
        if (pushDirection == Direction.Teleport || !CanBeKnockedBack())
        {
            return false;
        }

        GridSpace currentSpace = gridSpace;
        for (int i = distance; i > 0; i--)
        {
            //Check if grid space has a neighbor in the desired direction
            if (currentSpace.TryGetNeighbor(out GridSpace nextSpace, pushDirection))
            {
                //Break out of loop if the tile doesn't exist
                if (nextSpace == null)
                {
                    break;
                }
                else
                {
                    if (nextSpace.CanOccupy() && CanTransverse(nextSpace))
                    {
                        currentSpace = nextSpace;
                    }
                    else
                    {
                        if (nextSpace.HasGridObject() && nextSpace.GridObject.IsTarget())
                        {
                            return true;
                        }
                        break;
                    }
                }
            }
            else
            {
                break;
            }
        }

        return false;
    }

    public override bool WillHitObstacle(out GridObject obstacle, GridSpace attackTile, int distance)
    {
        //Find Direction to be knocked back
        Direction pushDirection = attackTile.GetDirection(gridSpace);
        //Check if we found a direction and if not return false
        if (pushDirection == Direction.Teleport || !CanBeKnockedBack())
        {
            obstacle = null;
            return false;
        }

        GridSpace currentSpace = gridSpace;
        for (int i = distance; i > 0; i--)
        {
            //Check if grid space has a neighbor in the desired direction
            if (currentSpace.TryGetNeighbor(out GridSpace nextSpace, pushDirection))
            {
                //Break out of loop if the tile doesn't exist
                if (nextSpace == null)
                {
                    break;
                }
                else
                {
                    if (nextSpace.CanOccupy() && CanTransverse(nextSpace))
                    {
                        currentSpace = nextSpace;
                    }
                    else
                    {
                        if (nextSpace.HasGridObject() && nextSpace.GridObject.IsTarget())
                        {
                            obstacle = nextSpace.GridObject;
                            return true;
                        }
                        break;
                    }
                }
            }
            else
            {
                break;
            }
        }

        obstacle = null;
        return false;
    }

    protected virtual void SetUpKnockback()
    {
        //Override Me
    }

    protected virtual void OnKnockbackFinished()
    {
        //Override Me
    }

    protected virtual bool CanTransverse(GridSpace tile)
    {
        return !tile.PathNode.IsRemoved();
    }

    private IEnumerator KnockedBack(List<GridSpace> path, GridObject obstacle)
    {
        //Set up speed and last position
        Vector3 last = obstacle != null ? obstacle.transform.position : path[path.Count - 1].transform.position;
        float speed = obstacle != null ? knockbackSpeed : knockbackSpeed * 2;

        // Find the direction the object will be moving in
        Vector3 forward = last - transform.position;
        forward.y = 0;

        //Setup destination 
        Vector3 destination = obstacle != null ? forward + (forward.normalized) : forward;
        destination += transform.position;

        bool hitObstacle = false;
        Vector3 target = transform.position;

        if (path.Count > 0)
        {
            target = path[0].transform.position;
        }
        else
        {
            hitObstacle = true;
            Vector3 dir = (obstacle.transform.position - gridSpace.transform.position);

            target = (dir/2) + gridSpace.transform.position;
        }

        while (true)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0;

            float angle = Vector3.Angle(direction, forward);
            if (direction.magnitude <= 0.05f || angle >= 30)
            {
                if (path.Count > 0)
                {
                    UpdateGridSpace(path[0]);
                    path.RemoveAt(0);
                }

                if (path.Count == 0)
                {
                    if (obstacle == null)
                    {
                        if (gridSpace != null)
                        {
                            transform.position = gridSpace.transform.position;

                            if (GetObjectType() == ObjectType.Unit && TryGetComponent(out EnemySuperClass enemy))
                            {
                                enemy.FindAttackRange();
                            }
                        }

                        knockedback = false;
                        OnKnockbackFinished();
                        if (FinishedKnockback != null)
                        {
                            FinishedKnockback.Invoke(this);
                        }
                        break;
                    }
                    else
                    {
                        if (!hitObstacle)
                        {
                            hitObstacle = true;
                            Vector3 dir = (obstacle.transform.position - gridSpace.transform.position);

                            target = (dir/2) + gridSpace.transform.position;
                            destination = dir* 2 + transform.position;
                        }
                        else if (obstacle != null)
                        {
                            HitObstacle(obstacle);
                            obstacle = null;
                            
                            if (GetObjectType() == ObjectType.Unit && GetComponent<GameCharacter>().GetHealth() <= 0)
                            {
                                break;
                            }

                            target = gridSpace.transform.position;
                            forward = gridSpace.transform.position - transform.position;
                            destination = gridSpace.transform.position;
                        }
                    }
                }
                else
                {
                    target = path[0].transform.position;
                }
            }
            else
            {
                float moveDelta = Mathf.Clamp01(speed * Time.deltaTime);
                Vector3 movement = Vector3.Lerp(transform.position, destination, moveDelta);
                transform.position = movement;
                yield return null;
            }
        }
    }

    protected virtual void HitObstacle(GridObject obstacle)
    {
        //Obstacle takes damage
        switch (obstacle.GetObjectType())
        {
            case ObjectType.Unit:
            {
                FaceTarget(gridSpace.transform.position, 0.25f);
                break;
            }
        }
        obstacle.TakeDamage(KnockbackDamage);

        //Invoke On Obstacle Hit
        OnObstacleHit?.Invoke(this);

        //If the object hasn't died then deal damage to itself
        if (gameObject.activeSelf)
        {
            switch (GetObjectType())
            {
                case ObjectType.Unit:
                {
                    FaceTarget(obstacle.transform.position, 0.25f);
                    break;
                }
            }
            TakeDamage(KnockbackDamage);
        }
    }

    public override bool CanBeKnockedBack()
    {
        return true;
    }

    public void KnockbackOnDeath()
    {
        StopAllCoroutines();
        willDieOnKnockback = false;
        knockedback = false;

        if (OnObstacleHit != null)
        {
            OnObstacleHit.Invoke(this);
        }
        if (FinishedKnockback != null)
        {
            FinishedKnockback.Invoke(this);
        }
    }

    public bool WillDieOnKnockback()
    {
        return willDieOnKnockback;
    }
}