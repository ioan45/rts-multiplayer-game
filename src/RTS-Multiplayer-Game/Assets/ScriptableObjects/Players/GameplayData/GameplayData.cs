using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayData", menuName = "ScriptableObjects/Players/GameplayData", order = 1)]
public class GameplayData : ScriptableObject
{
    public uint playerNumber;
    public int initialCameraConfigIndex;
    public Vector3 unitFacingDirOnSpawn;
    public string mainTargetObjName;
    public int alliesLayer;
    public int enemiesLayer;
}
