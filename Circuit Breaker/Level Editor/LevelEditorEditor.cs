/*
 * LevelEditorEditor.cs
 * Scott Duman
 * Allows Designers to use Unity's Editor to alter teh properties of the Grid Creator script
 */
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    //Circuit Editor Variables
    bool gridSaving = true, gridLoading = true, circuitCreator = true, newspaperInfo = true;
    bool postCircuitStory = true, circuitSaving = true, circuitLoading = true;
    string circuitFileName = "CircuitFile";
    string circuitName = "New Circuit";
    Difficulty circuitDifficulty = Difficulty.Normal;
    CircuitTier circuitTier = CircuitTier.Tier1;
    List<string> circuitLevels = new List<string>();
    string date = "August 8th, 2008";
    string headline = "Fresh Meat Enters the Circuit Breaker Sport!";
    string articleTitle = "An Actuual Challenge";
    string articleDescription = "The new challenger will actually have to try in order to overcome this challenge. I hope they survive... or at least show us a spectacular death!";
    bool hasStoryScene = false, finalCircuit = false;
    string storyHeader = "";
    string storyDescription = "";
    List<string> loadableCircuits = new List<string>();
    int circuitIndex = 0;

    private void Awake()
    {
        //FindCircuits();
        loadableCircuits = JsonSaveLoad.FindAllFiles(FolderPath.Circuits);
    }

    public override void OnInspectorGUI() 
    {
        base.OnInspectorGUI();
        LevelEditor manager = (LevelEditor)target;
        GridType[] types = new GridType[] {GridType.Radial, GridType.Square};

        if (Application.isPlaying)
        {
            if (CanSave(manager))
            {
                gridSaving = EditorGUILayout.BeginFoldoutHeaderGroup(gridSaving, "Grid Saving");
                if (gridSaving)
                {
                    manager.savefileName = EditorGUILayout.TextField("Save File Name", manager.savefileName);
                    if (GUILayout.Button("Save"))
                    {
                        manager.SaveGrid();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else if (manager.LoadableFiles != null && manager.LoadableFiles.Count > 0)
            {
                gridLoading = EditorGUILayout.BeginFoldoutHeaderGroup(gridLoading, "Grid Loading");
                if (gridLoading)
                {
                    int index = manager.LoadableFiles.IndexOf(manager.loadFileName);
                    manager.SetLoadFile(EditorGUILayout.Popup(index, manager.LoadableFiles.ToArray()));
                    if (GUILayout.Button("Load"))
                    {
                        manager.LoadGrid();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            circuitCreator = EditorGUILayout.BeginFoldoutHeaderGroup(circuitCreator, "Circuit Creator");
            if (circuitCreator)
            {
                circuitName = EditorGUILayout.TextField("Circuit Name", circuitName);
                circuitDifficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", circuitDifficulty);
                circuitTier = (CircuitTier)EditorGUILayout.EnumPopup("Circuit Teir", circuitTier);
                finalCircuit = EditorGUILayout.Toggle("Final Circuit", finalCircuit);

                for(int i = circuitLevels.Count - 1; i > -1; i--)
                {
                    if (GUILayout.Button("Remove " + circuitLevels[i]))
                    {
                        circuitLevels.RemoveAt(i);
                    }
                }

                GUILayout.Space(20);
                if (manager.currentGridFile != null && circuitLevels.Count < 7 && !circuitLevels.Contains(manager.currentGridFile))
                {
                    if (GUILayout.Button("Add Current Grid"))
                    {
                        circuitLevels.Add(manager.currentGridFile);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            newspaperInfo = EditorGUILayout.BeginFoldoutHeaderGroup(newspaperInfo, "NewspaperInfo");
            if (newspaperInfo)
            {
                date = EditorGUILayout.TextField("Date", date);
                headline = EditorGUILayout.TextField("Headline", headline);
                articleTitle = EditorGUILayout.TextField("Article Title", articleTitle);
                EditorGUILayout.LabelField("Article Description");
                articleDescription = EditorGUILayout.TextArea(articleDescription, GUILayout.Height(60));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            postCircuitStory = EditorGUILayout.BeginFoldoutHeaderGroup(postCircuitStory, "Post Circuit Story");
            if (postCircuitStory)
            {
                hasStoryScene = GUILayout.Toggle(hasStoryScene, "Has Story");
                storyHeader = EditorGUILayout.TextField("Story Header", storyHeader);
                GUILayout.Label("Story Scene Description");
                storyDescription = EditorGUILayout.TextArea(storyDescription, GUILayout.Height(60));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (loadableCircuits.Count > 0)
            {
                circuitLoading = EditorGUILayout.BeginFoldoutHeaderGroup(circuitLoading, "Circuit Loading");
                if (circuitLoading)
                {
                    circuitIndex = EditorGUILayout.Popup(circuitIndex, loadableCircuits.ToArray());
                    if (GUILayout.Button("Load"))
                    {
                        Circuit circuit = JsonSaveLoad.LoadFile<Circuit>(FolderPath.Circuits, loadableCircuits[circuitIndex]);
                        InfoList<NewspaperInfo> newsList = JsonSaveLoad.LoadResource<InfoList<NewspaperInfo>>("Newspaper");

                        circuitFileName = loadableCircuits[circuitIndex];
                        circuitName = circuit.name;
                        circuitDifficulty = circuit.difficulty;
                        circuitTier = circuit.circuitTier;
                        finalCircuit = circuit.IsFinalCircuit();
                        circuitLevels.Clear();
                        circuitLevels.AddRange(circuit.grids);
                        
                        bool missingNewspaper = true;
                        if (newsList != default(InfoList<NewspaperInfo>))
                        {
                            NewspaperInfo newsInfo = NewspaperInfo.BinarySearch(newsList.info, circuitName);
                            if (newsInfo != null)
                            {
                                missingNewspaper = false;
                                date = newsInfo.date;
                                headline = newsInfo.headline;
                                articleTitle = newsInfo.title;
                                articleDescription = newsInfo.description;
                            }
                        }

                        if (missingNewspaper)
                        {
                            date = "August 8th, 2008";
                            headline = "Fresh Meat Enters the Circuit Breaker Sport!";
                            articleTitle = "An Actual Challenge";
                            if (circuit.description != null)
                            {
                                articleDescription = circuit.description;
                            }
                            else
                            {
                                articleDescription = "The new challenger will actually have to try in order to overcome this challenge."
                                + " I hope they survive... or at least show us a spectacular death!";
                            }
                        }

                        if (circuit.postStoryScene != null && circuit.postStoryScene.Length >= 2)
                        {
                            hasStoryScene = true;
                            storyHeader = circuit.postStoryScene[0];
                            storyDescription = circuit.postStoryScene[1];
                        }
                        else
                        {
                            hasStoryScene = false;
                            storyHeader = "";
                            storyDescription = "";
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            if (circuitLevels.Count > 0)
            {
                circuitSaving = EditorGUILayout.BeginFoldoutHeaderGroup(circuitSaving, "Circuit Saving");
                if (circuitSaving)
                {
                    circuitFileName = EditorGUILayout.TextField("Circuit File Name", circuitFileName);
                    if (GUILayout.Button("Save Circuit"))
                    {   
                        string[] story = null;
                        if (hasStoryScene)
                        {
                            story = new string[2];
                            story[0] = storyHeader;
                            story[1] = storyDescription;
                        }

                        Circuit circuit = new Circuit(circuitName, articleDescription, circuitDifficulty, circuitTier, finalCircuit, circuitLevels.ToArray(), story);
                        string savedName = JsonSaveLoad.SaveFile(FolderPath.Circuits, circuit, circuitFileName, false, true);

                        //Get Newspaper Info and convert it into a list 
                        NewspaperInfo info = new NewspaperInfo(circuitName, date, headline, articleTitle, articleDescription);
                        InfoList<NewspaperInfo> newsInfo = JsonSaveLoad.LoadResource<InfoList<NewspaperInfo>>("Newspaper");

                        //Add new newspaper info into the list 
                        newsInfo.info = NewspaperInfo.BinaryInsert(newsInfo.info, info);

                        //Overwrite previous newspaper list with new data
                        JsonSaveLoad.SaveResource<InfoList<NewspaperInfo>>(newsInfo, "Newspaper");

                        Debug.Log("Saved Circuit as [" + savedName + "]");
                        loadableCircuits = JsonSaveLoad.FindAllFiles(FolderPath.Circuits);
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    private bool CanSave(LevelEditor manager)
    {

        bool teleportTiles = true;
        foreach(List<EditorTile> teleporters in manager.teleportTiles.Values)
        {
            if (teleporters.Count == 1)
            {
                teleportTiles = false;
                break;
            }
        }
        bool characterSpawns = (manager.enemySpawns != null && manager.enemySpawns.Count > 0);
        return (manager.player != null && characterSpawns && teleportTiles);
    }
}
#endif