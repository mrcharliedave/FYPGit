using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenBlock))]
public class GenBlockGui : Editor
{
    public override void OnInspectorGUI()
    {
        // Get our block
        serializedObject.Update();
        GenBlock myBlock = (GenBlock)target;

        // Set up our label fonts
        GUIStyle header = new GUIStyle();
        header.fontStyle = FontStyle.Bold;
        header.fontSize = 12;
        header.alignment = TextAnchor.UpperCenter;
        header.wordWrap = true;

        GUIStyle body = new GUIStyle();
        body.fontSize = 11;
        body.wordWrap = true;

        // Start our layout
        GUILayout.Space(10);

        GUILayout.Label("GenBlock Settings", header);

        GUILayout.Space(5);

        GUILayout.Label("This script is used to set the specifics of this current GenBlock. Wider options can be found in the Generation Manager script.", body);

        GUILayout.Space(5);

        // Lock Bool
        GUI.backgroundColor = serializedObject.FindProperty("mLock").boolValue ? Color.red : Color.green; // Set the colour depending on our locked status
        if (GUILayout.Button(serializedObject.FindProperty("mLock").boolValue ? "Unlock GenBlock Terrain" : "Lock Genblock Terrain"))
        {
            serializedObject.FindProperty("mLock").boolValue = !serializedObject.FindProperty("mLock").boolValue;
        }

        // Gizmo Bool
        GUI.backgroundColor = serializedObject.FindProperty("mDrawSpacialDataGizmos").boolValue ? Color.red : Color.green; // Set the colour depending on our locked status
        if (GUILayout.Button(serializedObject.FindProperty("mDrawSpacialDataGizmos").boolValue ? "Hide Spacial Data Gizmos" : "Draw Spacial Data Gizmos"))
        {
            serializedObject.FindProperty("mDrawSpacialDataGizmos").boolValue = !serializedObject.FindProperty("mDrawSpacialDataGizmos").boolValue;
        }

        GUI.backgroundColor = Color.white; // Reset the GUI Colour

        serializedObject.ApplyModifiedProperties();
    }
}
