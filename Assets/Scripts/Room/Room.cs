using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum RoomTypes
{
    ROOM        = (1<<0),
    CORRIDOR    = (1<<1),
    STAIRCASE   = (1<<2),
}

public class Room : MonoBehaviour
{
    // Variables for configuration of Room prefabs
    // This script doesn't need to run in editor as it's basically just here to hold some public options

    [EnumFlag]
    [SerializeField]
    public GenBlock2DSpacialProperties mSpacialProperties;
    public GenBlockLevelSpacialProperties mElevationAccess;
    public RoomTypes mRoomType;
    public int mFloorLimiter; // The piece is limited to use on this floor (set to 0 to be ignored)
    public int mFloorExcluder; // The piece is limited to use on on ever floor bar this one (set to 0 to be ignored)
}
