using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Export))]
public class ExportGui : Editor
{

    public override void OnInspectorGUI()
    {
        // Get our block
        Export export = (Export)target;

        // Set up our label fonts
        GUIStyle header = new GUIStyle();
        header.fontStyle = FontStyle.Bold;
        header.fontSize = 12;
        header.alignment = TextAnchor.UpperCenter;
        header.wordWrap = true;

        GUIStyle boldBody = new GUIStyle();
        boldBody.fontStyle = FontStyle.Bold;
        boldBody.fontSize = 11;
        boldBody.wordWrap = true;

        GUIStyle body = new GUIStyle();
        body.fontSize = 11;
        body.wordWrap = true;

        // Start our layout
        GUILayout.Space(10);

        GUILayout.Label("Export Settings", header);

        GUILayout.Space(5);

        GUILayout.Label("This script is used to handle all information based around exporting your generated environment, as well as allowing the user to export the environment to their specified URL.", body);

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        serializedObject.FindProperty("mExport").boolValue = GUILayout.Button("Export Terrain");
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        
        GUILayout.Label("Export Path", boldBody);
        GUILayout.Space(2);
        serializedObject.FindProperty("mExportURL").stringValue = EditorGUILayout.TextArea(export.mExportURL);
        GUILayout.Space(5);

        GUILayout.Label("Export Name", boldBody);
        GUILayout.Space(2);
        serializedObject.FindProperty("mExportName").stringValue = EditorGUILayout.TextArea(export.mExportName);
        GUILayout.Space(5);

        serializedObject.FindProperty("mStripGenBlockSettings").boolValue = GUILayout.Toggle(export.mStripGenBlockSettings , "Strip Scripts from Export");

        // Apply changes
        serializedObject.ApplyModifiedProperties();
    }
}
