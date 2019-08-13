using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerationManager))]
public class GenerationGui : Editor
{
    // Serialised Properties
    public override void OnInspectorGUI()
    {
        // Get our block
        serializedObject.Update();
        GenerationManager manager = (GenerationManager)target;

        // Set up our label fonts
        GUIStyle header = new GUIStyle();
        header.fontStyle = FontStyle.Bold;
        header.fontSize = 12;
        header.alignment = TextAnchor.UpperCenter;
        header.wordWrap = true;

        GUIStyle body = new GUIStyle();
        body.fontSize = 11;
        body.wordWrap = true;

        GUIStyle boldBody = new GUIStyle();
        boldBody.fontStyle = FontStyle.Bold;
        boldBody.fontSize = 11;
        boldBody.wordWrap = true;

        // Start our layout
        GUILayout.Space(10);

        GUILayout.Label("Generation Manager", header);
        GUILayout.Space(5);

        GUILayout.Label("This script is the main area where you can control the plug-in. It allows you to generate an entirely new area in the created GenBlocks, add a new GenBlock to the scene, as well as a host of other useful features to help with generation.", body);
        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        serializedObject.FindProperty("mGenerate").boolValue = GUILayout.Button("Generate Terrain");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        GUILayout.Label("Project Settings", boldBody);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Environment Target", body);
        serializedObject.FindProperty("mGenerationTarget").objectReferenceValue = (GameObject)EditorGUILayout.ObjectField(manager.mGenerationTarget, typeof(GameObject), true);
        GUILayout.EndHorizontal();
        GUILayout.Space(2.5f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Environment Data", body);
        serializedObject.FindProperty("mEnvironmentDatabase").objectReferenceValue = (EnvironmentData)EditorGUILayout.ObjectField(manager.mEnvironmentDatabase, typeof(EnvironmentData), false);
        GUILayout.EndHorizontal();
        GUILayout.Space(2.5f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Use Maze Generation", body);
        serializedObject.FindProperty("mUseMaze").boolValue = EditorGUILayout.Toggle(serializedObject.FindProperty("mUseMaze").boolValue);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        serializedObject.FindProperty("mAddGenBlock").boolValue = GUILayout.Button("Add GenBlock to Scene");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);

        GUILayout.Label("GenBlock Settings", boldBody);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("GenBlock Prefab", body);
        serializedObject.FindProperty("mGenBlockPrefab").objectReferenceValue = (GameObject)EditorGUILayout.ObjectField(manager.mGenBlockPrefab, typeof(GameObject), false);
        GUILayout.EndHorizontal();
        GUILayout.Space(5f);

        // Stair Count
        GUILayout.BeginHorizontal();
        GUILayout.Label("Stair Count per Floor", body);
        serializedObject.FindProperty("mStairCount").intValue = EditorGUILayout.IntField(manager.mStairCount);
        GUILayout.EndHorizontal();
        GUILayout.Space(5f);

        // GenBlock Colours
        GUILayout.Label("GenBlock WireFrame Colours", boldBody);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Locked", body);
        serializedObject.FindProperty("mGenBlockLockedColour").colorValue = EditorGUILayout.ColorField(manager.mGenBlockLockedColour);
        GUILayout.EndHorizontal();
        GUILayout.Space(2.5f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Regular", body);
        serializedObject.FindProperty("mGenBlockColour").colorValue = EditorGUILayout.ColorField(manager.mGenBlockColour);
        GUILayout.EndHorizontal();
        GUILayout.Space(2.5f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Selected", body);
        serializedObject.FindProperty("mGenBlockSelectedColour").colorValue = EditorGUILayout.ColorField(manager.mGenBlockSelectedColour);
        GUILayout.EndHorizontal();
        GUILayout.Space(2.5f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Isolated", body);
        serializedObject.FindProperty("mGenBlockIsolatedColour").colorValue = EditorGUILayout.ColorField(manager.mGenBlockIsolatedColour);
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);
        
        GUILayout.Label("Danger Settings", boldBody);
        GUILayout.Space(5);

        GUILayout.Label("These settings once played with will alter your scene drastically, use them with caution", body);
        GUILayout.Space(5);

        GUI.backgroundColor = Color.red;
        serializedObject.FindProperty("mClearGenBlocks").boolValue = GUILayout.Button("Remove all GenBlocks from the scene");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);

        GUI.backgroundColor = Color.red;
        serializedObject.FindProperty("mClearTerrain").boolValue = GUILayout.Button("Remove all Terrain from Unlocked GenBlocks");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);

        // Apply
        serializedObject.ApplyModifiedProperties();
    }
}
