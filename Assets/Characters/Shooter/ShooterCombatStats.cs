
using System.Collections.Generic;
using UnityEngine;

public class ShooterCombatStats : MonoBehaviour
{
    private readonly Dictionary<object, int> damageBonuses = new();

    public int DamageBonus
    {
        get
        {
            int total = 0;

            foreach (int bonus in damageBonuses.Values)
                total += bonus;

            return total;
        }
    }

    public void SetDamageBonus(object source, int amount)
    {
        damageBonuses[source] = amount;
    }

    public void RemoveDamageBonus(object source)
    {
        damageBonuses.Remove(source);
    }

    public int CalculateDamage(
        int baseDamage,
        float inheritedBonus = 1f)
    {
        int bonus = Mathf.RoundToInt(
            DamageBonus * Mathf.Max(0f, inheritedBonus)
        );

        return Mathf.Max(1, baseDamage + bonus);
    }
}
