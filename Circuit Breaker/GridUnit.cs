/*
 * GridUnit.cs
 * Scott Duman
 * Chase Kurkowski
 * Class describing units that occupies the grid and all the functionality for units to act and move on that grid.
 */
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using UnityEngine;

public class GridUnit: Knockbackable
{
	#region Variables
    private GameCharacter gameCharacter;
    public GameCharacter Character
    {
        get { return gameCharacter; }
    }

    [SerializeField, Range(1, 10)]
    private float movementSpeed = 2.5f;
    bool moving = false;

    [HideInInspector]
    public UnityEvent MovementFinished; //HealTile, GuardianShieldActivate, JumpTile;

    #endregion

    private void Awake() 
    {
        MovementFinished = new UnityEvent();

        MovementFinished.AddListener(delegate
        {
            if (Character == null || Character.GetHealth() <= 0)
            {
                TurnManager.Instance.NextIndex(false);
                return;
            }
            TurnManager.Instance.NextAction();
        });

        Initalize();
    }

    public override void Initalize()
    {
        if (gameCharacter == null)
        {
            gameCharacter = GetComponent<GameCharacter>();
        }

        ActivateTile();
    }

    private IEnumerator MoveInGrid(List<GridSpace> path, int directionIndex)
    {
        if (directionIndex < 0)
        {
            FindNextDirection(path, ref directionIndex);
        }

        Vector3 forward = path[directionIndex].transform.position - transform.position;
        forward.y = 0;

        while (path.Count > 0)
        {
            Vector3 direction = path[0].transform.position - transform.position;
            direction.y = 0;

            float angle = Vector3.Angle(forward, direction);
            if (direction.magnitude <= 0.05f || angle >= 30)
            {
                UpdateGridSpace(path[0]);
                path.RemoveAt(0);

                if (path.Count == 0)
                {
                    FinishMovement();
                    break;
                }

                directionIndex--;
                if (directionIndex < 0)
                {
                    if (FindNextDirection(path, ref directionIndex) == Direction.Teleport)
                    {
                        StartCoroutine(TeleportObject(path));
                        break;
                    }
                    else
                    {
                        forward = path[directionIndex].transform.position - transform.position;
                        forward.y = 0;
                    }
                }
            }
            else
            {
                transform.position = UpdateMoveDirection(path, directionIndex);
                yield return null;
            }
        }
    }

    public void ActivateTile()
    {
        //Check if grid space is null
        gridSpace?.ActivateTile();
    }

    private IEnumerator TeleportObject(List<GridSpace> path)
    {
        //Preparing for Teleport
        Character.Animator.Play("Idle");
        yield return new WaitForSeconds(0.5f);

        //Teleport Character
        gridSpace.GetComponent<TeleportTile>().Teleport();
        path.RemoveAt(0);
        yield return new WaitForSeconds(0.5f);
        
        StartMoving(path);
    }

    private void FinishMovement()
    {
        ActivateTile();

        if (MovementFinished != null)
        {
            MovementFinished.Invoke();
        }

        Character.Animator.SetTrigger("FinishedMovement");
        Character.Animator.speed = 1;

        moving = false;
    }

    private Vector3 UpdateMoveDirection(List<GridSpace> path, int directionIndex)
    {
        Vector3 targetPos = path[directionIndex].transform.position;
        Vector3 dir = (targetPos - transform.position);
        float t = Mathf.Clamp01(movementSpeed * Time.deltaTime);
        float cutoff = 0.5f;

        if (dir.magnitude > 1)
        {
            Character.Animator.speed = 1;
            targetPos = Vector3.Normalize(dir) + transform.position;
        }
        else if (directionIndex != path.Count - 1 && dir.magnitude < cutoff)
        {
            Character.Animator.speed = cutoff;
            targetPos = (Vector3.Normalize(dir) * cutoff) + transform.position;
        }
        else
        {
            Character.Animator.speed = Mathf.Max(dir.magnitude, 0.3f);
        }

        Vector3 moveDir = Vector3.Lerp(transform.position, targetPos, t);
        return moveDir;
    }

    public void SetMovementPath(List<GridSpace> movementPath)
    {
        if (!moving)
        {
            if (movementPath != null && movementPath.Count > 0)
            {
                moving = true;
                int directionIndex = -1;

                if (FindNextDirection(movementPath, ref directionIndex) == Direction.Teleport)
                {
                    if (gridSpace.GetTileType() == TileType.Teleport)
                    {
                        StartCoroutine(TeleportObject(movementPath));
                        return;
                    }
                }

                Character.Animator.Play("Moving");
                StartCoroutine(MoveInGrid(movementPath, directionIndex));
            }
            else
            {
                Debug.LogWarning("WARNING: Passed movement path does not contain any grid spaces!");
                FinishMovement();
            }
        }
    }

    public void StartMoving(List<GridSpace> path)
    {
        if (path.Count == 0)
        {
            FinishMovement();
            return;
        }

        int directionIndex = -1;
        FindNextDirection(path, ref directionIndex);

        Character.Animator.Play("Moving");
        StartCoroutine(MoveInGrid(path, directionIndex));
    }

    public Direction FindNextDirection(List<GridSpace> path, ref int directionIndex, bool faceDirection = true)
    {
        directionIndex++;
        Direction direction = Direction.Teleport;
        
        if (directionIndex < path.Count)
        {
            direction = gridSpace.GetDirection(path[directionIndex]);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Direction nextDirection =  path[i].GetDirection(path[i + 1]);
                
                if(i + 1 >= path.Count || direction != nextDirection)
                {
                   break; 
                }
                directionIndex++;
            }
        }

        if (faceDirection && (int)direction < 6)
        {
            FaceDirection(direction);
        }

        return direction;
    }

    protected override void SetUpKnockback()
    {
        //Makes sure the knockedback Target playes the damaged Animation
        Character.TakeDamage(0);
        //Makes sure the health bar displayes the correct hp on delayed damage for knockback
        if (willDieOnKnockback)
        {
            Character.PlayDead();
        }

        //If we have the stun upgrade, apply movement reduction to the GridObject
        if (UpgradeManager.Instance.GetKBStun() && !gameCharacter.TryGetComponent(out Character c))
        {
            gameCharacter.ApplyStun(true);
        }
    }

    protected override void OnKnockbackFinished()
    {
        if (gameCharacter != null && Character.GetHealth() > 0 && Character.TryGetComponent(out EnemySuperClass enemy))
        {
            enemy.FindAttackRange();
        }
        
        //Make sure that the character dies if they are suppose to
        if (willDieOnKnockback && Character.GetHealth() > 0)
        {
            StartCoroutine(CheckDeath());
        }
    }

    private IEnumerator CheckDeath()
    {
        yield return null;
        if (willDieOnKnockback)
        {
            Character.TakeDamage(Character.GetHealth());
        }
    }

    protected override bool CanTransverse(GridSpace tile)
    {
        return !tile.PathNode.IsRemoved() || Character.GetUnitType() == UnitType.Healio;
    }

    public override bool CanBeKnockedBack()
    {
        return Character.CanBeKnockedBack();
    }

    public override void UpdateGridSpace(GridSpace newSpace)
    {
        UpdateGridSpace(newSpace, true);
    }

    public void UpdateGridSpace(GridSpace newSpace, bool activateTileOnEnter)
    {
        //Update position to match the new tile location
        Vector3 pos = newSpace.transform.position;
        pos.y = transform.position.y;
        transform.position = pos;

        //Unoccupy the current tile if occupying one
        if (gridSpace != null)
        {
            gridSpace.UnoccupySpace();
        }
        gridSpace = newSpace;

        //Collect money on the tile if it's there 
        if (gridSpace.IsObjectType(ObjectType.Money))
        {
            //Need line here to give money to the player

            gridSpace.GridObject.GetComponent<Money>().UpdateGridSpace(null);
        }

        //
        gridSpace.OccupySpace(this);

        if (activateTileOnEnter && !willDieOnKnockback)
        {
            //Checks if the enemy will die from any damage on entering a tile while being knocked back, mark the object for death
            if (knockedback && Character.GetHealth() <= gridSpace.OnTileEnterDamage())
            {
                willDieOnKnockback = true;
                FinishedKnockback.AddListener(delegate
                {
                    Character.TakeDamage(Character.GetHealth());
                });

                Character.PlayDead();
            }
            else
            {
                gridSpace.OnTileEnter();
            }
        }

        //CheckDecoyTrap(newSpace);
    }

    public void SwapGridSpace(GridUnit allyToSwap)
    {
        if (allyToSwap == null)
        {
            Debug.LogWarning("WARNING: Tried to swap places with Unit that did not exist!");
            return;
        }

        Vector3 originalTransform = transform.position;
        Vector3 pos = allyToSwap.transform.position;

        if (allyToSwap.gridSpace != null)
        {
            pos = allyToSwap.gridSpace.transform.position;
        }

        pos.y = transform.position.y;
        transform.position = pos;

        pos = originalTransform;
        pos.y = allyToSwap.transform.position.y;
        allyToSwap.transform.position = pos;

        GridSpace currentSpace = gridSpace;
        GridSpace allySpace = allyToSwap.gridSpace;

        currentSpace.UnoccupySpace();
        allySpace.UnoccupySpace();

        currentSpace.OccupySpace(allyToSwap);
        allySpace.OccupySpace(this);

        if (allyToSwap.knockedback)
        {
            allyToSwap.StopAllCoroutines();
        }

        gridSpace = allySpace;
        allyToSwap.gridSpace = currentSpace;

        //Activate the tile on Enter in case its a lava tile
        gridSpace.OnTileEnter();
    }

    public bool IsMoving()
    {
        return moving;
    }

    public void OnDeath()
    {
        KnockbackOnDeath();

        if (moving && MovementFinished != null)
        {
            MovementFinished.Invoke();
        }

        if (gridSpace != null)
        {
            gridSpace.UnoccupySpace();
            gridSpace = null;
        }
    }

    public override ObjectType GetObjectType()
    {
        return ObjectType.Unit;
    }

    public override void TakeDamage(int damage)
    {
        gameCharacter.TakeDamage(Mathf.Abs(damage), "Damaged");
    }
}