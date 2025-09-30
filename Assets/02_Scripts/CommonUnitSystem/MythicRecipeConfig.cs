using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Data-driven definition for mythic merge recipes. Configure in the inspector to map
/// a sequence of base unit blocks to the unit data that should be created.
/// </summary>
[CreateAssetMenu(fileName = "MythicRecipeConfig", menuName = "Forest Guardians/Mythic Recipe Config")]
public class MythicRecipeConfig : ScriptableObject
{
    [SerializeField] private List<MythicRecipe> recipes = new List<MythicRecipe>();
    public IEnumerable<MythicRecipe> ActiveRecipes => recipes.Where(r => r != null && r.IsValid());
}

/// <summary>
/// Individual mythic recipe as a ScriptableObject for easy management in the inspector.
/// Each recipe defines a sequence of units that can be merged into a mythic unit.
/// </summary>
[CreateAssetMenu(fileName = "MythicRecipe", menuName = "Forest Guardians/Mythic Recipe")]
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

    [Tooltip("Optional message surfaced when the recipe resolves")] 
    public string UnlockMessage = string.Empty;

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

    /// <summary>
    /// Gets a human-readable description of this recipe
    /// </summary>
    public string GetDescription()
    {
        if (!IsValid()) return "Invalid Recipe";
        
        var sequenceNames = Sequence.Select(unit => unit.name).ToArray();
        return $"{Id}: {string.Join(" + ", sequenceNames)} → {ResultUnit.name}";
    }
}
