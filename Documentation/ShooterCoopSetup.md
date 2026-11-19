# Game_01 合作房间的 Unity 配置

此版本从 `MainMenu_1` 进入。单人按钮直接加载 `Game_01`。多人按钮允许创建局域网房间或通过房主 IPv4 加入，由房主开局。合作回合默认 3 波 Birb，支持 1–4 人。单机继续跑原有的 20 波流程。合作里的 Boss、奖励门、掉落和带道具重开留到后续接入。

## 1. Build Settings

停在编辑模式。打开 **File > Build Settings**，把 **Assets/Scenes/MainMenu_1.unity** 加入 Scenes In Build 并启用。若希望新菜单成为应用启动画面，拖到第 0 位。确认 **Assets/Scenes/Game_01.unity** 也在列表且启用。

## 2. 两个合作 Prefab

在 Project 切到 **Edit > Project Settings > Netcode for GameObjects**。为新 Prefab 使用独立的注册表时，建议先关闭 **Generate Default Network Prefabs List**，这样 Unity 不会在保存合作 Prefab 时自动向现有的 DefaultNetworkPrefabs 写入登记。此项目设置会影响以后自动登记的新 Prefab。

1. 在 `Assets/Prefabs` 里，为现有 **Game_Player.prefab** 创建一个 **Prefab Variant**，命名为 `ShooterCoopPlayer`。在变体根上添加 **NetworkObject**、**NetworkTransform** 和 **ShooterCoopPlayer**。NetworkTransform 使用默认服务端权威，打开 **Interpolate**，保留位置 X/Y/Z 同步。保留根上的 CharacterController、GamerController、AutoShooter、ShooterPlayerHealth、PlayerInput 及其现有引用。PlayerInput 的 **Behavior** 保持 **Send Messages**、**Default Map** 保持 **Player**，将 PlayerInput 组件勾选框关掉，运行时由所属客户端打开。AutoShooter 的 **Fire Point** 和 **Muzzle Flash** 沿用 Prefab，**Bullet Pool** 由场景绑定。
2. 在玩家变体中，把子对象 **LegendaryCam** 设为不激活。合作变体里的 **LegendaryPickupCutscene**、**ShooterItemRuntime** 和 **ShooterCompanionManager** 组件可以先取消勾选。Prefab Mode 里只保存变体。
3. 为 **Assets/Characters/Shooter/Enemy/Birb.prefab** 创建 **Prefab Variant**，命名为 `ShooterCoopBirb`。在变体根上添加 **NetworkObject**、**NetworkTransform** 和 **ShooterCoopEnemy**。保持默认服务端权威、**Interpolate** 打开。保留原有 Enemy、EnemyForwardMovement、EnemyAnimation、模型和碰撞体。确认 Enemy 的 **Initialize On Start** 未勾选。怪物出生时由服务端调用现有的 Enemy.Spawn。
4. 在 Project 右键 **Create > Netcode > Network Prefabs List**，创建 `ShooterCoopPrefabs`，登记上面这两个变体各一次。

可以在一个临时场景里创建变体。把原 Prefab 拖进临时场景，再把实例拖到 Project 的新路径，弹窗里选 **Prefab Variant**。保存后删除这个临时实例。合作玩家和怪物都由房主在开局后生成。

## 3. 新菜单 `MainMenu_1`

1. 新建一个根级空物体 `ShooterNetworkManager`，给它加 **NetworkManager**、**Unity Transport**、**ShooterRoom**。NetworkManager 必须在 Hierarchy 顶层。
2. 在 **NetworkManager** 上，**Network Transport** 指向同物体的 Unity Transport，**Network Prefabs Lists** 仅放 `ShooterCoopPrefabs`，**Player Prefab** 留空。勾选 **Enable Scene Management**、**Connection Approval** 和 **Run In Background**。Unity Transport 采用默认的直连协议。
3. **ShooterRoom** 默认端口 `7778`、容量 `4`。每台设备使用同一个端口设置。选中现有 **MenuUI**，在 **MainMenu1Controller > Room** 拖入这个网络管理物体。
4. 在 UI Builder 打开 **Assets/UI/MainMenu_1/MainMenu.uxml**。在现有 **MultiplayerPage** 下新增这三个元素。给开局按钮加现有 `menu-button` 类，状态文字可加 `page-hint` 类，再在 UI Builder 里调整间距和字体。

| 类型 | Name | 显示内容 |
| --- | --- | --- |
| TextField | `RoomAddress` | Label 用「房主 IP」，Value 可先填 `127.0.0.1` |
| Button | `RoomStartButton` | 「开始合作」 |
| Label | `RoomStatus` | 「请输入房主地址」 |

原有的 `CreateButton` 和 `JoinButton` 已有代码绑定。房主创建后留在多人页，队友加入后房主点「开始合作」。返回按钮会关闭当前菜单房间。

## 4. 在同一个 `Game_01` 场景挂合作组件

1. 保持原来的场景玩家 `Game_Player`、`GameDirector`、`GameplayCam` 和各个池，保持原有 Inspector 引用。新建根级空物体 `CoopRun`，添加 **NetworkObject** 和 **ShooterCoopRun**。不把 `CoopRun` 放进网络 Prefab 列表，它是场景里的网络对象。
2. 新建空物体 `CoopSpawnCenter`，把它放在当前单机玩家在道路上的初始位置，旋转对齐该玩家。双方角色从这个点两边分开出生。
3. 给 **ShooterCoopRun** 配置下表。

| 字段 | 在 Hierarchy 或 Project 里拖入 |
| --- | --- |
| Solo Player | `Game_01` 里原来摆好的 `Game_Player` |
| Solo Only Actors | 场景中手动摆好的 **Door**。此 Door 配了 `Initialize On Start`，合作试玩会在开局前隐藏它。其他手工摆放且仅供单机执行的战斗对象也放这里 |
| Director | `GameDirector` |
| Run Hud、Health Hud | `UIDocument` 上的 ShooterRunHUD、ShooterHealthHUD |
| Bullet Pool | `ObjectPool` 下的 `BulletPool` |
| Coop Spawner | 现有 `BirbSpawner`，保持其 `Auto Start` 关闭 |
| Spawn Center | `CoopSpawnCenter` |
| Player Prefab、Enemy Prefab | Project 的 `ShooterCoopPlayer`、`ShooterCoopBirb` 变体 |
| Road Width Objects | 道路平面 `Plane`，以及需要和道路一起按 X 拉宽的地面对象 |
| Side Boundaries | 左右两侧墙或 Box Collider 挡墙的 Transform，各放一次 |
| Gameplay Camera | `GameplayCam` 的 Cinemachine Virtual Camera |
| Road Scroller | 道路 `Plane` 上的 RoadScroller，用于在合作结算时停止滚动 |

道路物体和挡墙分开填写。如果挡墙是拉宽物体的子对象，选择独立的道路网格来拉宽，或者只让父对象缩放带着挡墙移动。字段里的宽度物体应以道路中心为轴，且本地 X 对齐世界 X。

4. 合作范围填 **Solo Road Half Width = 5**、**Extra Half Width Per Player = 1.5**、**Extra Spawn Depth Per Player = 1.5**。先在 Scene 里核对单人道路中心到边缘的距离。如果你的实际道路半宽不同，把 `Solo Road Half Width` 改成实测值。玩家活动范围会留出 0.5 米边距，刷怪会留出 1 米边距。
5. **Combat Waves = 3**。每增加一人，批次刷怪数和同时存活上限按开局人数扩大，默认怪物基础血量额外加 50%、移速加 8%、伤害加 25%。这些比例可在 **ShooterCoopRun > 合作难度**调整，整数血量和伤害沿用现有取整规则。怪物到达终点时扣距离它横向最近的存活玩家的血。
6. 若需要局中随时退出，在 UI Builder 打开 **Assets/Characters/Shooter/ShooterHUD.uxml**，在 HUD 的可见区域新增一个 Button，**Name** 填 `RunLeaveRoom`，文字填「退出房间」，按原界面样式摆放。与其他需要点到的按钮一样，确保它本身的 Picking Mode 是 Position。局末的返回主菜单按钮使用现有结算 UI。

## 5. 验收

1. **单机**。直接打开 `Game_01` 点 Play，检查自动开火、命中、拾取和重开。从 `MainMenu_1` 点「单人游戏」也应进入相同场景。
2. **创建和加入**。保存所有配置后，用项目的 **ParrelSync > Clones Manager** 打开第二个编辑器。两边打开 `MainMenu_1` 点 Play。原窗口创建房间，Clone 填 `127.0.0.1` 加入。房主人数显示 `2/4` 后开局。在两台设备测试时，加入方填房主的局域网 IPv4，两边使用相同端口。Windows 上可以用 `ipconfig` 查看房主当前网卡的 IPv4 地址。
3. **合作战斗**。两边都加载 `Game_01`，各自只能控制自己的角色，能看到队友移动、双方射击和同一批怪物。让一人倒下，另一人仍可继续。全员倒下或波次完成后，两边都应出现合作结算。
4. **人数缩放**。用一人开一局、两人再开一局。默认道路宽度从 10 米增至 13 米，Birb 每次刷新的配置数量和存活上限变为两倍，初始出生点横向范围和纵向空间随人数变大。中途掉线维持本局开局规模，下一局按新人数计算。
5. **离房**。Client 退出时 Host 可继续。Host 退出时 Client 返回主菜单。再次创建房间后能重新加入。

若出现「合作回合无法开局」，优先检查 `CoopRun` 是否有 NetworkObject、Inspector 必填引用是否齐全，以及两个变体是否都登记在本房间的 Network Prefabs List。若加入失败，先用同机 `127.0.0.1` 验证端口和场景配置。
