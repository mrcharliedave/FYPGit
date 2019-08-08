using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// #CM -    This script handles the logic attached to our GenBlocks. These are the literal building blocks of our environment,
//          they will be responsible for managing themselves and reporting to the Generation Manager

[System.Flags]
public enum GenBlock2DSpacialProperties : int
{
    LEFT    = (1 << 0),
    RIGHT   = (1 << 1),
    FORWARD = (1 << 2),
    BACK    = (1 << 3),
}

public enum GenBlockLevelSpacialProperties
{
    NONE = 0,
    UP = 1,
    DOWN = 2
}

public struct SpacialData
{
    public GenBlock2DSpacialProperties mProperties;
    public GenBlockLevelSpacialProperties mLevelAccess;
    public bool mIsolated;
}

public struct RoomAndRotation
{
    public RoomAndRotation(GameObject obj, float rotate)
    {
        mRoom = obj;
        mRotateOnY = rotate;
    }

    public GameObject mRoom;
    public float mRotateOnY;
}

// We execute all of our scripts in editor
[ExecuteInEditMode]
public class GenBlock : MonoBehaviour
{
    private GenerationManager mGenerationManager;

    [Header("General Settings")]
    public bool mLock;

    [Tooltip("This block will specifically contain access stairs. Depending on this setting, the blocks above or below could be altered.")]
    public GenBlockLevelSpacialProperties mLevelAccess = GenBlockLevelSpacialProperties.NONE;

    [Header("Clutter Settings")]
    public bool mOverideGlobalClutter;

    [Tooltip("Controls how much clutter is allowed in this specific block.")]
    public bool mUseClutter;

    [HideInInspector]
    public int mCurrentFloorLevel;

    public SpacialData mSpacialData;

    private List<List<GenBlock2DSpacialProperties>> mRoomRotations;

    // Maybe set neighbours and do it this way??
    private GenBlock[,,] mNeighbours;

    private void OnDestroy()
    {
        if (mGenerationManager)
        {
            mGenerationManager.mGenBlocks.Remove(this);
        }
    }

    private void Awake()
    {
        mSpacialData = new SpacialData();
    }

    void Update()
    {
        // Grab the generation manager instance if we don't have it yet
        if(!mGenerationManager)
        {
            mGenerationManager = GameObject.FindObjectOfType<GenerationManager>();
        }

        if(mRoomRotations == null)
        {
            SetupRoomRotations();
        }

        // Run our block through its required updates
        Rescale();
        Reposition();
        UpdateSpacialProperties();
    }

    private void Rescale()
    {
        // Restrict our size to the one determined by the GenerationManager
        if (transform.localScale.x != mGenerationManager.mMaxGenBlockSize ||
            transform.localScale.y != mGenerationManager.mMaxGenBlockSize ||
            transform.localScale.z != mGenerationManager.mMaxGenBlockSize)
        {
            transform.localScale = new Vector3(mGenerationManager.mMaxGenBlockSize, mGenerationManager.mMaxGenBlockSize, mGenerationManager.mMaxGenBlockSize);
            gameObject.GetComponent<BoxCollider>().size = transform.localScale;
        }
    }

    private void Reposition()
    {
        // Update our local position
        Vector3 pos = transform.localPosition;

        float normalisedSize = 1.0f / mGenerationManager.mGridSize;

        pos.x = Mathf.Round(transform.localPosition.x * normalisedSize) / normalisedSize;
        pos.y = Mathf.Max(0, Mathf.Round(transform.localPosition.y * normalisedSize) / normalisedSize); // Need to pin our minimum Y pos to 0
        pos.z = Mathf.Round(transform.localPosition.z * normalisedSize) / normalisedSize;

        // Set Position
        transform.localPosition = pos;
    }

    private void UpdateSpacialProperties()
    {
        // Reset spacial data
        mSpacialData.mProperties = 0;

        // Cast out to find neighbours on the correct collision layer.
        int layerMask = 1 << 9;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.left, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlock2DSpacialProperties.LEFT;
        }
        if (Physics.Raycast(transform.position, Vector3.right, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlock2DSpacialProperties.RIGHT;
        }
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlock2DSpacialProperties.FORWARD;
        }
        if (Physics.Raycast(transform.position, Vector3.back, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlock2DSpacialProperties.BACK;
            hit.transform.GetComponent<GenBlock>();
        }

        // Set isolated if we are
        mSpacialData.mIsolated = mSpacialData.mProperties == 0 ? true : false;
    }

    public void RemoveTerrain(bool ignoreLock = false)
    {
        if(mLock && !ignoreLock)
        {
            return;
        }

        foreach(Transform child in gameObject.transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private void SetupRoomRotations()
    {
        // Add rotations in a clockwise manner
        mRoomRotations = new List<List<GenBlock2DSpacialProperties>>();
        List<GenBlock2DSpacialProperties> OneFlatConnection = new List<GenBlock2DSpacialProperties>();
        List<GenBlock2DSpacialProperties> LineConnection = new List<GenBlock2DSpacialProperties>();
        List<GenBlock2DSpacialProperties> TwoFlatConnection = new List<GenBlock2DSpacialProperties>();
        List<GenBlock2DSpacialProperties> ThreeFlatConnection = new List<GenBlock2DSpacialProperties>();

        // If we have 1 entrance, the room matches, just apply a rotation, 4 types
        OneFlatConnection.Add(GenBlock2DSpacialProperties.FORWARD);
        OneFlatConnection.Add(GenBlock2DSpacialProperties.RIGHT);
        OneFlatConnection.Add(GenBlock2DSpacialProperties.BACK);
        OneFlatConnection.Add(GenBlock2DSpacialProperties.LEFT);

        // If we have 2 entrances
        // If the entrances are adjacent, rotate, 2 types
        LineConnection.Add(GenBlock2DSpacialProperties.FORWARD | GenBlock2DSpacialProperties.BACK);
        LineConnection.Add(GenBlock2DSpacialProperties.LEFT | GenBlock2DSpacialProperties.RIGHT);

        // If entrances are next to each other, rotate, 4 types
        TwoFlatConnection.Add(GenBlock2DSpacialProperties.FORWARD | GenBlock2DSpacialProperties.RIGHT);
        TwoFlatConnection.Add(GenBlock2DSpacialProperties.RIGHT | GenBlock2DSpacialProperties.BACK);
        TwoFlatConnection.Add(GenBlock2DSpacialProperties.BACK | GenBlock2DSpacialProperties.LEFT);
        TwoFlatConnection.Add(GenBlock2DSpacialProperties.LEFT | GenBlock2DSpacialProperties.FORWARD);

        // if we have 3 entrances are next to each other, rotate, 4 types
        ThreeFlatConnection.Add(GenBlock2DSpacialProperties.FORWARD | GenBlock2DSpacialProperties.RIGHT | GenBlock2DSpacialProperties.BACK);
        ThreeFlatConnection.Add(GenBlock2DSpacialProperties.RIGHT | GenBlock2DSpacialProperties.BACK | GenBlock2DSpacialProperties.LEFT);
        ThreeFlatConnection.Add(GenBlock2DSpacialProperties.BACK | GenBlock2DSpacialProperties.LEFT | GenBlock2DSpacialProperties.FORWARD);
        ThreeFlatConnection.Add(GenBlock2DSpacialProperties.LEFT | GenBlock2DSpacialProperties.FORWARD | GenBlock2DSpacialProperties.RIGHT);

        mRoomRotations.Add(OneFlatConnection);
        mRoomRotations.Add(LineConnection);
        mRoomRotations.Add(TwoFlatConnection);
        mRoomRotations.Add(ThreeFlatConnection);
    }

    public RoomAndRotation GetRandomRoom()
    {
        List<RoomAndRotation> rooms = GetAvailableRooms();
        return rooms.Count > 0 ? rooms[Random.Range(0, rooms.Count)] : new RoomAndRotation();
    }

    public List<RoomAndRotation> GetAvailableRooms()
    {
        // Set up our room list
        List<RoomAndRotation> rooms = new List<RoomAndRotation>();

        // Grab our correct Room Pool
        List<GameObject> roomPool = new List<GameObject>();
        switch (mSpacialData.mLevelAccess)
        {
            case GenBlockLevelSpacialProperties.NONE:
                {
                    roomPool = mGenerationManager.mEnvironmentDatabase.mRooms;
                }
                break;
            case GenBlockLevelSpacialProperties.DOWN:
                {
                    roomPool = mGenerationManager.mEnvironmentDatabase.mDownAccessRooms;
                }
                break;
            case GenBlockLevelSpacialProperties.UP:
                {
                    roomPool = mGenerationManager.mEnvironmentDatabase.mUpAccessRooms;
                }
                break;
        }

        for(int i = 0; i < roomPool.Count; i++)
        {
            Room room = roomPool[i].GetComponent<Room>();

            if (room.mSpacialProperties == mSpacialData.mProperties)
            {
                // If a room matches our spacial data perfectly, add it to the list
                rooms.Add(new RoomAndRotation(room.gameObject, 0));
            }
            else
            {

                int degrees = FindRoomRotations(mSpacialData.mProperties, room.mSpacialProperties);
                if (degrees > int.MinValue)
                {
                    rooms.Add(new RoomAndRotation(room.gameObject, degrees));
                }
            }
        }

        return rooms;
    }

    // Returns an int that details how much we have to rotate by if we have a match. If the min int is returned, no match.
    private int FindRoomRotations(GenBlock2DSpacialProperties room, GenBlock2DSpacialProperties prefab)
    {
        // Check each pattern
        for(int i = 0; i < mRoomRotations.Count; i++)
        {
            for(int j = 0; j < mRoomRotations[i].Count; j++)
            {
                // We have found our room, now we need to find our rotation
                if (mRoomRotations[i][j] == room)
                {
                    // Check for a match
                    for (int k = 0; k < mRoomRotations[i].Count; k++)
                    {
                        if (mRoomRotations[i][k] == prefab)
                        {
                            // Because our rotations are set up to from "forward" in a clockwise motion, our rotation angle is the difference between our two rooms multiplied by 90.
                            return (j - k) * 90;
                        }
                    }
                }
            }
        }

        // No match
        return int.MinValue;
    }

    // Show GenBlock outline
    private void OnDrawGizmos()
    {
        if(mGenerationManager)
        {
            Gizmos.color = mGenerationManager.mGenBlockColour;

            Gizmos.color = mLock ? mGenerationManager.mGenBlockLockedColour : Gizmos.color;
            Gizmos.color = mSpacialData.mIsolated ? mGenerationManager.mGenBlockIsolatedColour : Gizmos.color;
            Gizmos.color = Selection.activeGameObject == this.gameObject ? mGenerationManager.mGenBlockSelectedColour : Gizmos.color;
        }
        else
        {
            Gizmos.color = Color.white;
        }

        Gizmos.DrawWireCube(transform.position, new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z));
    }
}
