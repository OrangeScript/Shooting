using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
[RequireComponent(typeof(UIDocument))]
public class GameHUDController : MonoBehaviour
{
    private VisualElement healthFill;

    private PlayerHealth localHealth;

    private int lastHP = -1;
    private int lastMaxHP = -1;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>()
            .rootVisualElement;

        healthFill = root.Q<VisualElement>("HealthFill");

    }

    private void Update()
    {
        // 1. 找到当前客户端的玩家
        var manager = NetworkManager.Singleton;

        if (manager == null || !manager.IsClient ||
            manager.LocalClient == null)
            return;

        NetworkObject player =
            manager.LocalClient.PlayerObject;

        if (player == null)
            return;

        // 2. 获取本地玩家的 PlayerHealth
        if (localHealth == null ||
            localHealth.gameObject != player.gameObject)
        {
            localHealth = player.GetComponent<PlayerHealth>();

            lastHP = -1;
            lastMaxHP = -1;
        }

        if (localHealth == null)
            return;

        int hp = localHealth.CurrentHealth;
        int maxHP = localHealth.MaxHealth;

        // 血量没变化就不刷新 UI
        if (hp == lastHP && maxHP == lastMaxHP)
            return;

        lastHP = hp;
        lastMaxHP = maxHP;


        // 4. 更新红色血条的长度
        float ratio = Mathf.Clamp01(
            (float)hp / Mathf.Max(1, maxHP)
        );

        healthFill.style.width =
            Length.Percent(ratio * 100f);
    }
}
