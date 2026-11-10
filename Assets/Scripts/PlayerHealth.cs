
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(
            0, currentHealth - damage
        );

        Debug.Log(
            $"{name} HP: {currentHealth}/{maxHealth}"
        );

        if (IsDead)
        {
            Debug.Log($"{name} died!");
        }
    }
}
