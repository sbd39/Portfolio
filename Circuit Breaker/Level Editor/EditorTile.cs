/*
 * EditorTile.cs
 * Scott Duman
 * Represents a tile in the editor grid. Change change types and models to represent any tile in the game.
 */
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Standard,
    UnbreakableWall,
    Lava,
    Heal,
    Teleport,
    Guardian,
    Removed,
    Jump,
}

public class EditorTile : MonoBehaviour
{
    private static Dictionary<TileType, List<Material>> tileMaterials = null;
    private static LevelEditor gridCreator;

    [HideInInspector]
    public GameObject gridButton;
    private EditorObject gridObject = null;
    public EditorObject GridObject
    {
        get {return gridObject;}
    }
    private TileType tileType = TileType.Standard;
    public TileType Type
    {
        get {return tileType;}
    }

    [SerializeField]
    private int variant = 0;
    public int GetVariant()
    {
        return variant;
    }

    private EditorList parentList;
    public Vector2Int Position
    {
        get
        {
            return new Vector2Int(gridCreator.grid.IndexOf(parentList), parentList.IndexOf(this));
        }
    }

    private EditorTile teleportPartner = null;
    public Vector2Int TeleportPosition
    {
        get
        {
            if (teleportPartner != null)
            {
                return teleportPartner.Position;
            }
            return Vector2Int.left;
        }
    }

    private GameObject[] activeModels;

    public TeleporterColor TeleporterColor
    {
        get { return (TeleporterColor)variant; }
    }
    private Dictionary<TileType, MeshRenderer> renderers = null;

    private void Awake()
    {
        activeModels = new GameObject[2];
        activeModels[0] = transform.GetChild(0).gameObject;

        SetTileMaterials();
    }

    private void SetTileMaterials()
    {
        if (tileMaterials == null)
        {
            tileMaterials = new Dictionary<TileType, List<Material>>();

            AddMaterials(TileType.Teleport, 3);
            AddMaterials(TileType.Standard, 3, "Base");
        }
    }

    private void AddMaterials(TileType type, int count, string prefex = "")
    {
        if (prefex == "")
        {
            prefex = type.ToString();
        }

        List<Material> mats = new List<Material>();
        for(int i = 1; i <= count; i++)
        {
            mats.Add(Resources.Load<Material>("Materials/Tiles/"+ prefex + "_" + i));
        }
        tileMaterials.Add(type, mats);
    }


    private void Start() 
    {

        int count = transform.childCount;
        if (IsOccuppied())
        {
            count--;
        }

        for(int i = 0; i < count; i++)
        {
            Transform parent = transform.GetChild(i);

            if (i != 4)
            {
                MouseTriggers triggers = null;
                if (i == 2)
                {
                    triggers = parent.GetChild(0).GetChild(0).GetComponent<MouseTriggers>();
                }
                else
                {
                    triggers = parent.GetChild(0).GetComponent<MouseTriggers>();
                }

                triggers.onMouseDown.AddListener(OnMouseDown);
                triggers.onMouseEnter.AddListener(OnMouseEnter);
                triggers.onMouseOver.AddListener(OnMouseOver);
            }

            if (i != 0)
            {
                parent.gameObject.SetActive(false);
            }
        }

        SetUpRenderers();
    }

    private void SetUpRenderers()
    {
        renderers = new Dictionary<TileType, MeshRenderer>();
        renderers.Add(TileType.Teleport, transform.GetChild(4).GetChild(1).GetChild(0).GetComponent<MeshRenderer>());
        renderers.Add(TileType.Standard, transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>());
    }

    public void Initalize(EditorList parent, LevelEditor creator, GameObject button)
    {
        if (gridCreator == null)
        {
            gridCreator = creator;
        }
        gridButton = button;
        gridButton.transform.position -= (Vector3.up * 0.1f);
        gridButton.SetActive(false);

        parentList = parent;
        RemoveGridObject();
        UpdateTile(TileType.Standard);
    }

    public void SetVariant(int matVariant)
    {
        if (matVariant < 0)
        {
            variant = 0;
            return;
        }

        if (tileMaterials == null)
        {
            SetTileMaterials();
        }
        if (renderers == null)
        {
            SetUpRenderers();
        }

        if (renderers.ContainsKey(tileType) && tileMaterials.ContainsKey(tileType) && matVariant < tileMaterials[tileType].Count)
        {
            renderers[tileType].material = tileMaterials[tileType][matVariant];
            variant = matVariant;
        }
        else
        {
            variant = 0;
        }
    }

    public void SetTeleportDesination(EditorTile tile, int color)
    {
        if (tile != null)
        {
            int x = tile.Position.x;
            if (gridCreator.grid.Count > x && x > -1 && gridCreator.grid[x].Contains(tile))
            {
                if (tile.Type == TileType.Teleport && tileType == TileType.Teleport)
                {
                    if (!CheckTeleportPosition(tile.Position))
                    {
                        teleportPartner = tile;
                    }
                    SetVariant(color);

                    if (!tile.CheckTeleportPosition(Position))
                    {
                        tile.SetTeleportDesination(this, color);
                    }
                    return;
                }
            }
        }
        //Teleport Desination Removed
        teleportPartner = null;
    }

    private bool CheckTeleportPosition(Vector2Int position)
    {
        if (position == null || teleportPartner == null)
        {
            return false;
        }
        Vector2Int teleportPos = TeleportPosition;
        return (teleportPos.x == position.x && teleportPos.y == position.y);
    }

    public bool IsOccuppied()
    {
        return (gridObject != null);
    }

    public void AddGridObject(EditorObject gridObject)
    {
        if (gridObject == null)
        {
            Debug.LogError("ERROR: Passed Values are null!");
        }

        if (this.IsOccuppied())
        {
            RemoveGridObject();
        }

        this.gridObject = gridObject;
    }

    public void RemoveGridObject()
    {
        if (gridObject != null)
        {
            //Grid has change and file is no longer consistant with grid
            if (gridCreator.currentGridFile != null)
            {
                gridCreator.currentGridFile = null;
            }
            gridObject.DestroyObject();
            if (gridObject.IsUnit())
            {
                gridCreator.RemoveCharacterSpawn((EditorUnit)gridObject);
            }
            else
            {
                gridCreator.RemoveLevelHazard((EditorLevelObject)gridObject);
            }
            gridObject = null;
        }
    }

    public void UpdateTile()
    {
        TileType type = gridCreator.editorParams.tile;

        int tileVariant = 0;
        if (gridCreator.editorParams.tileVariants.ContainsKey(type))
        {
            tileVariant = gridCreator.editorParams.tileVariants[type].variant;
        }

        UpdateTile(type, tileVariant);
        if (type == TileType.Removed)
        {
            gridCreator.removedTileButtons.Add(gridButton);
        }
    }

    public void UpdateTile(TileType type, int tileVariant = 0)
    {
        if (type != tileType)
        {
            if (gridCreator.currentGridFile != null)
            {
                gridCreator.currentGridFile = null;
            }

            tileType = type;
            foreach(GameObject model in activeModels)
            {
                if (model != null)
                {
                    model.SetActive(false);
                }
            }

            if (type != TileType.Removed)
            {
                TeleporterColor color = gridCreator.editorParams.GetTeleColor();
                int tileIndex = (int)type;
                
                if (tileIndex < 6)
                {
                    activeModels[0] = transform.GetChild(tileIndex).gameObject;
                }
                else if (tileIndex > 6)
                {
                    activeModels[0] = transform.GetChild(tileIndex - 1).gameObject;
                }

                activeModels[0].SetActive(true);
                if (type == TileType.Teleport)
                {
                    activeModels[1] = transform.GetChild(0).gameObject;
                    activeModels[1].SetActive(true);
                    AddTeleporter(color);
                }
                else if (tileType == TileType.Teleport && gridCreator.teleportTiles[color].Count > 1)
                {
                    RemoveTeleportPartner(color);
                }

                tileType = type;
                if (gridCreator.editorParams.tileVariants.ContainsKey(tileType))
                {
                    SetVariant(gridCreator.editorParams.tileVariants[tileType].variant);
                }

                //Removes Character if the tile has been replaced with a wall
                if (type == TileType.UnbreakableWall)
                {
                    RemoveGridObject();
                }
            }
            else
            {
                RemoveGridObject();
                if (gridCreator.editorParams.tile == TileType.Removed)
                {
                    gridButton.SetActive(true);
                }
                gameObject.SetActive(false);
            }
        }
        else if (gridCreator.editorParams.tileVariants.ContainsKey(type) && variant != tileVariant)
        {
            if (gridCreator.currentGridFile != null)
            {
                gridCreator.currentGridFile = null;
            }

            SetVariant(tileVariant);
            if (type == TileType.Teleport)
            {
                TeleporterColor color = (TeleporterColor)tileVariant;
                RemoveTeleportPartner(color);
                AddTeleporter(color);
            }
        }
    }

    private void RemoveTeleportPartner(TeleporterColor color)
    {
        if (teleportPartner != null)
        {
            teleportPartner.SetTeleportDesination(null, this.variant);
        }

        if (gridCreator.teleportTiles[color].Contains(this))
        {
            gridCreator.teleportTiles[color].Remove(this);
        }
        teleportPartner = null;
    }

    private void AddTeleporter(TeleporterColor color)
    {
        if (gridCreator.teleportTiles[color].Count > 1)
        {
            gridCreator.teleportTiles[color][0].UpdateTile(TileType.Standard);
            gridCreator.teleportTiles[color].RemoveAt(0);
        }
        if (gridCreator.teleportTiles[color].Count == 1)
        {
            SetTeleportDesination(gridCreator.teleportTiles[color][0], (int)color);
        }
        gridCreator.teleportTiles[color].Add(this);
    }

    public void ResetTile()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            gridButton.SetActive(false);
        }
        UpdateTile(TileType.Standard);
        RemoveGridObject();
    }

    public void UpdateGridObject()
    {
        if (gridObject != null)
        {
            gridObject.UpdateObjectPosition();
        }
    }

    public void SetButtonActive(bool active)
    {
        gridButton.SetActive(active);
    }

    public void AddTile()
    {
        if (gridButton.activeSelf)
        {
            gridButton.SetActive(false);
        }
        UpdateTile(TileType.Standard);
        gridCreator.removedTileButtons.Remove(gridButton);
        gameObject.SetActive(true);
    }

    private void OnDestroy() 
    {
        if (IsOccuppied())
        {
            RemoveGridObject();
        }
    }

    private void OnMouseDown()
    {
        switch (gridCreator.editorParams.mode)
        {
            case EditMode.EditTiles:
            {
                UpdateTile();
                break;
            }
            case EditMode.EditSpawns:
            {
                if (tileType != TileType.UnbreakableWall)
                {
                    UpdateUnit();
                }
                break;
            }
            case EditMode.EditLevelObjects:
            {
                if (tileType != TileType.UnbreakableWall)
                {
                    UpdateLevelObject();
                }
                break;
            }
        }
    }

    private void OnMouseOver() 
    {
        switch(gridCreator.editorParams.mode)
        {
            case EditMode.EditTiles:
            {
                if (Input.GetMouseButton(1))
                {
                    UpdateTile(TileType.Standard);
                }
                break;
            }
            default:
            {
                if (tileType != TileType.UnbreakableWall && IsOccuppied() && Input.GetMouseButton(1))
                {
                    RemoveGridObject();
                }
                break;
            }
        }
    }

    private void OnMouseEnter()
    {
        if (gridCreator.editorParams.drag)
        {
            switch (gridCreator.editorParams.mode)
            {
                case EditMode.EditTiles:
                {
                    if (Input.GetMouseButton(0))
                    {
                        UpdateTile();
                    }
                    else if (Input.GetMouseButton(1))
                    {
                        UpdateTile(TileType.Standard);
                    }
                    break;
                }
                case EditMode.EditSpawns:
                {
                    if (tileType != TileType.UnbreakableWall)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            UpdateUnit();
                        }
                        else if (IsOccuppied() && Input.GetMouseButton(1))
                        {
                            RemoveGridObject();
                        }
                    }
                    break;
                }
                case EditMode.EditLevelObjects:
                {
                    if (tileType != TileType.UnbreakableWall)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            UpdateLevelObject();
                        }
                        else if (IsOccuppied() && Input.GetMouseButton(1))
                        {
                            RemoveGridObject();
                        }
                    }
                    break;
                }
            }
        }
        //Method Ends
    }

    private void UpdateUnit()
    {
        if (IsOccuppied() && gridObject.IsUnit())
        {
            EditorUnit unit = (EditorUnit)gridObject;

            if (unit.Type != gridCreator.editorParams.unit)
            {
                gridCreator.UpdateSpawn(unit);
                unit.UpdateUnit(gridCreator.editorParams.unit);
            }
        }
        else
        {
            gridCreator.AddCharacterSpawn(this);
        }
    }

    private void UpdateLevelObject()
    {
        if (IsOccuppied() && !gridObject.IsUnit())
        {
            Dictionary<ObjectType, Variant> dict = gridCreator.editorParams.objVariants;
            EditorLevelObject obj = (EditorLevelObject)gridObject;

            if (obj.Type != gridCreator.editorParams.obj)
            {
                if (dict.ContainsKey(obj.Type))
                {
                    obj.UpdateLevelObject(gridCreator.editorParams.obj);
                }
                obj.UpdateLevelObject(gridCreator.editorParams.obj);
            }
            else if (dict.ContainsKey(obj.Type) && obj.Variant != dict[obj.Type].variant)
            {
                obj.SetVariant(dict[obj.Type].variant);
            }
        }
        else
        {
            gridCreator.AddLevelHazard(this);
        }
    }

    public TileInfo GetInfo()
    {
        List<int> values = new List<int>();

        Vector2Int pos = Position;
        values.Add(pos.x);
        values.Add(pos.y);

        if (tileType == TileType.Teleport)
        {
            pos = TeleportPosition;
            values.Add(pos.x);
            values.Add(pos.y);
        }

        if (gridCreator.editorParams.tileVariants.ContainsKey(tileType))
        {
            values.Add(variant);
        }

        return new TileInfo(tileType, values);
    }
}