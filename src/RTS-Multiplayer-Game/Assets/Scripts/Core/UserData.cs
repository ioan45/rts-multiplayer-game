using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserData
{
    public static UserData SignedInUserData { get; set; }

    public string loginSessionToken;
    public string username;
    public string playerName;
    public int gold;
    public int trophies;
    public Dictionary<uint, OwnedCombatUnitData> ownedUnitsData;  // owned units IDs (key) and owned units data (value)
    public Dictionary<uint, uint> deckUnitsData;  // owned units IDs (key) and deck positions (value)
}
