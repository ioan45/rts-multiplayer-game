using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatUnitBasicData", menuName = "ScriptableObjects/Combat Units/CombatUnitBasicData", order = 1)]
public class CombatUnitBasicData : ScriptableObject
{
    public uint unitId;
    public string unitName;
    public Sprite unitIcon;
    public Sprite unitIconGreyscale;
    public uint maxLevel;
    public float energyCost;
    public float ySpawnCoord;

    public float baseMaxHp;
    public float baseMovementSpeed;
    public float baseMovementSpeedMin;
    public float baseMovementSpeedMax;
    public float baseAttackDamage;
    public float baseAttackSpeed;
    public float baseAttackSpeedMin;
    public float baseAttackSpeedMax;
    public float baseAttackRange;
    public float baseTargetSearchRange;
}
