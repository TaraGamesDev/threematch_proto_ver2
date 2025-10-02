using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Data-driven definition for mythic merge recipes. Configure in the inspector to map
/// a sequence of base unit blocks to the unit data that should be created.
/// </summary>
///     
[Preserve, CreateAssetMenu(fileName = "MythicRecipeConfig", menuName = "Forest Guardians/Mythic Recipe Config")]
public class MythicRecipeConfig : ScriptableObject
{
    [SerializeField] private List<MythicRecipe> recipes = new List<MythicRecipe>();
    public IEnumerable<MythicRecipe> ActiveRecipes => recipes.Where(r => r != null && r.IsValid());
}