using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// #CM -    This script handles the logic attached to our GenBlocks. These are the literal building blocks of our environment,
//          they will be responsible for managing themselves and reporting to the Generation Manager

[System.Flags]
public enum GenBlockSpacialProperties : int
{
    LEFT    = (1 << 0),
    RIGHT   = (1 << 1),
    FORWARD = (1 << 2),
    BACK    = (1 << 3),
    UP      = (1 << 4),
    DOWN    = (1 << 5),
}

public struct SpacialData
{
    public GenBlockSpacialProperties mProperties;
    public GenBlockSpacialProperties mUsedPaths;
    public Dictionary<GenBlockSpacialProperties, GenBlock> mNeighbours;
    public bool mIsolated;
    public bool mDeadEnd;
    public bool mVisited;
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

    public bool mDrawSpacialDataGizmos = true;

    [Header("Clutter Settings")]
    public bool mOverideGlobalClutter;

    [Tooltip("Controls how much clutter is allowed in this specific block.")]
    public bool mUseClutter;

    [HideInInspector]
    public int mCurrentFloorLevel;

    public SpacialData mSpacialData;

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

    public void UpdateSpacialProperties()
    {
        // Reset spacial data
        mSpacialData.mProperties = 0;
        mSpacialData.mNeighbours = new Dictionary<GenBlockSpacialProperties, GenBlock>();

        // Cast out to find neighbours on the correct collision layer.
        int layerMask = 1 << 9;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.left, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.LEFT;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.LEFT, hit.collider.gameObject.GetComponent<GenBlock>());
        }
        if (Physics.Raycast(transform.position, Vector3.right, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.RIGHT;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.RIGHT, hit.collider.gameObject.GetComponent<GenBlock>());
        }
        if (Physics.Raycast(transform.position, Vector3.forward, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.FORWARD;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.FORWARD, hit.collider.gameObject.GetComponent<GenBlock>());
        }
        if (Physics.Raycast(transform.position, Vector3.back, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.BACK;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.BACK, hit.collider.gameObject.GetComponent<GenBlock>());
        }
        if (Physics.Raycast(transform.position, Vector3.up, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.UP;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.UP, hit.collider.gameObject.GetComponent<GenBlock>());
        }
        if (Physics.Raycast(transform.position, Vector3.down, out hit, mGenerationManager.mGridSize / 2, layerMask))
        {
            mSpacialData.mProperties |= GenBlockSpacialProperties.DOWN;
            mSpacialData.mNeighbours.Add(GenBlockSpacialProperties.DOWN, hit.collider.gameObject.GetComponent<GenBlock>());
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

    public RoomAndRotation GetRandomRoom()
    {
        List<RoomAndRotation> rooms = GetAvailableRooms();
        return rooms.Count > 0 ? rooms[Random.Range(0, rooms.Count)] : new RoomAndRotation();
    }

    public List<RoomAndRotation> GetAvailableRooms()
    {
        // Set up our room list
        List<RoomAndRotation> rooms = new List<RoomAndRotation>();

        // Loop through to find candidate rooms
        for (int i = 0; i < mGenerationManager.mEnvironmentDatabase.mRooms.Count; i++)
        {
            Room room = mGenerationManager.mEnvironmentDatabase.mRooms[i].GetComponent<Room>();

            if (room.mSpacialProperties == mSpacialData.mUsedPaths)
            {
                // If a room matches our spacial data perfectly, add it to the list
                rooms.Add(new RoomAndRotation(room.gameObject, 0));
            }
            else
            {
                // Strip up and down properties
                // TODO - This could probably be moved in to the rotation calculation itself to help cut down on code clutter.
                GenBlockSpacialProperties ourRoom = mSpacialData.mUsedPaths;
                GenBlockSpacialProperties candidate = room.mSpacialProperties;
                if ((ourRoom & GenBlockSpacialProperties.UP) != 0 && (candidate & GenBlockSpacialProperties.UP) != 0)
                {
                    ourRoom &= ~GenBlockSpacialProperties.UP;
                    candidate &= ~GenBlockSpacialProperties.UP;
                }

                if ((ourRoom & GenBlockSpacialProperties.DOWN) != 0 && (candidate & GenBlockSpacialProperties.DOWN) != 0)
                {
                    ourRoom &= ~GenBlockSpacialProperties.DOWN;
                    candidate &= ~GenBlockSpacialProperties.DOWN;
                }

                int degrees = FindRoomRotations(ourRoom, candidate);
                if (degrees > int.MinValue)
                {
                    rooms.Add(new RoomAndRotation(room.gameObject, degrees));
                }
            }
        }

        return rooms;
    }

    // Returns an int that details how much we have to rotate by if we have a match. If the min int is returned, no match.
    private int FindRoomRotations(GenBlockSpacialProperties room, GenBlockSpacialProperties prefab)
    {
        // Check each pattern
        for(int i = 0; i < mGenerationManager.mRoomRotations.Count; i++)
        {
            for(int j = 0; j < mGenerationManager.mRoomRotations[i].Count; j++)
            {
                // We have found our room, now we need to find our rotation
                if (mGenerationManager.mRoomRotations[i][j] == room)
                {
                    // Check for a match
                    for (int k = 0; k < mGenerationManager.mRoomRotations[i].Count; k++)
                    {
                        if (mGenerationManager.mRoomRotations[i][k] == prefab)
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

        // This draws a Cyan line out towards where there is a connection with another GenBlock
        if(mDrawSpacialDataGizmos)
        {
            Gizmos.color = Color.cyan;

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.FORWARD) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.forward / 2);
            }

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.BACK) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position - Vector3.forward / 2);
            }

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.LEFT) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.left / 2);
            }

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.RIGHT) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.right / 2);
            }

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.UP) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up / 2);
            }

            if ((mSpacialData.mProperties & GenBlockSpacialProperties.DOWN) != 0)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down / 2);
            }
        }
    }
}
