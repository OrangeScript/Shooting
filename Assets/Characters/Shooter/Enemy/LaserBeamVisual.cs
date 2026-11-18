using UnityEngine;

public class LaserBeamVisual : MonoBehaviour
{
    private enum Mode { Off, Warning, Firing }

    [SerializeField] private LineRenderer warningLine;
    [SerializeField] private LineRenderer bodyLine;
    [SerializeField] private LineRenderer coreLine;
    [SerializeField] private LineRenderer glowLine;

    [SerializeField] private float groundY = 0.03f;
    [SerializeField] private float warningPulseHz = 2.5f;

    [SerializeField] private Color warningColor =
        new Color(1f, 0.8f, 0.1f, 0.4f);

    [SerializeField] private Color bodyColor =
        new Color(1f, 0.22f, 0.04f, 0.9f);

    [SerializeField] private Color coreColor =
        new Color(1f, 0.95f, 0.72f, 1f);

    [SerializeField] private Color glowColor =
        new Color(1f, 0.22f, 0.04f, 0.1f);

    private Enemy enemy;
    private LineRenderer[] lines;
    private Mode mode;
    private Vector3 start, end;
    private float width;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();

        lines = new[]
        {
            warningLine, bodyLine, coreLine, glowLine
        };

        foreach (LineRenderer renderer in lines)
        {
            if (renderer == null)
            {
                Debug.LogError("激光表现需要绑定四条Line。", this);
                enabled = false;
                return;
            }

            renderer.useWorldSpace = true;
            renderer.positionCount = 2;
            renderer.loop = false;
            renderer.widthCurve =
                AnimationCurve.Constant(0f, 1f, 1f);
        }

        Stop();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void ShowWarning(Vector3 from, Vector3 to, float beamWidth)
    {
        start = from;
        end = to;
        width = beamWidth;

        Stop();
        mode = Mode.Warning;
    }

    public void StartFiring()
    {
        if (mode != Mode.Warning)
            return;

        StopLines();
        mode = Mode.Firing;
    }

    public void Stop()
    {
        mode = Mode.Off;
        StopLines();
    }

    private void LateUpdate()
    {
        if (enemy != null && !enemy.IsAlive)
        {
            Stop();
            return;
        }

        if (mode == Mode.Warning)
        {
            Vector3 a = start;
            Vector3 b = end;
            a.y = b.y = groundY;

            float pulse = 0.5f + 0.5f * Mathf.Sin(
                Time.time * Mathf.PI * 2f * warningPulseHz
            );

            Color color = warningColor;
            color.a *= Mathf.Lerp(0.65f, 1.35f, pulse);

            Draw(warningLine, a, b, width, color);
        }
        else if (mode == Mode.Firing)
        {
            float shimmer =
                1f + Mathf.Sin(Time.time * 60f) * 0.06f;

            Draw(bodyLine, start, end, width, bodyColor);
            Draw(coreLine, start, end,
                width * 0.22f * shimmer, coreColor);
            Draw(glowLine, start, end,
                width * 1.9f, glowColor);
        }
    }

    private void Draw(
        LineRenderer renderer,
        Vector3 a,
        Vector3 b,
        float beamWidth,
        Color color)
    {
        renderer.SetPosition(0, a);
        renderer.SetPosition(1, b);
        renderer.widthMultiplier = beamWidth;
        renderer.startColor = renderer.endColor = color;
        renderer.enabled = true;
    }

    private void StopLines()
    {
        if (lines == null)
            return;

        foreach (LineRenderer renderer in lines)
            if (renderer != null)
                renderer.enabled = false;
    }
}