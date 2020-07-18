/*
 * GridInfo.cs
 * Scott Duman
 * Script that holds elements of the Grid Info for passing.
 */
using System.Collections.Generic;
using UnityEngine;

public struct GridInfo
{
	public string name;
	public GridType type;
	public int[] parameters;
	public TileInfo[] tiles;
	public UnitInfo[] spawns;

	public LevelObjectInfo[] levelObjects;

	/// <summary>Creates a new Grid Info struct that contains all the data nessicary to build a specific grid.</summary>
    /// <param name="type">Type of the grid being created</param>
	/// <param name="parameters">The size parameters for the grid</param>
	/// <param name="tiles">List of all tile info to be stored</param>
	/// <param name="spawns">List of all game character info to be stored</param>
	/// <param name="levelObjects">List of all harzard info to be stored</param>
	public GridInfo(string name, GridType type, int[] parameters, List<TileInfo> tiles, List<UnitInfo> spawns, List<LevelObjectInfo> levelObjects)
	{
		this.name = name;
		this.type = type;
		this.parameters = parameters;
		this.tiles = tiles.ToArray();
		this.spawns = spawns.ToArray();
		this.levelObjects = levelObjects.ToArray();
	}

	public List<TileInfo> GetTiles()
	{
		List<TileInfo> tileList = new List<TileInfo>();
		if (tiles != null && tiles.Length > 0)
		{
			tileList.AddRange(tiles);
		}
		return tileList;
	}

	public List<UnitInfo> GetSpawns()
	{
		List<UnitInfo> spawnList = new List<UnitInfo>();
		if (spawns != null && spawns.Length > 0)
		{
			spawnList.AddRange(spawns);
		}
		return spawnList;
	}

	public List<LevelObjectInfo> GetLevelObjects()
	{
		List<LevelObjectInfo> levelObjectList = new List<LevelObjectInfo>();
		if (levelObjects != null && levelObjects.Length > 0)
		{
			levelObjectList.AddRange(levelObjects);
		}
		return levelObjectList;
	}
}

public abstract class Info<T>
{
	[SerializeField]
	protected T type;

	[SerializeField]
	protected int[] position;

	public Vector2Int GetPosition()
	{
		if (position != null && position.Length >= 2)
		{
			return new Vector2Int(position[0], position[1]);
		}
		return (Vector2Int.one * -1);
	}

	public bool IsInvalid()
	{
		return (position == null || position.Length < 2 || position[0] < 0 || position[1] < 0);
	}

	public T GetInfoType()
	{
		return type;
	}
}

[System.Serializable]
public class TileInfo: Info<TileType>
{
	public TileInfo(TileType tileType, int[] gridPosition)
	{
		type = tileType;
		position = gridPosition;
	}

    public TileInfo(TileType tileType, List<int> gridPosition)
    {
        type = tileType;
        position = gridPosition.ToArray();
    }

    public Vector2Int GetTeleportPosition()
    {
        if (type == TileType.Teleport && position != null && position.Length >= 4)
        {
            return new Vector2Int(position[2], position[3]);
        }
        return (Vector2Int.one * -1);
    }

	public int GetVariant()
	{
		if (position != null)
		{
			switch (type)
			{
				case TileType.Teleport:
				{
					if (position.Length >= 5)
					{
						return Mathf.Max(position[4], 0);
					}
					break;
				}
				default:
				{
					if (position.Length >= 3)
					{
						return Mathf.Max(position[2], 0);
					}
					break;
				}
			}
		}
		return 0;
	}
}

[System.Serializable]
public class UnitInfo: Info<UnitType>
{
	public UnitInfo(UnitType type, Vector2Int gridPosition)
	{
		this.type = type;
		position = new int[2];
		position[0] = gridPosition.x;
		position[1] = gridPosition.y;
	}

	public int GetVariant()
	{
		if (position.Length >= 3)
		{
			return Mathf.Max(position[2], 0);
		}
		return 0;
	}
}

[System.Serializable]
public class LevelObjectInfo: Info<ObjectType>
{
	public LevelObjectInfo(ObjectType levelObjectType, int[] positionArray)
	{
		type = levelObjectType;
		position = positionArray;
	}

	public LevelObjectInfo(ObjectType levelObjectType, List<int> positionList)
	{
		type = levelObjectType;
		position = positionList.ToArray();
	}

	public int GetVariant()
	{
		if (position.Length >= 3)
		{
			return Mathf.Max(position[2], 0);
		}
		return 0;
	}
}