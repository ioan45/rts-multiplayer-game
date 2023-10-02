public class OwnedCombatUnitData
{
    public CombatUnitBasicData basicData;
    public uint unitLevel;

    public OwnedCombatUnitData(CombatUnitBasicData data, uint level)
    {
        basicData = data;
        unitLevel = level;
    }
}
