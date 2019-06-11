using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// #CM -    This script is in charge of managing our entire plug-in as well as the generation of terrain specifically.
//          Keeping this script tidy will be paramount to plug-in readability.

// We execute all of our scripts in editor
[ExecuteInEditMode]
public class GenerationManager : MonoBehaviour
{
    public List<GenBlock> mGenBlocks;

    // Project Settings
    public bool mGenerate;
    public GameObject mGenerationTarget;
    public EnvironmentData mEnvironmentDatabase;

    // Grid Settings
    public float mGridSize;

    // Genblock Settings
    public GameObject mGenBlockPrefab;
    public bool mAddGenBlock;

    public int mStairCount;

    public int mMaxGenBlockSize;

    public Color mGenBlockColour = Color.white;
    public Color mGenBlockIsolatedColour = Color.red;
    public Color mGenBlockSelectedColour = Color.green;
    public Color mGenBlockLockedColour = Color.gray;

    // Dangerous Buttons
    public bool mClearGenBlocks;
    public bool mClearTerrain;

    private GameObject mEnvironmentParent;

    private List<GameObject> mFloorParents;
    
    void Update()
    {
        if(mGenerate)
        {
            mGenerate = false;
            GenerateTerrain();
        }

        if(mAddGenBlock)
        {
            mAddGenBlock = false;
            AddGenBlock();
        }

        if(mClearGenBlocks)
        {
            mClearGenBlocks = false;
            ClearGenBlocks();
        }

        if(mClearTerrain)
        {
            mClearTerrain = false;
            ClearTerrain();
        }
    }
    
    private void GenerateTerrain()
    {
        // Find how many floors we have.
        int floorCount = 0;

        foreach(GenBlock block in mGenBlocks)
        {
            // Check to see we're not in another blocks space, if we are then delete us
            for(int i = 0; i < mGenBlocks.Count; i++)
            {
                if(mGenBlocks[i] == block)
                {
                    continue;
                }

                if(mGenBlocks[i].gameObject.transform.position == block.transform.position)
                {
                    DestroyImmediate(block);
                    break;
                }
            }

            if(block == null)
            {
                continue;
            }

            // Un-parent the GenBlock
            block.transform.SetParent(mGenerationTarget.transform);

            // Get our scene Height, we need to bump it by one because we're starting at 0.
            int height = (int)block.gameObject.transform.position.y / mMaxGenBlockSize;

            // Need to increase hight by 1 in our calculations to allot for generations where there is only 1 floor
            if(height + 1 > floorCount)
            {
                floorCount = height + 1;
            }

            // Set the floor level of our block
            block.mCurrentFloorLevel = height;

            // If our block isn't locked down, wipe the blocks geometry
            block.RemoveTerrain();
        }

        // Reset the floor parents
        if (mFloorParents == null)
        {
            // Create the holder objects for each floor.
            mFloorParents = new List<GameObject>();
        }
        else
        {
            foreach (GameObject obj in mFloorParents)
            {
                DestroyImmediate(obj);
            }

            mFloorParents.Clear();
        }

        for (int i = mFloorParents.Count; i < floorCount; i++)
        {
            GameObject newParent = new GameObject("Floor " + (i + 1).ToString());
            newParent.transform.SetParent(mGenerationTarget.transform);

            mFloorParents.Add(newParent);
        }

        // Set our floor parent heights, this is mainly just for aesthetics, could be removed if we're hurting for cycles.
        for(int i = 0; i < mFloorParents.Count; i++)
        {
            mFloorParents[i].transform.position = new Vector3(0, (i * mMaxGenBlockSize) - (mMaxGenBlockSize / 2), 0);
        }

        // Generate Map
        int currentStairCount = 0;
        GenBlock[] currentBlocks = null;
        foreach (GenBlock block in mGenBlocks)
        {
            // Don't generate the terrain for Isolated GenBlocks
            if(block.mSpacialData.mIsolated)
            {
                continue;
            }

            block.transform.SetParent(mFloorParents[block.mCurrentFloorLevel].transform);
            RoomAndRotation room = block.GetRandomRoom();

            if(room.mRoom != null)
            {
                GameObject roomObject = Instantiate(room.mRoom);
                roomObject.transform.parent = block.transform;
                roomObject.transform.localPosition = Vector3.zero;

                if(room.mRotateOnY != 0)
                {
                    roomObject.transform.rotation *= Quaternion.Euler(0, room.mRotateOnY, 0);
                }
            }
        }
    }

    private void AddGenBlock()
    {
        // Generate new block
        GameObject newObj = Instantiate(mGenBlockPrefab);
        
        // Set up the block
        newObj.name = "GenBlock";
        newObj.transform.position = new Vector3(0, 0, 0);
        newObj.transform.SetParent(mGenerationTarget.transform);

        mGenBlocks.Add(newObj.GetComponent<GenBlock>());
    }

    private void ClearGenBlocks()
    {
        for(int i = mGenBlocks.Count -1; i >= 0; i--)
        {
            DestroyImmediate(mGenBlocks[i].gameObject);
        }

        mGenBlocks.Clear();
    }

    private void ClearTerrain()
    {
        foreach(GenBlock block in mGenBlocks)
        {
            block.RemoveTerrain();
        }
    }
}
