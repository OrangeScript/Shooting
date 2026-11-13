
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animator State 名称")]
    [SerializeField]
    private string walkState = "Base Layer.Walk";

    [SerializeField]
    private string dieState = "Base Layer.Die";
    private string attackState = "Base Layer.Attack";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    public void PlayWalk()
    {
        if (animator != null)
            animator.Play(walkState, 0, 0f);
    }

    public void PlayDie()
    {
        if (animator != null)
            animator.Play(dieState, 0, 0f);
    }
    public void PlayAttack()
    {
        if (animator != null)
            animator.Play(attackState, 0, 0f);
    }
}

