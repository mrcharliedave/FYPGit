﻿using System.Collections;
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
    public bool mUseMaze = true;
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

    /// <summary>
    ///  Items below this line are specifically for internal processes. Not to be displayed in the custom inspector.
    /// </summary>
    public List<List<GenBlockSpacialProperties>> mRoomRotations;

    private GameObject mEnvironmentParent;
    private List<GameObject> mFloorParents;
    

    void Update()
    {
        if(mRoomRotations == null)
        {
            SetupRoomRotations();
        }

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

        // Clear any current gen blocks
        floorCount = PrepareGenblocks();

        // Clear our current floors
        ClearFloors();

        // Rebuild the floor hierarchy
        BuildFloors(floorCount);

        // Generate our map
        GenerateMap();
    }

    private void BuildFloors(int numFloors)
    {

        for (int i = mFloorParents.Count; i < numFloors; i++)
        {
            GameObject newParent = new GameObject("Floor " + (i + 1).ToString());
            newParent.transform.SetParent(mGenerationTarget.transform);

            mFloorParents.Add(newParent);
        }

        // Set our floor parent heights, this is mainly just for aesthetics, could be removed if we're hurting for cycles.
        for (int i = 0; i < mFloorParents.Count; i++)
        {
            mFloorParents[i].transform.position = new Vector3(0, (i * mMaxGenBlockSize) - (mMaxGenBlockSize / 2), 0);
        }
    }

    private void ClearFloors()
    {
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
    }

    // Prepares all Genblocks for generation, returns the number of floors
    private int PrepareGenblocks()
    {
        int floors = 0;

        foreach (GenBlock block in mGenBlocks)
        {
            // Check to see we're not in another blocks space, if we are then delete us
            for (int i = 0; i < mGenBlocks.Count; i++)
            {
                if (mGenBlocks[i] == block)
                {
                    continue;
                }

                if (mGenBlocks[i].gameObject.transform.position == block.transform.position)
                {
                    DestroyImmediate(block);
                    break;
                }
            }

            if (block == null)
            {
                continue;
            }

            // Un-parent the GenBlock
            block.transform.SetParent(mGenerationTarget.transform);

            // Get our scene Height, we need to bump it by one because we're starting at 0.
            int height = (int)block.gameObject.transform.position.y / mMaxGenBlockSize;

            // Need to increase hight by 1 in our calculations to allot for generations where there is only 1 floor
            if (height + 1 > floors)
            {
                floors = height + 1;
            }

            // Set the floor level of our block
            block.mCurrentFloorLevel = height;

            // If our block isn't locked down, wipe the blocks geometry
            block.RemoveTerrain();
        }

        return floors;
    }

    private bool GenerateMap()
    {
        // Find a GenBlock on the lowest level
        GenBlock startBlock = null;
        foreach(GenBlock block in mGenBlocks)
        {
            // Don't generate the terrain for Isolated GenBlocks
            if (block.mSpacialData.mIsolated)
            {
                mGenBlocks.Remove(block);
                DestroyImmediate(block);
                continue;
            }

            // Reset each blocks data
            block.mSpacialData.mDeadEnd = false;
            block.mSpacialData.mVisited = false;
            block.mSpacialData.mUsedPaths = 0;

            // Find if we're the start block
            if (!startBlock)
            {
                startBlock = block;
            }
            else if (block.transform.position.y < startBlock.transform.position.y)
            {
                startBlock = block;
            }
        }

        if(mUseMaze)
        {
            GenerateMaze(startBlock);
        }
        else
        {
            GenerateOpenPlan();
        }

        return true;
    }

    // Generate our terrain using a backwards recursive search to define the paths.
    private void GenerateMaze(GenBlock startBlock)
    {
        // Begin Recursive Backtracking
        Stack<GenBlock> blockStack = new Stack<GenBlock>();
        blockStack.Push(startBlock);
        while (blockStack.Count > 0)
        {
            // Pop our current block and process its spacial data
            GenBlock block = blockStack.Pop();
            if (!block.mSpacialData.mDeadEnd)
            {
                // Now that the block is popped, we have visited here
                block.mSpacialData.mVisited = true;

                // Check for available directions
                var candidatesDirections = GetViableDirections(block);
                if (candidatesDirections.Count > 0)
                {
                    // Get a random Direction
                    var direction = candidatesDirections[Random.Range(0, candidatesDirections.Count - 1)];

                    // Set our used path in this direction
                    block.mSpacialData.mUsedPaths |= direction;

                    // Set our new rooms used path in the opposite direction
                    block.mSpacialData.mNeighbours[direction].mSpacialData.mUsedPaths |= GetOppositeDirection(direction);

                    // Add our current block and then our next block to the stack
                    blockStack.Push(block);
                    blockStack.Push(block.mSpacialData.mNeighbours[direction]);
                }
                else
                {
                    // We have nowhere to go, so we are a dead end
                    block.mSpacialData.mDeadEnd = true;
                }
            }

            // We're a dead end, so generate our terrain
            if (block.mSpacialData.mDeadEnd)
            {
                GenerateBlockTerrain(block);
            }
        }
    }

    private void GenerateOpenPlan()
    {
        // Generate a path in every available direction
        foreach (GenBlock block in mGenBlocks)
        {
            block.mSpacialData.mUsedPaths = block.mSpacialData.mProperties;
            GenerateBlockTerrain(block);
        }
    }

    private void GenerateBlockTerrain(GenBlock block)
    {
        block.transform.SetParent(mFloorParents[block.mCurrentFloorLevel].transform);
        RoomAndRotation room = block.GetRandomRoom();

        if (room.mRoom != null)
        {
            GameObject roomObject = Instantiate(room.mRoom);
            roomObject.transform.parent = block.transform;
            roomObject.transform.localPosition = Vector3.zero;

            if (room.mRotateOnY != 0)
            {
                roomObject.transform.rotation *= Quaternion.Euler(0, room.mRotateOnY, 0);
            }
        }
    }
    
    private GenBlockSpacialProperties GetOppositeDirection(GenBlockSpacialProperties direction)
    {
        switch (direction)
        {
            case (GenBlockSpacialProperties.FORWARD):
                {
                    return GenBlockSpacialProperties.BACK;
                }
            case (GenBlockSpacialProperties.BACK):
                {
                    return GenBlockSpacialProperties.FORWARD;
                }
            case (GenBlockSpacialProperties.LEFT):
                {
                    return GenBlockSpacialProperties.RIGHT;
                }
            case (GenBlockSpacialProperties.RIGHT):
                {
                    return GenBlockSpacialProperties.LEFT;
                }
            case (GenBlockSpacialProperties.UP):
                {
                    return GenBlockSpacialProperties.DOWN;
                }
            case (GenBlockSpacialProperties.DOWN):
                {
                    return GenBlockSpacialProperties.UP;
                }
        }

        return 0;

    }

    // Finds directions for the recursive search, only for rooms that haven't already been visited
    private List<GenBlockSpacialProperties> GetViableDirections(GenBlock block)
    {
        List<GenBlockSpacialProperties> directions = new List<GenBlockSpacialProperties>();
        foreach (var pair in block.mSpacialData.mNeighbours)
        {
            if(!pair.Value.mSpacialData.mVisited && !pair.Value.mSpacialData.mDeadEnd)
            {
                directions.Add(pair.Key);
            }
        }

        return directions;
    }

    private void SetupRoomRotations()
    {
        // Add rotations in a clockwise manner
        mRoomRotations = new List<List<GenBlockSpacialProperties>>();
        List<GenBlockSpacialProperties> OneFlatConnection = new List<GenBlockSpacialProperties>();
        List<GenBlockSpacialProperties> LineConnection = new List<GenBlockSpacialProperties>();
        List<GenBlockSpacialProperties> TwoFlatConnection = new List<GenBlockSpacialProperties>();
        List<GenBlockSpacialProperties> ThreeFlatConnection = new List<GenBlockSpacialProperties>();

        // If we have 1 entrance, the room matches, just apply a rotation, 4 types
        OneFlatConnection.Add(GenBlockSpacialProperties.FORWARD);
        OneFlatConnection.Add(GenBlockSpacialProperties.RIGHT);
        OneFlatConnection.Add(GenBlockSpacialProperties.BACK);
        OneFlatConnection.Add(GenBlockSpacialProperties.LEFT);

        // If we have 2 entrances
        // If the entrances are adjacent, rotate, 2 types
        LineConnection.Add(GenBlockSpacialProperties.FORWARD | GenBlockSpacialProperties.BACK);
        LineConnection.Add(GenBlockSpacialProperties.LEFT | GenBlockSpacialProperties.RIGHT);

        // If entrances are next to each other, rotate, 4 types
        TwoFlatConnection.Add(GenBlockSpacialProperties.FORWARD | GenBlockSpacialProperties.RIGHT);
        TwoFlatConnection.Add(GenBlockSpacialProperties.RIGHT | GenBlockSpacialProperties.BACK);
        TwoFlatConnection.Add(GenBlockSpacialProperties.BACK | GenBlockSpacialProperties.LEFT);
        TwoFlatConnection.Add(GenBlockSpacialProperties.LEFT | GenBlockSpacialProperties.FORWARD);

        // if we have 3 entrances are next to each other, rotate, 4 types
        ThreeFlatConnection.Add(GenBlockSpacialProperties.FORWARD | GenBlockSpacialProperties.RIGHT | GenBlockSpacialProperties.BACK);
        ThreeFlatConnection.Add(GenBlockSpacialProperties.RIGHT | GenBlockSpacialProperties.BACK | GenBlockSpacialProperties.LEFT);
        ThreeFlatConnection.Add(GenBlockSpacialProperties.BACK | GenBlockSpacialProperties.LEFT | GenBlockSpacialProperties.FORWARD);
        ThreeFlatConnection.Add(GenBlockSpacialProperties.LEFT | GenBlockSpacialProperties.FORWARD | GenBlockSpacialProperties.RIGHT);

        // Add all of our rotations to our list of lists
        mRoomRotations.Add(OneFlatConnection);
        mRoomRotations.Add(LineConnection);
        mRoomRotations.Add(TwoFlatConnection);
        mRoomRotations.Add(ThreeFlatConnection);
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
