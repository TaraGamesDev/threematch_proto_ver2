using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles upgrade selection, application, and persistence across waves.
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    [Header("Upgrade Sources")]
    [SerializeField] private List<UpgradeDefinition> upgradePool = new List<UpgradeDefinition>();
    [SerializeField] private SpawnProbabilitySystem spawnProbabilityConfig;
    [SerializeField, Min(1)] private int optionsPerLevel = 3;

    public System.Action<IReadOnlyList<UpgradeDefinition>> OnUpgradeOptionsReady;
    public System.Action<UpgradeDefinition> OnUpgradeApplied;

    private readonly List<UpgradeDefinition> permanentUpgrades = new List<UpgradeDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OfferLevelUpChoices(int playerLevel)
    {
        if (upgradePool.Count == 0)
        {
            Debug.LogWarning("UpgradeSystem: No upgrades configured.");
            return;
        }

        List<UpgradeDefinition> options = DrawUniqueOptions(optionsPerLevel);
        if (OnUpgradeOptionsReady != null) OnUpgradeOptionsReady.Invoke(options);
        else Debug.Log($"UpgradeSystem: Level-up generated {options.Count} options but no listener is registered.");
    }

    public void ApplyUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade == null) return;

        permanentUpgrades.Add(upgrade);
        ApplyUpgradeImmediate(upgrade);
        if (OnUpgradeApplied != null) OnUpgradeApplied.Invoke(upgrade);
        else Debug.Log($"UpgradeSystem: Applied upgrade '{upgrade.upgradeName}' (no listeners registered).");
    }

    public void ApplyPermanentUpgradesToUnit(Animal animal)
    {
        if (animal == null) return;

        foreach (UpgradeDefinition upgrade in permanentUpgrades)
        {
            ApplyUpgradeToUnit(animal, upgrade);
        }
    }

    private List<UpgradeDefinition> DrawUniqueOptions(int count)
    {
        List<UpgradeDefinition> poolCopy = new List<UpgradeDefinition>(upgradePool);
        List<UpgradeDefinition> result = new List<UpgradeDefinition>();

        for (int i = 0; i < Mathf.Min(count, poolCopy.Count); i++)
        {
            int index = Random.Range(0, poolCopy.Count);
            result.Add(poolCopy[index]);
            poolCopy.RemoveAt(index);
        }

        return result;
    }

    private void ApplyUpgradeImmediate(UpgradeDefinition upgrade)
    {
        switch (upgrade.effectType)
        {
            case UpgradeDefinition.UpgradeEffectType.UnitAttackPercent:
            case UpgradeDefinition.UpgradeEffectType.UnitHealthPercent:
            case UpgradeDefinition.UpgradeEffectType.UnitAttackSpeedPercent:
            case UpgradeDefinition.UpgradeEffectType.UnitRangePercent:
                foreach (var unit in UnitManager.Instance?.activeUnits ?? new List<Animal>())
                {
                    ApplyUpgradeToUnit(unit, upgrade);
                }
                break;

            case UpgradeDefinition.UpgradeEffectType.PlayerMaxHealth:
                GameManager.Instance?.IncreaseMaxHealth(Mathf.RoundToInt(upgrade.value));
                break;

            case UpgradeDefinition.UpgradeEffectType.PlayerShield:
                GameManager.Instance?.AddShield(Mathf.RoundToInt(upgrade.value));
                break;

            case UpgradeDefinition.UpgradeEffectType.TierSpawnWeight:
                if (spawnProbabilityConfig != null)
                {
                    spawnProbabilityConfig.AddToTierProbability(upgrade.targetTier, upgrade.value);
                }
                break;
        }
    }

    private void ApplyUpgradeToUnit(Animal animal, UpgradeDefinition upgrade)
    {
        if (animal == null || upgrade == null) return;

        switch (upgrade.effectType)
        {
            case UpgradeDefinition.UpgradeEffectType.UnitAttackPercent:
                if (upgrade.isPercentage) animal.currentAttackDamage = Mathf.RoundToInt(animal.currentAttackDamage * (1f + upgrade.value / 100f));
                else animal.currentAttackDamage += Mathf.RoundToInt(upgrade.value);
                break;

            case UpgradeDefinition.UpgradeEffectType.UnitHealthPercent:
                if (upgrade.isPercentage)
                {
                    int bonus = Mathf.RoundToInt(animal.currentHealth * (upgrade.value / 100f));
                    animal.currentHealth += bonus;
                }
                else animal.currentHealth += Mathf.RoundToInt(upgrade.value);
                break;

            case UpgradeDefinition.UpgradeEffectType.UnitAttackSpeedPercent:
                if (upgrade.isPercentage) animal.currentAttackSpeed *= (1f + upgrade.value / 100f);
                else animal.currentAttackSpeed += upgrade.value;
                break;

            case UpgradeDefinition.UpgradeEffectType.UnitRangePercent:
                if (upgrade.isPercentage) animal.currentAttackRange *= (1f + upgrade.value / 100f);
                else animal.currentAttackRange += upgrade.value;
                break;
        }
    }
}
