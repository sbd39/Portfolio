/*
 * LevelEditor.cs
 * Scott Duman
 * Used to create and edit grids in the custom grid editor
 */
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public enum GridType
{
    Radial,
    Square
}

public enum EditMode
{
    EditTiles,
    EditSpawns,
    EditLevelObjects
}

public enum TeleporterColor: int
{
    Purple,
    Teal,
    Yellow,
}

public class LevelEditor : MonoBehaviour
{
    #region Variables
    public static string ResourcePath = "Prefabs/GridEditor/";
    private static GameObject tilePrefab = null;
    private static GameObject buttonPrefab = null;
    
    [HideInInspector]
    public List<EditorList> grid = new List<EditorList>();
    [HideInInspector]
    public UnityEvent onLevelLoad = new UnityEvent();
    private GridParameters gridParams = new GridParameters(5, 8, 8);
    private float gridSpacing = 1;

    [HideInInspector]
    public string currentGridFile = null;
    private List<string> loadableFiles = new List<string>();
    public List<string> LoadableFiles
    {
        get { return loadableFiles; }
    }

    [HideInInspector]
    public string savefileName = "New Grid", loadFileName = null;

    public LevelEditorParameters editorParams = new LevelEditorParameters();

    [HideInInspector]
    public EditorUnit player = null;
    [HideInInspector]
    public List<EditorUnit> decoyBotSpawns, enemySpawns;
    [HideInInspector]
    public List<EditorLevelObject> levelObjects = new List<EditorLevelObject>();

    [HideInInspector]
    public Dictionary<TeleporterColor, List<EditorTile>> teleportTiles = new Dictionary<TeleporterColor, List<EditorTile>>();
    [HideInInspector]
    public List<GameObject> removedTileButtons = new List<GameObject>();
    #endregion

    #region Initalization

    private void Awake() 
    {
        Init(grid == null);
    }

    /// <summary>Internal method used to initalize the grid creator.</summary>
    /// <param name="removeGrid">Parameter for whether the grid should be removed.</param>
    private void Init(bool removeGrid = true)
    {
        if (removeGrid)
        {
            RemoveGrid();
        }
        if (grid == null)
        {
            grid = new List<EditorList>();
        }

        for(int i = 0; i < 3; i++)
        {
            teleportTiles.Add((TeleporterColor)i,  new List<EditorTile>());
        }

        decoyBotSpawns = new List<EditorUnit>();
        enemySpawns = new List<EditorUnit>();

        SetPrefabs();
    }

    private void Start()
    {
        Time.timeScale = 1;
        UpdateLoadableFiles();

        Transform parent = transform.GetChild(2).GetChild(0);
        GridParamManager gpm = parent.GetChild(0).GetComponent<GridParamManager>();
        gpm.onGridTypeChange.AddListener(UpdateGridType);
        gpm.onRadiusChange.AddListener(UpdateRadius);
        gpm.onWidthChange.AddListener(UpdateWidth);
        gpm.onHeightChange.AddListener(UpdateHeight);
        gpm.onResetGrid.AddListener(ResetGrid);
        gpm.Initalize(gridParams, onLevelLoad);

        editorParams.tileVariants.Add(TileType.Standard, new Variant(3));
        editorParams.tileVariants.Add(TileType.Teleport, new Variant(3));
        editorParams.objVariants.Add(ObjectType.Wall, new Variant(3));
        editorParams.objVariants.Add(ObjectType.Bomb, new Variant(5));

        LevelEditingManager lem = parent.GetChild(1).GetComponent<LevelEditingManager>();
        lem.parameters = editorParams;
        lem.setButtonsActive = SetButtonsActive;
        lem.Initalize();
        
        GameManager.startWithControls = false;
        CameraManager.Instance.Initialize(gameObject, gameObject);
        CreateGrid();
    }

    /// <summary>Sets prefabs that the grid creator uses.</summary>
    private void SetPrefabs()
    {
        if (tilePrefab == null)
        {
            tilePrefab = Resources.Load<GameObject>(ResourcePath + "EditorTile");
        }
        if (buttonPrefab == null)
        {
            buttonPrefab = Resources.Load<GameObject>(ResourcePath + "TileButton");
        }
    }
    #endregion

    #region GridManagement
    private void UpdateGridType(System.Enum type)
    {
        GridType newType = (GridType)type;
        if (gridParams.type != newType)
        {
            gridParams.type = newType;
            CreateGrid();
        }
    }

    /// <summary>Creates grid baised on the current settings of the grid creator.</summary>
    public void CreateGrid()
    {
        //Sets prefabs if nessicary
        if (tilePrefab == null)
        {
            SetPrefabs();
        }

        switch (gridParams.type)
        {
            case GridType.Radial:
            {
                CreateRadialGrid();
                //Updates bounds of the Camera to the new grid bounds
                CameraManager.Instance.UpdateBounds(gridParams.type, gridParams.GetRadial());
                break;
            }
            case GridType.Square:
            {
                CreateSquareGrid();
                //Updates bounds of the Camera to the new grid bounds
                CameraManager.Instance.UpdateBounds(gridParams.type, gridParams.GetSquare());
                break;
            }
        }
    }

    /// <summary>Internal method used to create a radial grid baised on the current parameters of the grid creator.</summary>
    /// <param name="tiles">List of tile info to be included in the grid (used primarily for loading grids).</param>
    /// <param name="spawns">List of character spawn info to be included in the grid (used primarily for loading grids).</param>
    /// <param name="levelObjects">List of level object info to be included in the grid (used primarily for loading grids).</param>
    private void CreateRadialGrid(List<TileInfo> tiles = null, List<UnitInfo> spawns = null, List<LevelObjectInfo> levelObjects = null)
    {
        RemoveGrid();
        grid = new List<EditorList>();
        float xOffset = (gridParams.radius / 2 - (gridParams.radius % 2 == 0 ? 0.5f : 0)) * gridSpacing;
        float outerRadius = (gridSpacing / Mathf.Sqrt(3) * 1.5f);
        float zOffset = -outerRadius * (gridParams.radius - 1);
        int numHexes = gridParams.radius;

        for (int i = 0; i < gridParams.radius * 2 - 1; i++)
        {
            CreateRow(tiles, xOffset, zOffset, i, numHexes);

            if (i > gridParams.radius - 2)
            {
                numHexes--;
                xOffset -= gridSpacing / 2;
            }
            else
            {
                numHexes++;
                xOffset += gridSpacing / 2;
            }
            zOffset += outerRadius;
        }

        AddSpawners(spawns);
        AddLevelObjects(levelObjects);
    }

    /// <summary>Internal method used to create a square grid baised on the current parameters of the grid creator.</summary>
    /// <param name="tiles">List of tile info to be included in the grid (used primarily for loading grids).</param>
    /// <param name="spawns">List of character spawn info to be included in the grid (used primarily for loading grids).</param>
    /// <param name="levelObjects">List of level object info to be included in the grid (used primarily for loading grids).</param>
    private void CreateSquareGrid(List<TileInfo> tiles = null, List<UnitInfo> spawns = null, List<LevelObjectInfo> levelObjects = null)
    {
        RemoveGrid();
        grid = new List<EditorList>();
        float xOffset = (gridParams.width/2 - (gridParams.width % 2 == 0 ? 0.5f : 0)) * gridSpacing;
        float outerRadius = gridSpacing/Mathf.Sqrt(3);
        float zOffset =  -(outerRadius * 1.5f) * (gridParams.height/2);

        for (int i = 0; i < gridParams.height; i++)
        {
            CreateRow(tiles, xOffset, zOffset, i, gridParams.width);

            if (i % 2 == 0)
            {
                xOffset -= gridSpacing/2;
            }
            else
            {
                xOffset += gridSpacing/2;
            }
            zOffset += (outerRadius * 1.5f);
        }

        AddSpawners(spawns);
        AddLevelObjects(levelObjects);
    }

    /// <summary>Updates a radial grid to match the new given radius.</summary>
    /// <param name="newRadius">The value the grid radius will be updated to.</param>
    public void UpdateRadius(int newRadius)
    {
        //Grid has changed and file is no longer consistant with grid
        if (currentGridFile != null)
        {
            currentGridFile = null;
        }

        int difference = newRadius - gridParams.radius;
        if (difference < 0)
        {
            for (int i = 0; i < -difference; i++)
            {
                RemoveRow(grid.Count - 1);
                RemoveRow(0);
                foreach (EditorList list in grid)
                {
                    RemoveTile(list, list.list[0]);
                    RemoveTile(list, list.list[list.list.Count - 1]);
                }
            }
        }
        else if (difference > 0)
        {
            for(int i = 1; i <= difference; i++)
            {
                int radius = gridParams.radius + i;
                float xOffset = (radius / 2 - (radius % 2 == 0 ? 0.5f : 0)) * gridSpacing;
                float zOffset = ((gridSpacing / Mathf.Sqrt(3)) * 1.5f);

                foreach (EditorList list in grid)
                {
                    Vector3 pos = list.list[0].transform.position;
                    pos.x += gridSpacing;
                    list.Insert(0, CreateTile(list, pos));
                    pos.x -= gridSpacing * list.Count;
                    list.Add(CreateTile(list, pos));
                }

                float edge = grid[grid.Count - 1].list[0].transform.position.z;
                CreateRow(null, xOffset, edge + zOffset, grid.Count, radius);
                edge = grid[0].list[0].transform.position.z;
                CreateRow(null, xOffset, edge - zOffset, 0, radius);
            }
        }
        gridParams.radius = newRadius;
    }

    /// <summary>Updates a square grid's width to match the new given grid width.</summary>
    /// <param name="newWidth">The value the grid width will be updated to.</param>
    public void UpdateWidth(int newWidth)
    {
        //Grid has changed and file is no longer consistant with grid
        if (currentGridFile != null)
        {
            currentGridFile = null;
        }

        int widthDif = newWidth - gridParams.width;
        if (widthDif != 0)
        {
            Vector3 addPosition = grid[0].list[0].transform.position;
            bool adding = widthDif > 0;
            widthDif = Mathf.Abs(widthDif);
            for (int i = 0; i < widthDif; i++)
            {
                foreach (EditorList list in grid)
                {
                    if (adding)
                    {
                        addPosition = list.list[0].transform.position;
                        addPosition.x += gridSpacing;
                        list.Insert(0, CreateTile(list, addPosition));
                    }
                    else
                    {
                        RemoveTile(list, list.list[0]);
                    }
                }
            }
            
            //Updates the positions of the tiles so that the grid is centered
            float offset = widthDif * (gridSpacing / 2);
            if (adding)
            {
                offset = -offset;
            }
            foreach (EditorList list in grid)
            {
                Vector3 updatePosition = list.list[0].transform.position;
                foreach (EditorTile tile in list.list)
                {
                    updatePosition.x = tile.transform.position.x + offset;
                    tile.transform.position = updatePosition;
                    tile.UpdateGridObject();
                }
            }
        }
        gridParams.width = newWidth;
    }

    /// <summary>Updates a square grid's height to match the new given grid height.</summary>
    /// <param name="newHeight">The value the grid height will be updated to.</param>
    public void UpdateHeight(int newHeight)
    { 
        //Grid has changed and file is no longer consistant with grid
        if (currentGridFile != null)
        {
            currentGridFile = null;
        }

        int heightDif = newHeight - gridParams.height;
        if (heightDif != 0)
        {
            Vector3 addPosition = grid[grid.Count - 1].list[0].transform.position;
            float zOffset = ((gridSpacing / Mathf.Sqrt(3)) * 1.5f);
            bool adding = heightDif > 0;
            heightDif = Mathf.Abs(heightDif);
            for (int i = 0; i < heightDif; i++)
            {
                if (adding)
                {
                    float current = grid[grid.Count - 1].list[0].transform.position.x;
                    float previous = grid[grid.Count - 2].list[0].transform.position.x;
                    addPosition.x += -(current - previous);
                    addPosition.z += zOffset;
                    CreateRow(null, addPosition.x, addPosition.z, grid.Count, gridParams.width);
                }
                else
                {
                    RemoveRow(grid.Count - 1);
                }
            }

            //Updates the positions of the tiles so that the grid is centered
            float offset = heightDif * zOffset / 2;
            if (adding)
            {
                offset = -offset;
            }
            foreach(EditorList list in grid)
            {
                Vector3 updatePosition = list.list[0].transform.position;
                updatePosition.z += offset;
                foreach (EditorTile tile in list.list)
                {
                    updatePosition.x = tile.transform.position.x;
                    tile.transform.position = updatePosition;
                    tile.UpdateGridObject();
                }
            }
        }
        gridParams.height = newHeight;
    }

    /// <summary>Resets grid by seting all tiles back to standard tiles and removes all character spawns.</summary>
    public void ResetGrid()
    {
        currentGridFile = null;
        if (grid != null && grid.Count > 0)
        {
            foreach (EditorList list in grid)
            {
                if (list != null)
                {
                    foreach (EditorTile space in list.list)
                    {
                        if (space != null)
                        {
                            space.ResetTile();
                        }
                    }
                }
            }
        }
        foreach(TeleporterColor color in teleportTiles.Keys)
        {
            teleportTiles[color].Clear();
        }
        removedTileButtons.Clear();
        enemySpawns.Clear();
        player = null;
    }

    /// <summary>Removes the current grid.</summary>
    public void RemoveGrid()
    {
        currentGridFile = null;
        if (grid != null && grid.Count > 0)
        {
            foreach (EditorList list in grid)
            {
                if (list != null)
                {
                    foreach (EditorTile space in list.list)
                    {
                        if (space != null)
                        {
                            SimpleObjectPool.Despawn(space.gridButton);
                            SimpleObjectPool.Despawn(space.gameObject);
                        }
                    }
                }
            }
            grid.Clear();
        }
        foreach(TeleporterColor color in teleportTiles.Keys)
        {
            teleportTiles[color].Clear();
        }
        removedTileButtons.Clear();
        enemySpawns.Clear();
        player = null;
    }
    #endregion

    #region TileManagement
    /// <summary>Creates a tile at the given position.</summary>
    /// <param name="parent">The editor list the tile will be in.</param>
    /// <param name="position">The desired position of the tile.</param>
    private EditorTile CreateTile(EditorList parent, Vector3 position)
    {
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 90, 0));
        GameObject EditorSpace = SimpleObjectPool.Spawn(tilePrefab, position, rotation, transform.GetChild(0));
        EditorTile tile = EditorSpace.GetComponent<EditorTile>();

        rotation = Quaternion.Euler(new Vector3(90, 0, 0));
        GameObject gridButton = SimpleObjectPool.Spawn(buttonPrefab, position, rotation, transform.GetChild(1));

        UnityEngine.UI.Button button = gridButton.GetComponent<UnityEngine.UI.Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(tile.AddTile);

        tile.Initalize(parent, this, gridButton);
        return tile;
    }

    /// <summary>Creates a tile at the given position.</summary>
    /// <param name="parent">The editor list the tile will be in.</param>
    /// <param name="xOffset">The desired x position of the tile.</param>
    /// <param name="zOffset">The desired z position of the tile.</param>
    private EditorTile CreateTile(EditorList parent, float xOffset, float zOffset)
    {
        Vector3 position = new Vector3(xOffset, 0, zOffset) + transform.position;
        return CreateTile(parent, position);
    }

    /// <summary>Removes tile from the grid.</summary>
    /// <param name="list">The editor list the tile is contained in.</param>
    /// <param name="tile">The tile to be removed.</param>
    private void RemoveTile(EditorList list, EditorTile tile)
    {
        list.list.Remove(tile);
        tile.RemoveGridObject();
        SimpleObjectPool.Despawn(tile.gameObject);
        foreach(List<EditorTile> teleporters in teleportTiles.Values)
        {
            if (teleporters.Contains(tile))
            {
                teleporters.Remove(tile);
                break;
            }
        }
    }

    /// <summary>Creates a row of editor tiles based on the given parameters.</summary>
    /// <param name="tiles">List of tile info that could be included in the row (used primarily for loading grids).</param>
    /// <param name="xOffset">The x position of teh first tile.</param>
    /// <param name="zOffset">The z position of all tiles in the row.</param>
    /// <param name="gridIndex">The index of the row in the grid.</param>
    /// <param name="numHexes">The number of desired tiles in the row.</param>
    private void CreateRow(List<TileInfo> tiles, float xOffset, float zOffset, int gridIndex, int numHexes)
    {
        EditorList list = new EditorList();
        if (gridIndex > grid.Count - 1)
        {
            grid.Add(list);
        }
        else if (gridIndex > -1)
        {
            grid.Insert(gridIndex, list);
        }
        else return;

        for (int n = 0; n < numHexes; n++)
        {
            EditorTile tile = CreateTile(list, xOffset - (n * gridSpacing), zOffset);

            if (tile != null)
            {
                bool teleport = false;
                Vector2Int teleportPos = Vector2Int.left;
                list.Add(tile);
                if (tiles != null && tiles.Count > 0)
                {
                    while(tiles.Count > 0 && tiles[0].IsInvalid())
                    {
                        tiles.RemoveAt(0);
                    }

                    if (tiles.Count > 0 && MatchSpace(gridIndex, n, tiles[0].GetPosition()))
                    {
                        TileType type = tiles[0].GetInfoType();
                        if (type == TileType.Teleport)
                        {
                            //Sets teleporter parner Position
                            teleportPos = tiles[0].GetTeleportPosition();
                            //Sets teleporter's color
                            editorParams.tileVariants[TileType.Teleport].variant = tiles[0].GetVariant();
                            teleport = true;
                        }
                        else if (type == TileType.Removed)
                        {
                            removedTileButtons.Add(tile.gridButton);
                        }



                        tile.UpdateTile(type, tiles[0].GetVariant());
                        tiles.RemoveAt(0);
                    }
                }

                if (teleport)
                {
                    if (CheckTilePosition(teleportPos))
                    {
                        tile.SetTeleportDesination(GetTile(teleportPos), editorParams.tileVariants[TileType.Teleport].variant);
                    }
                }
            }
            else
            {
                Debug.LogError("Error: Grid Creator's Editor Tile prefab does not have a EditorTile component!");
            }
        }
    }

    /// <summary>Removes the given row from the grid.</summary>
    /// <param name="gridListIndex">The index of the row to be removed.</param>
    private void RemoveRow(int gridListIndex)
    {
        if (grid != null && gridListIndex > -1 && gridListIndex < grid.Count)
        {
            EditorList list = grid[gridListIndex];
            for (int i = list.Count - 1; i > -1; i--)
            {
                RemoveTile(list, list.list[i]);
            }
            grid.Remove(list);
        }
    }
    #endregion

    #region CharacterSpawnManagement
    /// <summary>Adds character spawns to the grid (only used for loading grids).</summary>
    /// <param name="spawns">List of spawn info to be added to the grid.</param>
    private void AddSpawners(List<UnitInfo> spawns)
    {
        if (spawns != null && spawns.Count > 0)
        {
            enemySpawns = new List<EditorUnit>();
            foreach (UnitInfo info in spawns)
            {
                if (!CheckTilePosition(info.GetPosition()))
                {
                    continue;
                }

                Vector2Int position = info.GetPosition();
                EditorTile tile = grid[position.x].Get(position.y);
                UnitType type = info.GetInfoType();

                EditorUnit character = new EditorUnit(type, tile);
                tile.AddGridObject(character);

                if (type == UnitType.Player)
                {
                    player = character;
                }
                else if (type == UnitType.DecoyBot)
                {
                    decoyBotSpawns.Add(character);
                }
                else
                {
                    enemySpawns.Add(character);
                }
            }
        }
    }

    /// <summary>Adds character spawn to the given tile.</summary>
    /// <param name="tile">Tile that the character will be spawned at.</param>
    public void AddCharacterSpawn(EditorTile tile)
    {
        bool isWall = false;
        if (tile.IsOccuppied())
        {
            if (tile.GridObject.IsUnit())
            {
                UnitType type = ((EditorUnit)tile.GridObject).Type;
                if (type == editorParams.unit)
                {
                    return;
                }
            }
            else
            {
                isWall = ((EditorLevelObject)tile.GridObject).Type == ObjectType.Wall;
            }
        }

        if (tile.Type != TileType.UnbreakableWall && !isWall)
        {
            //Grid has change and file is no longer consistant with grid
            if (currentGridFile != null)
            {
                currentGridFile = null;
            }

            EditorUnit character = new EditorUnit(editorParams.unit, tile);
            tile.AddGridObject(character);

            switch (editorParams.unit)
            {
                case UnitType.Player:
                {
                    if (player != null)
                    {
                        player.Tile.RemoveGridObject();
                    }
                    player = character;
                    break;
                }
                case UnitType.DecoyBot:
                {
                    if (decoyBotSpawns == null)
                    {
                        decoyBotSpawns = new List<EditorUnit>();
                    }
                    decoyBotSpawns.Add(character);
                    break;
                }
                default:
                {
                    if (enemySpawns == null)
                    {
                        enemySpawns = new List<EditorUnit>();
                    }
                    enemySpawns.Add(character);
                    break;
                }
            }
        }
    }

    /// <summary>Removes character spawn from the grid.</summary>
    /// <param name="info">Editor character that is being removed.</param>
    public void RemoveCharacterSpawn(EditorUnit unit)
    {
        switch (unit.Type)
        {
            case UnitType.Player:
            {
                if (player != null && player.Equals(unit))
                {
                    player = null;
                }
                break;
            }
            case UnitType.DecoyBot:
            {
                if (decoyBotSpawns != null && decoyBotSpawns.Contains(unit))
                {
                    decoyBotSpawns.Remove(unit);
                }
                break;
            }
            default:
            {
                if (enemySpawns != null && enemySpawns.Contains(unit))
                {
                    enemySpawns.Remove(unit);
                }
                break;
            }
        }
    }

    public void UpdateSpawn(EditorUnit unit)
    {
        RemoveCharacterSpawn(unit);

        //Grid has change and file is no longer consistant with grid
        if (currentGridFile != null)
        {
            currentGridFile = null;
        }

        switch (editorParams.unit)
        {
            case UnitType.Player:
            {
                if (player != null)
                {
                    player.Tile.RemoveGridObject();
                }
                player = unit;
                break;
            }
            case UnitType.DecoyBot:
            {
                if (decoyBotSpawns == null)
                {
                    decoyBotSpawns = new List<EditorUnit>();
                }
                decoyBotSpawns.Add(unit);
                break;
            }
            default:
            {
                if (enemySpawns == null)
                {
                    enemySpawns = new List<EditorUnit>();
                }
                enemySpawns.Add(unit);
                break;
            }
        }
    }
    #endregion

    #region HazardManagement

    /// <summary>Adds level objects to the level (only used for loading grids).</summary>
    /// <param name="levelObjectInfo">List of hazard info to be added to the grid.</param>
    public void AddLevelObjects(List<LevelObjectInfo> levelObjectInfo)
    {
        if (levelObjectInfo != null && levelObjectInfo.Count > 0)
        {
            levelObjects = new List<EditorLevelObject>();
            foreach (LevelObjectInfo info in levelObjectInfo)
            {
                if (!CheckTilePosition(info.GetPosition()))
                {
                    continue;
                }

                Vector2Int position = info.GetPosition();
                EditorTile tile = grid[position.x].Get(position.y);
                ObjectType type = info.GetInfoType();
                EditorLevelObject hazard = new EditorLevelObject(type, tile, info.GetVariant());

                tile.AddGridObject(hazard);
                levelObjects.Add(hazard);
            }
        }
    }

    /// <summary>Adds hazard to the given tile.</summary>
    /// <param name="tile">Tile that the hazard will be spawned at.</param>
    public void AddLevelHazard(EditorTile tile)
    {
        if (tile.IsOccuppied() && !tile.GridObject.IsUnit())
        {
            ObjectType type = ((EditorLevelObject)tile.GridObject).Type;
            if (type == editorParams.obj)
            {
                return;
            }
        }

        if (tile.Type != TileType.UnbreakableWall)
        {
            //Grid has change and file is no longer consistant with grid
            if (currentGridFile != null)
            {
                currentGridFile = null;
            }

            int variant = 0;
            if (editorParams.objVariants.ContainsKey(editorParams.obj))
            {
                variant = editorParams.objVariants[editorParams.obj].variant;
            }

            EditorLevelObject hazard = new EditorLevelObject(editorParams.obj, tile, variant);
            tile.AddGridObject(hazard);

            if (levelObjects == null)
            {
                levelObjects = new List<EditorLevelObject>();
            }
            levelObjects.Add(hazard);
        }
    }

    /// <summary>Removes character spawn from the grid.</summary>
    /// <param name="info">Editor character that is being removed.</param>
    public void RemoveLevelHazard(EditorLevelObject hazardObject)
    {
        if (levelObjects != null && levelObjects.Contains(hazardObject))
        {
            levelObjects.Remove(hazardObject);
        }
    }
    #endregion

    #region Saving/Loading
    /// <summary>Saves the current grid as a json file.</summary>
    public void SaveGrid()
    {
        if (grid != null)
        {
            int[] parameters = gridParams.GetParamaters();

            List<TileInfo> tiles = new List<TileInfo>();
            foreach(EditorList row in grid)
            {
                foreach(EditorTile tile in row.list)
                {
                    if (tile.Type != TileType.Standard || tile.GetVariant() != 0)
                    {
                        tiles.Add(tile.GetInfo());
                    }
                }
            }

            List<UnitInfo> spawns = new List<UnitInfo>();
            spawns.Add(player.GetInfo());
            foreach(EditorUnit decoy in decoyBotSpawns)
            {
                spawns.Add(decoy.GetInfo());
            }

            //Create Turn Order dictionary
            Dictionary<UnitType, List<UnitInfo>> turnOrder = new Dictionary<UnitType, List<UnitInfo>>();
            for (int i = 2; i < 10; i++)
            {
                UnitType type = (UnitType)i;
                List<UnitInfo> list = new List<UnitInfo>();
                turnOrder.Add(type, list);
            }
            //Add enemies to their specific list in the turn order
            foreach(EditorUnit character in enemySpawns)
            {
                turnOrder[character.Type].Add(character.GetInfo());
            }

            //Add enemy lists into spawns based on their turn order
            int[] types = {4,2,9,3,5,6,8,7};
            foreach(int index in types)
            {
                UnitType type = (UnitType)index;
                spawns.AddRange(turnOrder[type]);
            }

            List<LevelObjectInfo> objects = new List<LevelObjectInfo>();
            foreach(EditorLevelObject levelObject in levelObjects)
            {
                objects.Add(levelObject.GetInfo());
            }

            GridInfo info = new GridInfo(savefileName, gridParams.type, parameters, tiles, spawns, objects);
            string finalFileName = JsonSaveLoad.SaveFile<GridInfo>(FolderPath.Grids, info, savefileName, false, true);
            Debug.Log("Saved new Grid, [" + finalFileName + "]");
            UpdateLoadableFiles();
            currentGridFile = savefileName;
        }
        else
        {
            Debug.LogWarning("WARNING: Trying to save empty grid!");
        }
    }

    /// <summary>Loads the chosen grid.</summary>
    public void LoadGrid()
    {
        if (loadFileName == null)
        {
            Debug.LogWarning("WARNING: Load file has not been set!");
            return;
        }

        GridInfo info = JsonSaveLoad.LoadFile<GridInfo>(FolderPath.Grids, loadFileName);

        RemoveGrid();
        gridParams.type = info.type;
        switch (info.type)
        {
            case GridType.Radial:
            {
                if (info.parameters != null && info.parameters.Length == 1 && info.parameters[0] > 0)
                {
                    gridParams.radius = info.parameters[0];
                    CreateRadialGrid(info.GetTiles(), info.GetSpawns(), info.GetLevelObjects());
                }
                break;
            }
            case GridType.Square:
            {
                if (info.parameters != null && info.parameters.Length == 2 && info.parameters[0] > 0 && info.parameters[1] > 0)
                {
                    gridParams.height = info.parameters[0];
                    gridParams.width = info.parameters[1];
                    CreateSquareGrid(info.GetTiles(), info.GetSpawns(), info.GetLevelObjects());
                }
                break;
            }
        }

        onLevelLoad.Invoke();

        CameraManager.Instance.UpdateBounds(gridParams.type, info.parameters);
        currentGridFile = loadFileName;
        savefileName = loadFileName;
    }

    /// <summary>Sets the grid to be loaded.</summary>
    /// <param name="index">Index of the chosen grid from the list of loadable grids.</param>
    public void SetLoadFile(int index)
    {
        if (loadableFiles == null || index < 0 || index > loadableFiles.Count)
        {
            Debug.LogError("Error: Index for Loadable file");
            return;
        }
        loadFileName = loadableFiles[index];
    }

    /// <summary>Updates the list of loadable files.</summary>
    private void UpdateLoadableFiles()
    {
        loadableFiles = JsonSaveLoad.FindAllFiles(FolderPath.Grids);
        if (loadableFiles != null && loadableFiles.Count > 0)
        {
            if (loadFileName == null || !loadableFiles.Contains(loadFileName))
            {
                loadFileName = loadableFiles[0];
            }
        }
        else
        {
            loadFileName = null;
        }
    }
    #endregion

    /// <summary>Sets the grid to be loaded.</summary>
    /// <param name="index">Index of the chosen grid from the list of loadable grids.</param>
    public EditorTile GetTile(Vector2Int gridPosition)
    {
        if (CheckTilePosition(gridPosition))
        {
            return grid[gridPosition.x].Get(gridPosition.y);
        }
        else
        {
            return null;
        }
    }

    /// <summary>Sets removed tile buttons as active based on given value.</summary>
    /// <param name="active">value that sets the buttons as active or inactive.</param>
    public void SetButtonsActive(bool active)
    {
        foreach (GameObject button in removedTileButtons)
        {
            button.SetActive(active);
        }
    }

    /// <summary>Checks to see if given tile position exists.</summary>
    /// <param name="position">Tile position to check.</param>
    private bool CheckTilePosition(Vector2Int position)
    {
        if (position == null || grid == null || grid.Count == 0)
        {
            return false;
        }

        int x = position.x; int y = position.y;
        return ((x >= 0 && x < grid.Count) && (y >= 0 && y < grid[x].Count));
    }

    /// <summary>Checks if given grid Indexs matches given tile position (used when loading grids).</summary>
    /// <param name="gridIndex">Index of the tile in its row.</param>
    /// <param name="gridListIndex">Index of the row of the tile in the grid.</param>
    /// <param name="position">Position of the tile to be checked.</param>
    private bool MatchSpace(int gridIndex, int gridListIndex, Vector2Int position)
    {
        return (gridIndex == position.x && gridListIndex == position.y);
    }
}
