/*
 * GridSpace.cs
 * Scott Duman
 * Chase Kurkowski
 * The script that holds all the base functionality for the gridspaces.
 */
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

///<summary>Enum for indicationg the Direction of neighboring Grid Spaces to a Grid Space.</summary>
[System.Serializable]
public enum Direction : int
{
    NorthEast,
    NorthWest,
    West,
    SouthWest,
    SouthEast,
    East,
    Teleport,
}

[System.Serializable]
public abstract class GridSpace : MonoBehaviour
{
    public static UnityEvent ResetDamageNumbers = new UnityEvent();

    #region variables
    private bool awaitingToolTip = false;
    protected bool initialized = false;
    
    [SerializeField]
    protected int movementCost = 1;
    public int MovementCost
    {
        get { return movementCost; }
    }
    protected GridObject gridObject;
    public GridObject GridObject
    {
        get { return gridObject; }
    }
    protected TileButton gridButton;
    public TileButton GridButton
    {
        get { return gridButton; }
    }
    protected PathNode pathNode;
    public PathNode PathNode 
    {
        get { return pathNode; }
    }

    protected Vector2Int gridPosition;
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
    }

    protected Dictionary<Direction, GridSpace> neighbors;
    public TileEvent onEnter, onExit, onActivate;
    protected MouseTriggers mouseTriggers;

    #endregion

    #region StaticFunctions
    public static Direction GetOppositeDirection(Direction direction)
    {
        int dir = (int)direction;
        if (dir < 3)
        {
            return (Direction)(dir + 3);
        }
        else if (dir < 6)
        {
            return (Direction)(dir - 3);
        }
        else
        {
            return direction;
        }
    }

    public static Direction FindDirection(Vector3 direction)
    {
        direction.y = 0;
        direction = Vector3.Normalize(direction);

        float angle = Vector3.SignedAngle(direction, Vector3.right, Vector3.up);

        if (angle <= -150 || angle > 150)
        {
            return Direction.West;
        }
        else if (angle <= -90)
        {
            return Direction.SouthWest;
        }
        else if (angle <= -30)
        {
            return Direction.SouthEast;
        }
        else if (angle <= 30)
        {
            return Direction.East;
        }
        else if (angle <= 90)
        {
            return Direction.NorthEast;
        }
        else
        {
            return Direction.NorthWest;
        }
    }
    #endregion

    public virtual void Initalize(GameObject button, PathNode node, Vector2Int gridPos)
    {
        if (!initialized)
        {
            gridButton = button.GetComponent<TileButton>();
            UpdateGridButtonPosition();
            gridButton.Deactivate();

            onEnter = new TileEvent();
            onExit = new TileEvent();
            onActivate = new TileEvent();

            neighbors = new Dictionary<Direction, GridSpace>();
            pathNode = node;
            gridPosition = gridPos;
            SetUpToolTips();

            initialized = true;
        }
    }

    protected virtual void SetUpToolTips()
    {
        mouseTriggers = transform.GetChild(0).GetChild(0).GetComponent<MouseTriggers>();
        SetToolTipEvents();
    }

    protected void SetWallTriggers()
    {
        if (IsObjectType(ObjectType.Wall))
        {
            mouseTriggers = gridObject.transform.GetChild(0).GetChild(0).GetComponent<MouseTriggers>();
            SetToolTipEvents();
        }
    }

    protected void SetToolTipEvents()
    {
        if (mouseTriggers != null)
        {
            mouseTriggers.onMouseEnter.AddListener(ShowToolTip);
            mouseTriggers.onMouseExit.AddListener(HideToolTip);
        }
    }


    #region NeighborFunctions
    public Dictionary<Direction, GridSpace> GetNeighborhood()
    {
        Dictionary<Direction, GridSpace> neighborhood = new Dictionary<Direction, GridSpace>();
        
        foreach (Direction dir in neighbors.Keys)
        {
            neighborhood.Add(dir, neighbors[dir]);
        }

        return neighborhood;
    }

    public List<GridSpace> GetAllNeighbors(bool includeTeleport = true)
    {
        List<GridSpace> neighborhood = new List<GridSpace>();

        if (includeTeleport)
        {
            neighborhood.AddRange(neighbors.Values);
        }
        else
        {
            foreach(Direction dir in neighbors.Keys)
            {
                if (dir != Direction.Teleport)
                {
                    neighborhood.Add(neighbors[dir]);
                }
            }
        }

        return neighborhood;
    }

    ///<summary>Gets a list of the directions of the Neighbors of this tile</summary>
    public List<Direction> GetAllDirections(bool includeTeleport = true)
    {
        List<Direction> neighborhood = new List<Direction>();

        if (includeTeleport)
        {
            neighborhood.AddRange(neighbors.Keys);
        }
        else
        {
            foreach(Direction dir in neighbors.Keys)
            {
                if (dir != Direction.Teleport)
                {
                    neighborhood.Add(dir);
                }
            }
        }

        return neighborhood;
    }

    public bool TryGetNeighbor(out GridSpace space, Direction direction)
    {
        if (HasDirection(direction))
        {
            space = neighbors[direction];
            return true;
        }

        space = null;
        return false;
    }

    public GridSpace GetNeighbor(Direction direction)
    {
        if (neighbors.ContainsKey(direction))
        {
            return neighbors[direction];
        }

        return null;
    }

    public bool HasDirection(Direction dir)
    {
        return neighbors.ContainsKey(dir);
    }

    public Direction GetDirection(GridSpace neighbor)
    {
        foreach(Direction dir in GetAllDirections())
        {
            if (neighbors[dir].Equals(neighbor))
            {
                return dir;
            }
        }

        Debug.LogError("ERROR: Grid Space, " + gridPosition + ", doesn't have the given Grid Space, " + neighbor.gridPosition + ", as a neighbor!");
        return Direction.Teleport;
    }

    public bool TryGetDirection(out Direction direction, GridSpace neighbor)
    {
        foreach(Direction dir in GetAllDirections())
        {
            if (neighbors[dir].Equals(neighbor))
            {
                direction = dir;
                return true;
            }
        }

        direction = Direction.Teleport;
        return false;
    }

    public void UpdateNeighbor(Direction direction, GridSpace neighbor)
    {
        if (neighbors.ContainsKey(direction))
        {
            if (neighbor == null)
            {
                neighbors.Remove(direction);
                return;
            }
            neighbors[direction] = neighbor;
        }
        else
        {
            //Do not add a neighbor that is null
            if (neighbor == null)
            {
                return;
            }

            neighbors.Add(direction, neighbor);
        }

        //Find out the opposite direction of the neighbor
        Direction opposite = GetOppositeDirection(direction);
        //Checks if the new neighbor has this grid space set as a neighbor 
        if (!neighbor.HasDirection(opposite) || !neighbor.neighbors[opposite].Equals(this))
        {
            //Sets grid space as a neighbor of its neighbor 
            neighbor.UpdateNeighbor(opposite, this);
        }
    }
    #endregion

    /// <summary>Sets the grid space as occupied with the given character grid object.</summary>
    /// <param name="newObject">Parameter for the grid object that will occupy the grid space.</param>
    /// <returns>Returns true if the grid object is not null and returns false if it is null.</returns>
    public bool OccupySpace(GridObject newObject)
    {
        if (newObject != null)
        {
            // Remove current occupant if there is one
            if (HasGridObject())
            {
                UnoccupySpace();
            }

            gridObject = newObject;
            if (IsObjectType(ObjectType.Wall))
            {
                SetWallTriggers();
                UpdateGridButtonPosition();
            }
            if (GameToolTipManager.Instance.IsActive() && GameToolTipManager.Instance.IsShowingToolTip(this))
            {
                GameToolTipManager.Instance.Deactivate();
                GameToolTipManager.Instance.Activate(this);
            }

            // Call on Enter
            onEnter?.Invoke(this);
            return true;
        }
        Debug.LogWarning("WARNING: Grid Object Passed to Grid Space is null!");
        return false;
    }

    /// <summary>Unoccupies grid object from the grid space.</summary>
    public void UnoccupySpace()
    {
        bool updateButton = false;
        if (IsObjectType(ObjectType.Wall))
        {
            SetUpToolTips();
            updateButton = true;
        }

        //Call on Exit
        onExit?.Invoke(this);

        gridObject = null;
        if (GameToolTipManager.Instance.IsActive() && GameToolTipManager.Instance.IsShowingToolTip(this))
        {
            GameToolTipManager.Instance.Deactivate();
            GameToolTipManager.Instance.Activate(this);
        }
        if (updateButton)
        {
            UpdateGridButtonPosition();
        }
    }

    /// <summary>Checks whether or not the grid space can be occupied.</summary>
    /// <returns>Returns true if grid space is not inaccesable and not occupied and false if not.</returns>
    public virtual bool CanOccupy()
    {
        return !IsOccupied();
    }

    /// <summary>Checks whether or not the grid space is occupied.</summary>
    /// <returns>Returns true if grid space is occupied and false if not.</returns>
    public virtual bool IsOccupied()
    {
        return (gridObject != null && gridObject.OccupysSpace());
    }

    public virtual int OnTileEnterDamage()
    {
        return 0;
    }

    public virtual void OnTileEnter()
    {
        // Override Me
    }

    public virtual void ActivateTile()
    {
        // Override Me
        onActivate?.Invoke(this);
    }

    /// <summary>Checks whether or not the grid space is occupied by a grid Object.</summary>
    /// <returns>Returns true if grid space has a gridObject and false if not.</returns>
    public bool HasGridObject()
    {
        return gridObject != null;
    }

    public bool IsTarget()
    {
        return HasGridObject() && gridObject.IsTarget();
    }

    public bool IsObjectType(ObjectType type)
    {
        return HasGridObject() && gridObject.GetObjectType() == type;
    }

    public bool IsObjectTypes(ObjectType[] types)
    {
        if (HasGridObject())
        {
            foreach(ObjectType type in types)
            {
                if (type == gridObject.GetObjectType())
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Activates the grid space's button.</summary>
    /// <returns>Returns true if the grid space's button is active and returns false if not.</returns>
    public bool SetMovementPath(List<GridSpace> path)
    {
        //Checks if space can be occupied and if so make the button inactive
        if (CanOccupy())
        {
            gridButton.SetPath(path);
            return true;
        }
        else if (IsObjectType(ObjectType.Unit) && gridObject.TryGetComponent(out Character character))
        {
            gridButton.UpdateState(ButtonState.Filled);
            return false;
        }

        gridButton.UpdateState(ButtonState.Blocked);
        return false; 
    }

    public void UpdatePath(List<GridSpace> path)
    {
        gridButton.SetPath(path);
    }

    /// <summary>Sets the grid space up to be .</summary>
    /// <param name="attackPanel">Parameter for the Attack Panel that will need to be informed when a target is picked.</param>
    /// <returns>Returns true if grid space is has an object occupying it that is targetable.</returns>
    public void TargetedSpace(Character player, Attack attack, UnityAction onEnter = null, UnityAction onExit = null)
    {

        UnityAction onTileClick = delegate
        {
            TurnManager.Instance.ActionPanel.Deactivate();
            GameToolTipManager.Instance.ShowAttackRange(true);

            GridSpace.ResetDamageNumbers.Invoke();
            player.Attack(attack, this);
        };

        UnityAction onTileEnter = delegate
        {
            //gridButton.material = targetHover;
            if (onEnter != null)
            {
                onEnter.Invoke();
            }

            if(attack.GetDamage() >= 0 && IsObjectType(ObjectType.Unit))
            {
                if (attack.GetName() == "Swing")
                {
                    int moveDist = 0;
                    if (attack.GetType() == typeof(Knockback))
                    {
                        moveDist = ((Knockback)attack).moveDistance;
                    }

                    if (moveDist > 0 && GridObject.WillHitObstacle(out GridObject obstacle, player.GetGridSpace(), moveDist))
                    {
                        if (IsObjectType(ObjectType.Unit))
                        {
                            GetCharacter().PassDamageNumber(attack.GetDamage() + Knockbackable.KnockbackDamage);
                        }

                        if (obstacle.GetObjectType() == ObjectType.Unit)
                        {
                            obstacle.GetComponent<GameCharacter>().PassDamageNumber(Knockbackable.KnockbackDamage);
                        }

                        return;
                    }
                }

                GetCharacter().PassDamageNumber(attack.GetDamage());
            }
        };

        UnityAction onTileExit = delegate
        {
            if (onExit != null)
            {
                onExit.Invoke();
            }

            ResetDamageNumbers.Invoke();
        };

        gridButton.UpdateEvents(onTileClick, onTileEnter, onTileExit);
        gridButton.UpdateState(ButtonState.Targeted);
    }

    public void AudienceTargetedSpace(Audience currentAudience, List<GridSpace> damageSpaces, bool middleTileMat = false, UnityAction onEnter = null, UnityAction onExit = null)
    {
        UnityAction onTileClick = delegate
        {
            currentAudience.CheckAbility(this);

            GridManager.DeactivateTiles.Invoke();

            GameToolTipManager.Instance.ShowAttackRange(true);
            ResetDamageNumbers.Invoke();
        };

        UnityAction onTileEnter = delegate
        {
            if(middleTileMat)
            {
                gridButton.material = TileButton.moveHover;
            }

            int audienceDamage = AudienceManager.Instance.GetAudienceDamage(currentAudience.CurrentAudienceType);
            if(audienceDamage > 0 && IsObjectType(ObjectType.Unit))
            {
                GetCharacter().PassDamageNumber(audienceDamage);
            }

            foreach(GridSpace tile in damageSpaces)
            {
                if(!tile.Equals(this))
                {
                    tile.gridButton.UpdateState(ButtonState.AreaOfEffect);

                    int audienceRowDamage = AudienceManager.Instance.GetAudienceDamage(currentAudience.CurrentAudienceType, true);

                    if (audienceRowDamage >= 0 && tile.TryGetCharacter(out GameCharacter unit))
                    {
                        if (currentAudience.CurrentAudienceType == AudienceType.Star && !tile.HasPlayer())
                        {
                            if (unit.GridUnit.WillHitObstacle(out GridObject obstacle, this, 1))
                            {
                                unit.PassDamageNumber(audienceRowDamage + Knockbackable.KnockbackDamage);

                                if (obstacle is GridUnit obstacleUnit)
                                {
                                    obstacleUnit.Character.PassDamageNumber(Knockbackable.KnockbackDamage);
                                }

                                continue;
                            }
                        }

                        unit.PassDamageNumber(audienceRowDamage);
                    }
                }
            }

            if (onEnter != null)
            {
                onEnter.Invoke();
            }
            gridButton.UpdateState(ButtonState.AreaOfEffect);
        };

        UnityAction onTileExit = delegate
        {
            gridButton.UpdateState(ButtonState.Targeted);
            // Debug.Log("Grid Button Exit");

            GridSpace.ResetDamageNumbers.Invoke();

            foreach(GridSpace tile in damageSpaces)
            {
                if (tile != this)
                {
                    tile.gridButton.UpdateState();
                }
            }

            if (onExit != null)
            {
                onExit.Invoke();
            }
        };

        gridButton.UpdateEvents(onTileClick, onTileEnter, onTileExit);
        gridButton.UpdateState(ButtonState.Targeted);
    }

    public void GrenadeJumpTargeting(Character player, bool middleTileMat = false, bool meteor = false, List<GridSpace> damageSpaces = null, UnityAction onClick = null, UnityAction onEnter = null, UnityAction onExit = null)
    {
        if (damageSpaces == null)
        {
            damageSpaces = new List<GridSpace>();
        }

        UnityAction onTileClick = delegate
        {
            UnityAction FinishLanding = delegate
            {
                if (IsObjectType(ObjectType.Unit))
                {
                    ActivateTile();
                }

                TurnManager.Instance.ActionPanel.Activate(TurnManager.Instance.GetCharacter().GetActions());
                if (TurnManager.Instance.GetCharacter().GetGrenJumped())
                {
                    TurnManager.Instance.GetCharacter().SetGrenJumped(false);
                }
            };
            
            UnityAction onLanding = delegate
            {
                if (onClick != null)
                {
                    onClick.Invoke();
                }

                int damage = 2;
                List<Knockbackable> knockbackTargets = new List<Knockbackable>();

                foreach (GridSpace tile in damageSpaces)
                {
                    if (tile.GetDirection(this) != Direction.Teleport && tile.IsTarget())
                    {
                        if (tile.GridObject.CanBeKnockedBack())
                        {
                            knockbackTargets.Add(tile.GridObject.GetComponent<Knockbackable>());
                        }
                        else
                        {
                            tile.GridObject.TakeDamage(damage);
                        }
                    }
                }

                if (knockbackTargets.Count > 0)
                {
                    foreach (Knockbackable subTarget in knockbackTargets)
                    {
                        subTarget.FinishedKnockback.AddListener(delegate (Knockbackable returnTarget)
                        {
                            if (knockbackTargets.Contains(returnTarget))
                            {
                                knockbackTargets.Remove(returnTarget);
                                returnTarget.FinishedKnockback.RemoveAllListeners();
                                
                                if (knockbackTargets.Count == 0)
                                {
                                    FinishLanding();
                                }
                            }
                        });
                    }

                    Knockback.KnockbackObjects(knockbackTargets, this, 1, damage);
                }
                else
                {
                    FinishLanding();
                }
            };

            GridSpace.ResetDamageNumbers.Invoke();

            player.Jump(this, onLanding);
        };

        UnityAction onTileEnter = delegate
        {
            if (!middleTileMat)
            {
                gridButton.UpdateState(ButtonState.AreaOfEffect);
            }
            else
            {
                gridButton.material = TileButton.moveHover;
            }

            foreach(GridSpace tile in damageSpaces)
            {
                if(!tile.Equals(this))
                {
                    tile.gridButton.UpdateState(ButtonState.AreaOfEffect);

                    int meteorDamage = 0;
                    if (meteor)
                    {
                        meteorDamage = 2;
                    }

                    if(tile.IsObjectType(ObjectType.Unit) && !tile.HasPlayer())
                    {
                        if (tile.GridObject.WillHitObstacle(out GridObject obstacle, this, 1))
                        {
                            tile.GetCharacter().PassDamageNumber(meteorDamage + Knockbackable.KnockbackDamage);

                            if (obstacle.GetType() == typeof(GridUnit))
                            {
                                obstacle.GetComponent<GameCharacter>().PassDamageNumber(Knockbackable.KnockbackDamage);
                            }
                        }

                        if (meteorDamage > 0)
                        {
                            tile.GetCharacter().PassDamageNumber(meteorDamage);
                        }
                    }
                }
            }

            if (onEnter != null)
            {
                onEnter.Invoke();
            }
            gridButton.UpdateState(ButtonState.AreaOfEffect);
        };

        UnityAction onTileExit = delegate
        {
            gridButton.UpdateState(ButtonState.Targeted);

            GridSpace.ResetDamageNumbers.Invoke();
            foreach (GridSpace tile in damageSpaces)
            {
                if (tile != null)
                {
                    tile.gridButton.UpdateState();
                }
            }

            if (onExit != null)
            {
                onExit.Invoke();
            }
        };

        gridButton.UpdateEvents(onTileClick, onTileEnter, onTileExit);
        gridButton.UpdateState(ButtonState.Targeted);
    }

    protected virtual void UpdateGridButtonPosition()
    {
        if (gridButton != null)
        {
            if (IsObjectType(ObjectType.Wall))
            {
                gridButton.transform.position = transform.position + (Vector3.up * 0.8f);
            }
            else
            {
                gridButton.transform.position = transform.position + (Vector3.up * 0.025f);
            }
            gridButton.transform.localEulerAngles = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("WARNING: Trying to update grid space's button when no button is set to the GridSpace!");
        }
    }

    public GridUnit GetGridUnit()
    {
        if (gridObject != null && gridObject.TryGetComponent(out GridUnit unit))
        {
            return unit;
        }
        // Debug.LogWarning("WARNING: Trying to access grid unit of a space that doesn't contain a grid unit!");
        return null;
    }

    ///<summary>Checks if the player is occupying this tile</summary>
    public bool HasPlayer()
    {
        if (TryGetCharacter(out GameCharacter player) && player is Character)
        {
            return true;
        }
        return false;
    }
    
    ///<summary>Tries to get the game character that is occupying the tile if there is one.</summary>
    ///<returns>Returns true if there is a game character occupying the tile</returns>
    public bool TryGetCharacter(out GameCharacter character)
    {
        if (gridObject != null && gridObject is GridUnit unit && unit.Character != null)
        {
            character = unit.Character;
            return true;
        }

        character = null;
        return false;
    }

    public GameCharacter GetCharacter()
    {
        if (gridObject != null && gridObject is GridUnit unit && unit.Character != null)
        {
            return unit.Character;
        }

        // Debug.LogWarning("WARNING: Trying to access game character of a grid space that is not occupied by a grid unit!");
        return null;
    }

    public bool TryGetEnemyUnit(out EnemySuperClass enemy)
    {
        if (IsObjectType(ObjectType.Unit) && gridObject.TryGetComponent(out enemy))
        {
            return true;
        }

        enemy = null;
        return false;
    }

    protected void ShowToolTip()
    {
        StartCoroutine(DelayToolTip());
    }

    private System.Collections.IEnumerator DelayToolTip(float delay = 0.5f)
    {
        awaitingToolTip = true;
        yield return new WaitForSeconds(delay);
        awaitingToolTip = false;
        GameToolTipManager.Instance.Activate(this);
    }

    protected void HideToolTip()
    {
        if (awaitingToolTip)
        {
            StopAllCoroutines();
            awaitingToolTip = false;
        }
        else if (GameToolTipManager.Instance.IsActive() && GameToolTipManager.Instance.IsShowingToolTip(this))
        {
            GameToolTipManager.Instance.Deactivate();
        }
    }

    public TileType GetTileType()
    {
        return pathNode.TileType;
    }

    public bool IsTileType(TileType type)
    {
        return pathNode.TileType == type;
    }
}