
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LightningArcVFX : MonoBehaviour
{
    public static LightningArcVFX Instance { get; private set; }

    [Header("Material")]
    [SerializeField] private Material lightningMaterial;

    [Header("Appearance")]
    [SerializeField, Range(3, 24)]
    private int segments = 12;

    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float jitter = 0.35f;
    [SerializeField] private float heightOffset = 1f;

    [SerializeField] private float coreWidth = 0.10f;
    [SerializeField] private float glowWidth = 0.30f;

    [Header("Test")]
    [SerializeField] private Transform testFrom;
    [SerializeField] private Transform testTo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("存在多个 LightningArcVFX", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(Vector3 start, Vector3 end)
    {
        if (lightningMaterial == null)
        {
            Debug.LogWarning("没有配置闪电材质", this);
            return;
        }

        start += Vector3.up * heightOffset;
        end += Vector3.up * heightOffset;

        if ((end - start).sqrMagnitude < 0.001f)
            return;

        // 每段闪电都有自己的临时对象
        GameObject arc = new GameObject("LightningArc");
        arc.transform.SetParent(transform);

        LineRenderer glow = CreateLine(
            arc.transform,
            "Glow",
            glowWidth,
            new Color(0.1f, 0.65f, 1f, 0.5f)
        );

        LineRenderer core = CreateLine(
            arc.transform,
            "Core",
            coreWidth,
            Color.white
        );

        StartCoroutine(AnimateArc(
            arc, glow, core, start, end
        ));
    }

    private LineRenderer CreateLine(
        Transform parent,
        string objectName,
        float width,
        Color color)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent);

        LineRenderer line = obj.AddComponent<LineRenderer>();

        line.sharedMaterial = lightningMaterial;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.positionCount = segments + 1;

        line.widthMultiplier = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;

        line.startColor = color;
        line.endColor = color;

        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        return line;
    }

    private IEnumerator AnimateArc(
        GameObject arc,
        LineRenderer glow,
        LineRenderer core,
        Vector3 start,
        Vector3 end)
    {
        Vector3[] points = new Vector3[segments + 1];

        Vector3 direction = (end - start).normalized;

        // 构造与闪电方向垂直的偏移方向
        Vector3 sideways = Vector3.Cross(direction, Vector3.up);

        if (sideways.sqrMagnitude < 0.001f)
            sideways = Vector3.Cross(direction, Vector3.forward);

        sideways.Normalize();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float fade = 1f - elapsed / duration;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;

                Vector3 position = Vector3.Lerp(start, end, t);

                // 两端不抖动，中间抖动最明显
                float envelope = Mathf.Sin(Mathf.PI * t);

                position += sideways
                    * Random.Range(-jitter, jitter)
                    * envelope;

                position += Vector3.up
                    * Random.Range(-jitter, jitter)
                    * 0.4f * envelope;

                points[i] = position;
            }

            glow.SetPositions(points);
            core.SetPositions(points);

            glow.startColor = glow.endColor =
                new Color(0.1f, 0.65f, 1f, fade * 0.5f);

            core.startColor = core.endColor =
                new Color(1f, 1f, 1f, fade);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(arc);
    }

    [ContextMenu("Test / Play Lightning")]
    private void TestLightning()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在 Play 模式下测试");
            return;
        }

        if (testFrom == null || testTo == null)
        {
            Debug.LogWarning("请绑定 Test From / Test To");
            return;
        }

        Play(testFrom.position, testTo.position);
    }
}
