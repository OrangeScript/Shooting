using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class DoorBreakVisual : MonoBehaviour
{
    [SerializeField] private Renderer intactRenderer;
    [SerializeField] private Material lightCrackMaterial;
    [SerializeField] private Material heavyCrackMaterial;

    [SerializeField] private Transform breakOrigin;
    [SerializeField] private GameObject fragmentsPrefab;

    private Enemy enemy;
    private Material originalMaterial;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (intactRenderer != null)
            originalMaterial = intactRenderer.sharedMaterial;
    }

    private void OnEnable()
    {
        enemy.Spawned += RestoreDoor;
        enemy.Damaged += UpdateCracks;
    }

    private void OnDisable()
    {
        enemy.Spawned -= RestoreDoor;
        enemy.Damaged -= UpdateCracks;
    }

    private void RestoreDoor()
    {
        if (intactRenderer == null)
            return;

        intactRenderer.sharedMaterial = originalMaterial;
        intactRenderer.enabled = true;
    }

    private void UpdateCracks(int remainingHealth)
    {
        if (intactRenderer == null)
            return;

        float ratio = remainingHealth / (float)enemy.MaxHealth;

        if (ratio <= 0.34f && heavyCrackMaterial != null)
            intactRenderer.sharedMaterial = heavyCrackMaterial;
        else if (ratio <= 0.67f && lightCrackMaterial != null)
            intactRenderer.sharedMaterial = lightCrackMaterial;
    }

    public void Shatter()
    {
        if (intactRenderer == null || breakOrigin == null ||
            fragmentsPrefab == null)
        {
            Debug.LogError("请绑定木门 Renderer、Break Origin 和碎片预制体。", this);
            return;
        }

        intactRenderer.enabled = false;

        GameObject fragments = Instantiate(
            fragmentsPrefab, breakOrigin.position, breakOrigin.rotation);

        // 给碎片一点向外的速度，重力负责让它们落地。
        foreach (Rigidbody piece in fragments.GetComponentsInChildren<Rigidbody>())
        {
            piece.AddExplosionForce(
                0.6f, breakOrigin.position, 3f, 0f,
                ForceMode.VelocityChange);
        }

        Destroy(fragments, 4f);
    }
}