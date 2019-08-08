using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Environment Data", menuName = "Databases/Environment Data", order = 1)]
public class EnvironmentData : ScriptableObject
{
    public List<GameObject> mRooms;
    public List<GameObject> mUpAccessRooms;
    public List<GameObject> mDownAccessRooms;
}
