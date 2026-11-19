
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

    [SerializeField] private string waveState = "Base Layer.Wave";
    [SerializeField] private string fireState = "Base Layer.Fire";
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField] private string recoverState = "Base Layer.Recover";

    public void PlayWave() => BlendTo(waveState);
    public void PlayFire() => BlendTo(fireState);
    public void PlayIdle() => BlendTo(idleState);
    public void PlayRecover() => BlendTo(recoverState);

    private void BlendTo(string state)
    {
        if (animator != null)
            animator.CrossFadeInFixedTime(state, 0.08f, 0);
    }

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

