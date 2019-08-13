using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// We execute all of our scripts in editor
[ExecuteInEditMode]
public class Export : MonoBehaviour
{
    public bool mExport;

    [Header("Export Tools")]
    public bool mStripGenBlockSettings;

    public string mExportURL = "../Exports/";
    public string mExportName = "Environment";

    // Update is called once per frame
    void Update()
    {
        if(mExport)
        {
            ExportEnvironment();
            mExport = false;
        }
    }

    private void ExportEnvironment()
    {
        // Create the correct GameObject
        GameObject exportObj = GameObject.Instantiate(GameObject.FindObjectOfType<GenerationManager>().mGenerationTarget);
        exportObj.name = mExportName;

        if (mStripGenBlockSettings)
        {
            // Loop the environment
            foreach(Transform floor in exportObj.transform)
            {
                // Loop Each floor
                foreach(Transform block in floor.transform)
                {
                    // Get Terrain
                    if(block.childCount > 0)
                    {
                        // Re parent the geometry from the GenBlock to the Floor
                        foreach (Transform room in block.transform)
                        {
                            room.parent = floor;

                            foreach (Room roomScript in room.GetComponentsInChildren<Room>())
                            {
                                // Remove the "Room" scripts from the terrain
                                DestroyImmediate(roomScript);
                            }
                        }
                    }

                    // Delete the GenBlock
                    DestroyImmediate(block.gameObject);
                }
            }
        }
        
        // #IMPROVEMENT - It would be nice to have a full popup window system to help the user save out their prefabs
        // Export the "Environment" gameobject as a prefab
        if(AssetDatabase.IsValidFolder(mExportURL))
        {
            string fullName = mExportURL + "/" + mExportName + ".prefab";
            if (!AssetDatabase.LoadAssetAtPath(fullName, typeof(GameObject)))
            {
                PrefabUtility.SaveAsPrefabAsset(exportObj, fullName);
                Debug.Log("Export Success");
            }
            else
            {
                Debug.Log("File asset already exists!");
            }
        }
        else
        {
            Debug.Log("Export path doesn't exist!");
        }
    }
}
