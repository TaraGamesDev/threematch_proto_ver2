using UnityEngine;

/// <summary>
/// Scriptable definition for player upgrades presented on level-up.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Forest Guardians/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    public enum UpgradeEffectType
    {
        UnitAttackPercent,
        UnitHealthPercent,
        UnitAttackSpeedPercent,
        UnitRangePercent,
        PlayerMaxHealth,
        PlayerShield,
        TierSpawnWeight
    }

    [Header("Metadata")]
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public UpgradeEffectType effectType = UpgradeEffectType.UnitAttackPercent;
    public float value = 5f;
    public bool isPercentage = true;
    [Tooltip("Used when the effect targets a specific tier")] public UnitData.UnitTier targetTier = UnitData.UnitTier.Tier1;
}
