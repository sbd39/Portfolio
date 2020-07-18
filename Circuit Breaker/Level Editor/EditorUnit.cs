/*
 * EditorUnit.cs
 * Scott Duman
 * Represents a tile in the editor grid. Change change types and models to represent any tile in game.
 */
using UnityEngine;
using System.Collections.Generic;

public enum UnitType
{
    Player,
	DecoyBot,
    Grunto,
    Marko,
    Demo,
    Hotso,
    Healio,
    Bombo,
	Defendo,
    Grabbo,
}

public class EditorUnit : EditorObject 
{
	private static Dictionary<UnitType, GameObject> unitPrefabs;
	private UnitType type;
	public UnitType Type
	{
		get { return type; }
	}

	public EditorUnit(UnitType type, EditorTile tile, int variant = 0): base(tile, variant)
	{
		this.type = type;

		CreateGridUnit(variant);
	}

	public void UpdateUnit(UnitType newType, int unitVariant = 0)
	{
		type = newType;

		DestroyObject();
		CreateGridUnit(unitVariant);
	}

	private void CreateGridUnit(int newVariant = -1)
	{
		this.gridObject = SimpleObjectPool.Spawn(unitPrefabs[this.type], tile.transform.position, Quaternion.identity, tile.transform);
		gridObject.transform.LookAt(tile.transform.root);
		SetVariant(newVariant > -1 ? newVariant : variant);
	}

	public void SetVariant(int newVariant = 0)
	{
		if (newVariant < 0)
		{
			newVariant = 0;
		}
		/*
		if (objMats.ContainsKey(type) && variant < objMats[type].Count)
		{
			gridObject.GetComponentInChildren<MeshRenderer>().material = objMats[type][variant];
		}
		else
		{
			newVariant = 0;
		}
		*/
		this.variant = newVariant;
	}

	public override bool IsUnit()
	{
		return true;
	}

	protected override void SetUpPrefabs()
	{
		if (unitPrefabs == null)
		{
			unitPrefabs = new Dictionary<UnitType, GameObject>();
			for (int i = 0; i < 10; i++)
            {
                UnitType type = (UnitType)i;
                unitPrefabs.Add(type, Resources.Load<GameObject>(LevelEditor.ResourcePath + "Characters/" + type.ToString()));
            }
		}
	}

	protected override void SetUpMaterials()
	{
		//Do Nothing for now
	}

	public UnitInfo GetInfo()
	{
		return new UnitInfo(type, tile.Position);
	}
}