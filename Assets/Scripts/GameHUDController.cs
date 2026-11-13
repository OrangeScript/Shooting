using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameHUDController : MonoBehaviour
{
    [Header("Touch Controls")]
    [SerializeField] private bool showTouchControlsInEditor = true;
    [SerializeField, Range(0f, 0.5f)] private float joystickDeadZone = 0.12f;

    private static GameHUDController activeHud;
    private VisualElement root;
    private VisualElement healthFill;
    private VisualElement touchControls;
    private VisualElement joystickArea;
    private VisualElement joystickKnob;
    private VisualElement[] actionButtons;
    private readonly Dictionary<VisualElement, int> pressedButtons = new();
    private int joystickPointerId = -1;
    private bool touchControlsVisible;
    private bool hasFocus = true;
    private bool isPaused;
    private Rect lastSafeArea;
    private Vector2 lastRootSize;
    private Vector2Int lastScreenSize;
    private NetworkObject localPlayer;
    private PlayerController localController;
    private PlayerHealth localHealth;
    private int lastHP = -1;
    private int lastMaxHP = -1;

    private void OnEnable()
    {
        activeHud = this;
        root = GetComponent<UIDocument>().rootVisualElement;
        healthFill = root.Q<VisualElement>("HealthFill");
        touchControls = root.Q<VisualElement>("TouchControls");
        joystickArea = root.Q<VisualElement>("JoystickArea");
        joystickKnob = root.Q<VisualElement>("JoystickKnob");
        actionButtons = new[]
        {
            root.Q<VisualElement>("TouchAttackButton"),
            root.Q<VisualElement>("TouchSAttackButton"),
            root.Q<VisualElement>("TouchKickButton"),
            root.Q<VisualElement>("TouchJumpButton")
        };

        if (joystickArea != null)
        {
            joystickArea.RegisterCallback<PointerDownEvent>(OnJoystickDown);
            joystickArea.RegisterCallback<PointerMoveEvent>(OnJoystickMove);
            joystickArea.RegisterCallback<PointerUpEvent>(OnJoystickUp);
            joystickArea.RegisterCallback<PointerCancelEvent>(OnJoystickCancel);
            joystickArea.RegisterCallback<PointerCaptureOutEvent>(OnJoystickCaptureOut);
        }
        foreach (var button in actionButtons)
        {
            if (button == null) continue;
            button.RegisterCallback<PointerDownEvent>(OnActionDown);
            button.RegisterCallback<PointerUpEvent>(OnActionUp);
            button.RegisterCallback<PointerCancelEvent>(OnActionCancel);
            button.RegisterCallback<PointerCaptureOutEvent>(OnActionCaptureOut);
        }
        if (touchControls != null)
            touchControls.style.display = DisplayStyle.None;
        lastRootSize = Vector2.zero;
    }

    private void OnDisable()
    {
        ResetTouchInput();
        if (joystickArea != null)
        {
            joystickArea.UnregisterCallback<PointerDownEvent>(OnJoystickDown);
            joystickArea.UnregisterCallback<PointerMoveEvent>(OnJoystickMove);
            joystickArea.UnregisterCallback<PointerUpEvent>(OnJoystickUp);
            joystickArea.UnregisterCallback<PointerCancelEvent>(OnJoystickCancel);
            joystickArea.UnregisterCallback<PointerCaptureOutEvent>(OnJoystickCaptureOut);
        }
        if (actionButtons != null)
        {
            foreach (var button in actionButtons)
            {
                if (button == null) continue;
                button.UnregisterCallback<PointerDownEvent>(OnActionDown);
                button.UnregisterCallback<PointerUpEvent>(OnActionUp);
                button.UnregisterCallback<PointerCancelEvent>(OnActionCancel);
                button.UnregisterCallback<PointerCaptureOutEvent>(OnActionCaptureOut);
            }
        }
        SetTouchControlsVisible(false);
        localPlayer = null;
        localController = null;
        localHealth = null;
        if (activeHud == this)
            activeHud = null;
    }

    private void Update()
    {
        var manager = NetworkManager.Singleton;
        NetworkObject player = manager != null && manager.IsClient && manager.LocalClient != null
            ? manager.LocalClient.PlayerObject : null;
        if (localPlayer != player)
        {
            ResetTouchInput();
            localPlayer = player;
            localController = player != null ? player.GetComponent<PlayerController>() : null;
            localHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
            lastHP = lastMaxHP = -1;
        }

        bool showControls = (Application.isMobilePlatform ||
            (Application.isEditor && showTouchControlsInEditor)) && hasFocus && !isPaused &&
            localController != null && localController.IsSpawned && localController.IsOwner &&
            (localHealth == null || !localHealth.IsDead);
        SetTouchControlsVisible(showControls);
        if (showControls)
            UpdateSafeArea();

        if (localHealth == null || healthFill == null)
            return;
        int hp = localHealth.CurrentHealth;
        int maxHP = localHealth.MaxHealth;
        if (hp == lastHP && maxHP == lastMaxHP)
            return;
        lastHP = hp;
        lastMaxHP = maxHP;
        healthFill.style.width = Length.Percent(
            Mathf.Clamp01((float)hp / Mathf.Max(1, maxHP)) * 100f);
    }

    private void SetTouchControlsVisible(bool visible)
    {
        if (touchControls == null || touchControlsVisible == visible)
            return;
        if (!visible)
            ResetTouchInput();
        touchControlsVisible = visible;
        touchControls.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateSafeArea()
    {
        if (root.panel == null || root.contentRect.width <= 0f || root.contentRect.height <= 0f)
            return;
        Rect safe = Screen.safeArea;
        Vector2 size = root.contentRect.size;
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        if (safe == lastSafeArea && size == lastRootSize && screenSize == lastScreenSize)
            return;
        lastSafeArea = safe;
        lastRootSize = size;
        lastScreenSize = screenSize;

        // 屏幕坐标先翻转 Y，再转换为面板单位，兼容 PanelSettings 的缩放。
        Vector2 min = root.WorldToLocal(RuntimePanelUtils.ScreenToPanel(root.panel,
            new Vector2(safe.xMin, Screen.height - safe.yMax)));
        Vector2 max = root.WorldToLocal(RuntimePanelUtils.ScreenToPanel(root.panel,
            new Vector2(safe.xMax, Screen.height - safe.yMin)));
        touchControls.style.left = Mathf.Max(0f, min.x);
        touchControls.style.top = Mathf.Max(0f, min.y);
        touchControls.style.right = Mathf.Max(0f, size.x - max.x);
        touchControls.style.bottom = Mathf.Max(0f, size.y - max.y);
    }

    private void OnJoystickDown(PointerDownEvent evt)
    {
        if (!touchControlsVisible || evt.button != 0 || joystickPointerId != -1)
            return;
        joystickPointerId = evt.pointerId;
        joystickArea.CapturePointer(evt.pointerId);
        UpdateJoystick(evt.position);
        evt.StopPropagation();
    }

    private void OnJoystickMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != joystickPointerId)
            return;
        UpdateJoystick(evt.position);
        evt.StopPropagation();
    }

    private void UpdateJoystick(Vector2 position)
    {
        float knobWidth = joystickKnob != null ? joystickKnob.resolvedStyle.width : 84f;
        float radius = Mathf.Max(1f, (joystickArea.contentRect.width - knobWidth) * 0.5f);
        Vector2 offset = Vector2.ClampMagnitude(
            joystickArea.WorldToLocal(position) - joystickArea.contentRect.center, radius);
        Vector2 input = offset / radius;
        float strength = Mathf.Clamp01((input.magnitude - joystickDeadZone) / (1f - joystickDeadZone));
        localController?.SetTouchMoveInput(new Vector2(input.x, -input.y).normalized * strength);
        if (joystickKnob != null)
        {
            joystickKnob.style.marginLeft = -knobWidth * 0.5f + offset.x;
            joystickKnob.style.marginTop = -joystickKnob.resolvedStyle.height * 0.5f + offset.y;
        }
    }

    private void OnJoystickUp(PointerUpEvent evt) => ReleaseJoystick(evt.pointerId);
    private void OnJoystickCancel(PointerCancelEvent evt) => ReleaseJoystick(evt.pointerId);
    private void OnJoystickCaptureOut(PointerCaptureOutEvent evt) => ReleaseJoystick(evt.pointerId);

    private void ReleaseJoystick(int pointerId)
    {
        if (pointerId != joystickPointerId)
            return;
        joystickPointerId = -1;
        localController?.SetTouchMoveInput(Vector2.zero);
        if (joystickKnob != null)
        {
            joystickKnob.style.marginLeft = -42f;
            joystickKnob.style.marginTop = -42f;
        }
        if (joystickArea != null && joystickArea.HasPointerCapture(pointerId))
            joystickArea.ReleasePointer(pointerId);
    }

    private void OnActionDown(PointerDownEvent evt)
    {
        var button = (VisualElement)evt.currentTarget;
        if (!touchControlsVisible || evt.button != 0 || pressedButtons.ContainsKey(button))
            return;
        pressedButtons.Add(button, evt.pointerId);
        button.CapturePointer(evt.pointerId);
        button.style.opacity = 0.65f;
        switch (button.name)
        {
            case "TouchAttackButton": localController?.RequestAttack(); break;
            case "TouchSAttackButton": localController?.RequestSAttack(); break;
            case "TouchKickButton": localController?.RequestKick(); break;
            case "TouchJumpButton": localController?.RequestJump(); break;
        }
        evt.StopPropagation();
    }

    private void OnActionUp(PointerUpEvent evt) => ReleaseAction((VisualElement)evt.currentTarget, evt.pointerId);
    private void OnActionCancel(PointerCancelEvent evt) => ReleaseAction((VisualElement)evt.currentTarget, evt.pointerId);
    private void OnActionCaptureOut(PointerCaptureOutEvent evt) => ReleaseAction((VisualElement)evt.currentTarget, evt.pointerId);

    private void ReleaseAction(VisualElement button, int pointerId)
    {
        if (!pressedButtons.TryGetValue(button, out int pressedPointer) || pressedPointer != pointerId)
            return;
        pressedButtons.Remove(button);
        button.style.opacity = 1f;
        if (button.HasPointerCapture(pointerId))
            button.ReleasePointer(pointerId);
    }

    private void ResetTouchInput()
    {
        if (joystickPointerId != -1)
            ReleaseJoystick(joystickPointerId);
        localController?.SetTouchMoveInput(Vector2.zero);
        if (actionButtons == null) return;
        foreach (var button in actionButtons)
        {
            if (button != null && pressedButtons.TryGetValue(button, out int pointerId))
                ReleaseAction(button, pointerId);
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused)
            SetTouchControlsVisible(false);
    }

    private void OnApplicationPause(bool paused)
    {
        isPaused = paused;
        if (paused)
            SetTouchControlsVisible(false);
    }

    // 编辑器用鼠标测试触屏按钮时，不让同一次点击同时触发鼠标攻击绑定。
    public static bool IsPointerOverTouchControls()
    {
        var hud = activeHud;
        if (hud == null || !hud.touchControlsVisible || hud.root?.panel == null || Mouse.current == null)
            return false;
        Vector2 position = Mouse.current.position.ReadValue();
        Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(hud.root.panel,
            new Vector2(position.x, Screen.height - position.y));
        for (var element = hud.root.panel.Pick(panelPosition); element != null; element = element.parent)
        {
            if (element == hud.touchControls)
                return true;
        }
        return false;
    }
}
