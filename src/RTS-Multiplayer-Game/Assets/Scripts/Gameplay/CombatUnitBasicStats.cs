using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatUnitBasicStats
{
    public Property<float> HealthPoints;
    public Property<float> HealthPointsMax;
    public Property<float> MovementSpeed;
    public Property<float> MovementSpeedMin;
    public Property<float> MovementSpeedMax;
    public Property<float> AttackDamage;
    public Property<float> AttackSpeed;
    public Property<float> AttackSpeedMin;
    public Property<float> AttackSpeedMax;
    public Property<float> AttackRange;
    public Property<float> TargetSearchRange;

    public bool CanTakeDamage { get; set; }

    public CombatUnitBasicStats(uint unitLevel, CombatUnitBasicData baseStats)
    {
        HealthPoints = new Property<float>(GetLevelBasedMaxHp(baseStats.baseMaxHp, unitLevel));
        HealthPointsMax = new Property<float>(GetLevelBasedMaxHp(baseStats.baseMaxHp, unitLevel));
        MovementSpeed = new Property<float>(baseStats.baseMovementSpeed);
        MovementSpeedMin = new Property<float>(baseStats.baseAttackSpeedMin);
        MovementSpeedMax = new Property<float>(baseStats.baseAttackSpeedMax);
        AttackDamage = new Property<float>(GetLevelBasedAtkDmg(baseStats.baseAttackDamage, unitLevel));
        AttackSpeed = new Property<float>(baseStats.baseAttackSpeed);
        AttackSpeedMin = new Property<float>(baseStats.baseAttackSpeedMin);
        AttackSpeedMax = new Property<float>(baseStats.baseAttackSpeedMax);
        AttackRange = new Property<float>(baseStats.baseAttackRange);
        TargetSearchRange = new Property<float>(baseStats.baseTargetSearchRange);

        CanTakeDamage = true;
    }

    public static int GetLevelBasedLevelUpGold(uint unitCurrentLevel)
        => (int)(1000.0f + 500.0f * Mathf.Pow(unitCurrentLevel, 2));
    
    public static float GetLevelBasedMaxHp(float baseMaxHp, uint unitLevel)
        => baseMaxHp + 500.0f * unitLevel;
    
    public static float GetLevelBasedAtkDmg(float baseAttackDamage, uint unitLevel)
        => baseAttackDamage + 50 * unitLevel;

    public void TakeDamage(float positiveAmount)
    {
        if (!CanTakeDamage || positiveAmount <= 0 || HealthPoints.Value == 0)
            return;
        HealthPoints.Value = Mathf.Max(0, HealthPoints.Value - positiveAmount);
    }

    public void Heal(float positiveAmount)
    {
        if (positiveAmount <= 0 || HealthPoints.Value == HealthPointsMax.Value)
            return;
        HealthPoints.Value = Mathf.Min(HealthPointsMax.Value, HealthPoints.Value + positiveAmount);
    }
}
