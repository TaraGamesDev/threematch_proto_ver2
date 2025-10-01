using UnityEngine;

/// <summary>
/// Resolves combat interactions between units and keeps battle rules centralised.
/// Additional behaviour (status effects, damage modifiers) can be layered here later.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResolveAttack(Unit attacker, Unit defender, int rawDamage)
    {
        if (attacker == null || defender == null || defender.IsDead()) return;

        int finalDamage = Mathf.Max(0, rawDamage);
        defender.TakeDamage(finalDamage);
    }

    public void ResolvePlayerHit(int damage)
    {
        if (damage <= 0) return;
        GameManager.Instance?.TakeDamage(damage);
    }
}
