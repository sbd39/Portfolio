/*
 * PathNode.cs
 * Scott Duman
 * A script that holds values for searching and pathing.
 */
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNode: System.IComparable<PathNode>
{
	public bool visited = false;

	///<summary>Estimated distance between Path Node and target</summary>
	public float hCost = 0;

	///<summary>Movement cost to get to Path Node</summary>
	public float gCost = 0;

    public float fCost
	{
		get { return hCost + gCost; }
	}

	public PathNode parent = null;

	[SerializeField]
	private GridSpace gridSpace;
	public GridSpace GridSpace 
	{
		get { return gridSpace; }
	}

    private TileType tileType;
    public TileType TileType
    {
        get { return tileType; }
    }

    public Vector3 Position
	{
		get { return gridSpace.transform.position; }
	}

	public PathNode(GridSpace space, TileType type)
	{
		gridSpace = space;
        tileType = type;
	}

	public bool IsOccupied()
	{
		return GridSpace.IsOccupied();
	}

	public bool CanOccupy()
	{
		return GridSpace.CanOccupy();
	}

	public bool IsTarget()
	{
		return GridSpace.IsTarget();
	}

    public bool HasGridObject()
    {
        return GridSpace.HasGridObject();
    }

    public bool IsObjectType(ObjectType type)
    {
        return GridSpace.IsObjectType(type);
    }

	public bool IsObjectTypes(ObjectType[] types)
	{
		return GridSpace.IsObjectTypes(types);
	}

	public bool IsRemoved()
	{
		return tileType == TileType.Removed;
	}

	public bool IsLava()
	{
		return tileType == TileType.Lava;
	}

	public bool IsUnbreakable()
	{
		return tileType == TileType.UnbreakableWall;
	}

	public bool IsWall()
	{
		return IsObjectType(ObjectType.Wall);
	}

	public int GetMovementCost()
	{
		return GridSpace.MovementCost;
	}
	
	///<summary>Gets All Neighboring path nodes for this path node</summary>
	///<param="includeTeleport">Determines whether or not a neibor connected by a teleporter will be included in the list</param>
	public List<PathNode> GetAllNeighbors(bool includeTeleport = true)
	{
		List<PathNode> neighborhood = new List<PathNode>();

		foreach(GridSpace space in gridSpace.GetAllNeighbors(includeTeleport))
		{
			neighborhood.Add(space.PathNode);
		}

		return neighborhood;
	}

	///<summary>Gets the neighboring path node in the specificed direction</summary>
	///<param="direction">The direction the desired Neighbor is in</param>
	public PathNode GetNeighbor(Direction direction)
	{
		if (gridSpace.TryGetNeighbor(out GridSpace space, direction) && space != null)
		{
			return space.PathNode;
		}

		return null;
	}

	///<summary>Tries to get the neighboring path node in the specificed direction</summary>
	///<param="pathNode">The path node that will be set to the neighboring path Node</param>
	///<param="direction">The direction the desired Neighbor is in</param>
	public bool TryGetNeighbor(out PathNode pathNode, Direction direction)
	{
		if (gridSpace.TryGetNeighbor(out GridSpace space, direction) && space != null)
		{
			pathNode = space.PathNode;
			return true;
		}

		pathNode = null;
		return false;
	}

    public void ResetNode()
	{
		parent = null;
		visited = false;
		hCost = 0;
		gCost = 0;
	}

	public int CompareTo(PathNode node)
	{
		//Compare fCosts
		if (fCost < node.fCost)
		{
			return -1;
		}
		else if (fCost > node.fCost)
		{
			return 1;
		}

		//Compare hCosts
		if (hCost < node.hCost)
		{
			return -1;
		}
		else if (hCost > node.hCost)
		{
			return 1;
		}

		//See if nodes have parents and if one doesn't that one goes before the other
		if (parent != null && node.parent == null)
		{
			return 1;
		}
		if (node.parent != null && parent == null)
		{
			return -1;
		}

		//Compare Parent's fCost
		if (parent.fCost < node.parent.fCost)
		{
			return -1;
		}
		else if (parent.fCost > node.parent.fCost)
		{
			return 1;
		}
		
		//Compare Parent's hCost
		if (parent.hCost < node.parent.hCost)
		{
			return -1;
		}
		else if (parent.hCost > node.parent.hCost)
		{
			return 1;
		}

		return 0;
	}
}
