using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Individual mythic recipe as a ScriptableObject for easy management in the inspector.
/// Each recipe defines a sequence of units that can be merged into a mythic unit.
/// </summary>
[Preserve, CreateAssetMenu(fileName = "MythicRecipe", menuName = "Forest Guardians/Mythic Recipe")]
public class MythicRecipe : ScriptableObject
{
    [Tooltip("Identifier used in logs")] 
    public string Id;
    
    [Tooltip("Raw UnitData references that must appear in order")] 
    public List<UnitData> Sequence = new List<UnitData>();
    
    [Tooltip("Mythic unit that replaces the consumed blocks")] 
    public UnitData ResultUnit;
    
    [Tooltip("Number of result blocks inserted into the queue")] 
    public int OutputCount = 1;
    
    [Tooltip("이 신화 유닛이 해금되는 웨이브 번호")]
    public int unlockWave = 5;

    [Header("Unlock Settings")]
    [Tooltip("이 신화 유닛이 해금되었는지 여부")]
    public bool isUnlocked = false;

    /// <summary>
    /// Validates that this recipe has all required data
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Id) && 
               ResultUnit != null && 
               Sequence != null && 
               Sequence.Count > 0 && 
               Sequence.All(unit => unit != null);
    }
}
