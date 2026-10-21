## 许可
本项目基于 MIT 许可证开源，详情见 [LICENSE](LICENSE) 文件。

# 途畔归所 · 项目结构速览

> 生成：2026-08-19 ｜ 范围：`Dll/`（78 个 .cs）｜ 已与源码逐文件核对

## 概述

- **途畔归所（Pathside Refuge）**：1-8 人联机 RPG，主机权威 P2P，Godot 4.2+ / C# / ENet（Godot MultiplayerAPI）
- 统一命名空间 `途畔归所.Dll.*`（少量遗留 `维修公司.Dll` 与全局命名空间，见末尾勘误）。
- 资源加载：`ResourceManager` 扫描 `res://Prefab/` 与 `res://Scenes/`，按类型分流注册到 GUIManager / WorldManager / ItemManager / NetObjectInstance。

## 📁 目录结构

### Base/ — 基类

| 文件 | 说明 |
|------|------|
| `CreatureBase.cs` | 生物基类：数据驱动属性、伤害分层、受击事件 |
| `DropBase.cs` | 掉落表条目：按概率/数量生成 `ItemComp` |
| `EntityBase.cs` | 实体基类（当前空壳） |
| `PlacedBase.cs` | 放置物基类：持有 `PlacedData` |
| `SceneBase.cs` | 场景基类：`OnOwnershipGranted` 触发数据恢复 |
| `UIPanelBase.cs` | UI 面板基类 |
| `VegetationBase.cs` | 植被基类：血量、掉落、伤害 RPC |


### Comp/ — 通用组件

| 文件 | 说明 |
|------|------|
| `BuildComp.cs` | 建筑摆放：网格吸附预览（遗留命名空间） |
| `ContainerComp.cs` | 容器（箱子）：完整数据同步链路，统一 RPC 模式 |
| `CreatureAnimComp.cs` | 动画事件：攻击/连段/脚步回调 |
| `DialogComp.cs` | NPC 对话组件 |
| `ItemComp.cs` | 物品掉落实体：拾取、武器挂载/解绑 |
| `ObstacleComp.cs` | 导航障碍（全局命名空间） |
| `SaveComp.cs` | 存档 UI 面板 |
| `ScenePortalComp.cs` | 场景传送门：`CallDeferred` 延迟切换 |
| `SenseComp.cs` | 感知组件：视野内生物/物品列表 |
| `TreeComp.cs` | 树木（`VegetationBase` 子类） |


### Core/ — 核心入口与联网

| 文件 | 说明 |
|------|------|
| `GameCore.cs` | 游戏核心：初始化全部管理器 |
| `LanCore.cs` | LAN 房间广播/发现（UDP 3044） |
| `NetCore.cs` | ENet 联网核心：建房/加入，`IsHost` 判定 |


### Creature/ — 生物实体

| 文件 | 说明 |
|------|------|
| `Equipment.cs` | 装备管理：主/副手加载切换、RPC 同步 |
| `Humanoid.cs` | 人形生物基类：双手挂点、装备数据（命名空间 Base） |
| `StateMachine.cs` | 统一状态机：三态 + 攻击连段 + 动画 RPC |


#### Creature/Npc/

| 文件 | 说明 |
|------|------|
| `Npc.cs` | NPC 实体：组装 AI/战斗/移动/感知/对话 |
| `NpcAI.cs` | NPC 决策：巡逻/追击（主机端） |
| `NpcBattle.cs` | NPC 战斗：攻击冷却、武器切换 |
| `NpcMovement.cs` | NPC 移动：导航 + 避障（主机端） |


#### Creature/Player/

| 文件 | 说明 |
|------|------|
| `Player.cs` | 玩家实体：Controller/GUI、交互射线、库存接口 |
| `PlayerCamera.cs` | 玩家相机：鼠标视角、场景切换相机归还（全局命名空间） |
| `PlayerController.cs` | 玩家输入：移动/跳跃/攻击/防御 |
| `PlayerGUI.cs` | UI 根容器（Layer=2000） |


### Data/ — Resource 数据类

| 文件 | 说明 |
|------|------|
| `CreatureData.cs` | 生物数据：属性/成长/阵营/背包/装备 |
| `InventoryData.cs` | 背包数据：JSON 序列化，含 `IsOpen` / `InteractPeer` |
| `ItemData.cs` | 物品数据（遗留命名空间） |
| `PlacedData.cs` | 放置物数据（全局命名空间） |
| `SaveData.cs` | 存档根：玩家字典 + 世界字典 |
| `SceneData.cs` | 场景数据：序列化已移除，数据常驻 Registry |
| `VegetationData.cs` | 植被数据：ID/血量/掉落表 |
| `WorldData.cs` | 世界数据：世界 ID + 场景数据字典 |


### Interface/ — 接口

| 文件 | 说明 |
|------|------|
| `IDamageable.cs` | 可受伤：`TakeDamage` |
| `IEquipmentHolder.cs` | 装备持有者 |
| `IInteractable.cs` | 可交互（遗留命名空间） |
| `IInventoryHolder.cs` | 库存持有者 |
| `ISerializable.cs` | 序列化：`Serialize` / `Deserialize` |


### Manager/ — 管理器

| 文件 | 说明 |
|------|------|
| `ConsoleManager.cs` | 控制台命令分发（全局命名空间） |
| `GUIManager.cs` | UI 工厂：注册/获取预制件 |
| `ItemManager.cs` | 物品资源管理 |
| `NetObjectInstance.cs` | 网络对象实例管理器：生成/销毁场景节点 |
| `PlayerManager.cs` | 玩家数据管理：数据字典 + 生成/持久化 |
| `ResourceManager.cs` | 资源扫描与注册 |
| `SaveManager.cs` | 存档 IO 服务（不持有运行时数据） |
| `SceneOwnerManager.cs` | 场景拥有者管理器：RPC 直连模式 |
| `TimeManager.cs` | 全局游戏时间 |
| `WorldManager.cs` | 世界数据管理：场景切换 + 存档导出 |


### NetWork/ — 网络同步

| 文件 | 说明 |
|------|------|
| `NetID.cs` | 网络对象唯一标识（PeerID + LocalSeqId + SceneHash） |
| `NetObject.cs` | 网络对象数据：`CustomData` 为 `byte[]`，含版本号 + 事件 |
| `NetObjectRegistry.cs` | 网络对象注册表：三段式数据同步 RPC（命名空间 Manager） |
| `NetSyncBase.cs` | 同步组件：RPC 注册/分发、`Request/SubmitCustomData` |
| `NetTransformSync.cs` | 变换同步：20Hz 上报 + 插值平滑 |
| `RpcGateway.cs` | RPC 网关：`Reliable` / `Unreliable` / `SceneReliable` 三入口 |


### Scene/ — 场景脚本

| 文件 | 说明 |
|------|------|
| `MainMenu.cs` | 主菜单场景（全局命名空间） |
| `MainWorld.cs` | 游戏主场景：出生点生成玩家 |
| `PlayerCreator.cs` | 角色创建：输入名字 → `PlayerManager.CreatePlayer` |


### UI/ — 通用 UI 控件

| 文件 | 说明 |
|------|------|
| `SlotUI.cs` | 格子控件：委托化读写 + 拖拽 + 装备槽校验 |


### Utils/ — 工具

| 文件 | 说明 |
|------|------|
| `CatLog.cs` | 分级彩色日志 |
| `CatUtils.cs` | 通用工具：稳定哈希、查找子节点、安全销毁 |


### View/ — UI 视图

| 文件 | 说明 |
|------|------|
| `ButtonView.cs` | 通用按钮视图 |
| `ConsoleView.cs` | 控制台视图（Layer=1000） |
| `DialogView.cs` | 对话视图（Layer=200） |
| `EquipmentView.cs` | 装备栏视图 |
| `EscView.cs` | ESC 菜单视图（Layer=900） |
| `HudView.cs` | HUD 视图：血条（Layer=5） |
| `InventoryView.cs` | 背包视图 |
| `MainMenuView.cs` | 主菜单视图 |
| `PlayMenuView.cs` | 大厅视图：联机入口 |
| `PlayerSaveDataView.cs` | 玩家存档视图（全局命名空间） |
| `ReticleView.cs` | 准星视图 |
| `WorldSaveDataView.cs` | 世界存档视图 |

## 关键架构

1. **主机权威 + RPC 广播**：客户端 `RPC_RequestXxx` → 主机校验 → `CallAllRpc("RPC_SyncXxx")` 广播。状态机（动画/连段）、装备、植被伤害均走此模式。

2. **对象同步链路**：
   - `NetObjectRegistry`（登记 NetID）→ `NetObjectInstance`（实例化，绑定 `NetSyncBase.NetID`）
   - `NetSyncBase` 拥有自己的 `RpcMap`，通过 `RpcGateway` 收发 RPC
   - `RpcGateway` 作为轻量路由器，根据 `NetID` 查找目标节点，实现跨场景 RPC 隔离
   - `NetTransformSync` 通过 `NetSyncBase` 注册/调用不可靠 RPC

3. **数据同步三段式 RPC 链路（2026.08.15 新增）**：
   - `Rpc_RequestCustomData`：客户端请求数据 → 服务器响应
   - `Rpc_ReceiveCustomData`：服务器下发数据（携带 `DataRevision`）
   - `Rpc_SubmitCustomData`：客户端提交修改（携带本地版本号）
   - `Rpc_AcknowledgeCustomData`：服务器确认提交成功
   - `DataRevision` 版本号防旧数据覆盖，提交版本低于权威版本时拒绝
   - 所有可交互对象（箱子/门/电梯/工作台）复用此链路

4. **场景数据管理（2026.08.18 更新）**：
   - 数据常驻 `NetObjectRegistry._netObjects`，场景切换时不删除
   - 场景索引 `_sceneNetObjectsList` 维护场景内所有 NetID 列表
   - `LoadNetObjects` 从内存恢复场景对象
   - `SaveSceneData` 从 `_netObjects` 导出数据到 `WorldData.SceneDataDict`

5. **场景持久化**：`SceneBase._EnterTree` 从 `WorldManager` 加载 `SceneData`；`WorldManager.SaveSceneData` 将场景数据写入 `WorldData.SceneDataDict`；`SaveManager` 通过 `SaveWorldDataDict` / `SavePlayerDataDict` 导出各管理器数据后写入磁盘。

6. **场景拥有者机制（2026.08.18 更新）**：
   - `SceneOwnerManager` 全局单例管理场景所有权（`Dictionary<int, long> _sceneOwners`）
   - 核心 RPC 链路：`Rpc_RequestSceneOwnership` / `Rpc_GrantSceneOwnership` / `Rpc_TransferOwnership` / `Rpc_QuerySceneActive`
   - 拥有者离开场景时自动触发 `TransferOwnership` → `BroadcastQuerySceneActive` → 存活者重新申请
   - 主机可夺取有主场景的拥有权

7. **场景级 RPC 通信**：
   - `Rpc_SceneReliable` 场景级 RPC 入口，通过 `sceneHash` 校验实现跨场景隔离
   - `RpcGateway` 对象级 RPC 同样增加 `sceneHash` 校验，过滤跨场景无效消息

8. **UI 架构**：`Player` 直接持有各 View（经 `GUIManager.GetView` 创建并注入 holder），`PlayerGUI`（Layer=2000）统一挂载；`SlotUI` 全委托化，不依赖场景树。

9. **状态机**：`AnimState / NpcState / PlayerState` 三态 + `Stance/Defense` 混合参数 + 攻击连段；动画状态经 RPC 同步，客户端仅表现。

10. **数据与序列化**：数据类为 `Resource`，`ItemData / InventoryData` 实现 `ISerializable`（JSON）供存档与传输；`InventoryData` 完整序列化 `IsOpen` / `InteractPeer` 状态；`CustomData` 改为 `byte[]` 彻底解决 `EncodedObjectAsId` 问题。

11. **跨场景单机切换**：`ScenePortalComp` 通过 `CallDeferred` 延迟执行切换，规避物理回调冲突；`PlayerManager.SpawnLocalPlayer` 自动检测无效引用并重新实例化；`PlayerCamera._ExitTree` 归还相机给 `WorldManager`，确保新场景相机正常激活。切换前调用 `SaveSceneData` 保存旧场景，避免数据丢失。

12. **玩家数据持久化**：`PlayerManager.EnterGame()` 统一游戏入口，从存档恢复 `LastSceneHash` / `LastPosition` / `LastRotation`；`MainWorld._Ready` 根据存档位置生成玩家，实现跨场景位置恢复闭环。

13. **管理器数据自治**：`WorldManager` 持有 `WorldDataDict`，`PlayerManager` 持有 `PlayerDataDict`；`SaveManager` 仅提供 IO 服务，不持有运行时数据。

14. **跨场景 RPC 隔离（2026.08.18 更新）**：`RpcGateway` 通过 `sceneHash` 校验跨场景消息，丢弃不匹配场景的 RPC；对象级 RPC 和场景级 RPC 均支持场景隔离。

15. **网络身份系统**：`NetID` 由 `PeerID` + `LocalSeqId` + `SceneHash` 三元组构成，`PeerID` 表示“登记/创建者”，与 `SceneOwnerManager` 的场景拥有者概念分离。

16. **统一 RPC 模式（2026.08.15 新增）**：
    - 主机和客户端走同一套 RPC 入口，通过 `CallLocal = true` 支持本地调用
    - 主机操作不再需要本地分支（`if (IsHost)`），统一通过 RPC 处理
    - 代码对称，逻辑一致，减少维护成本

17. **RPC 可靠性分类（2026.08.15 新增）**：
    - 可靠（`SendRpcToXxx`）：操作请求/响应、数据同步
    - 不可靠（`SendFastRpcToXxx`）：位置/旋转/变换同步
    - 调用方主动选择，避免误用


## 规范速记

- 注释：类/方法 `/// <summary>注：xxx</summary>`；属性行尾 `// xxx`；接口实现不加注释。
- 命名：公开属性无前缀（`MainHandData`）；私有字段 `_` 前缀（`_node3D`）；布尔 `Is/Can/Has`；查询 `GetXxx`；动作动词开头；RPC 方法 `RPC_` 前缀。
- RPC 可靠性：默认可靠（`SendRpcToXxx`），不可靠显式使用 `SendFastRpcToXxx`。


## 勘误与备注

- 命名空间与目录不一致：`Humanoid.cs` 位于 `Creature/` 但命名空间为 `途畔归所.Dll.Base`；`NetObjectRegistry.cs` 位于 `NetWork/` 但命名空间为 `途畔归所.Dll.Manager`。
- 遗留命名空间 `维修公司.Dll`：`ItemData`（`...Dll.data`）、`IInteractable`、`BuildComp`。
- 全局命名空间（无 namespace）：`ConsoleManager`、`MainMenu`、`ObstacleComp`、`PlacedData`、`PlayerCamera`、`PlayerSaveDataView`。
- 孤儿 .uid（源 .cs 已删，可清理）：`Base/AnimTreeControllerBase`、`Base/BoxBase`、`Base/HumanoidDataBase`、`Base/ICatInterfaceBase`、`Base/PlacedComp`、`Comp/InventoryComp`、`Comp/NavigationRegionComp`、`Comp/PhoneComp`、`Comp/VegetationComp`、`Core/NetworkCore`、`Creature/Npc/NpcPerception`、`Creature/Player/PlayerAttack`、`Data/NpcData`、`Interface/ISyncStateAnimation`、`ToolUtils`（共 15 个 .uid）。
- `SceneType` 已从 `SceneData` 移至 `SceneBase`，数据与表现分离。
- `NetID` 字段已重命名：`OwnerPeerID` → `PeerID`，语义从“拥有者”变为“登记/创建者”，与场景拥有者概念分离。
- `RpcGateway` 当前定位为轻量路由器，不持有 `RpcDict`，不处理业务逻辑；已新增 `Rpc_SceneReliable` 场景级 RPC 入口；`Rpc_Local` 已移除，`CallLocal = true` 直接在 `Rpc_Reliable` / `Rpc_Unreliable` 上处理。
- `NetObject.m_customData` 已从 `Variant` 改为 `byte[]`，彻底解决 `EncodedObjectAsId` 序列化问题。
- `NetObject` 已删除 `NetDataType` 枚举，类型校验由泛型 `T` 在调用层保证。
- `NetDataPackage` 新增统一封包结构（2026.08.15），所有可交互对象共享 `UserLockId` / `GlobalFlag`。实际实现中 `NetDataPackage` 已移除，改为业务层自行序列化。
- `ContainerComp` 已完成完整重构（2026.08.15），统一 RPC 模式 + 数据同步链路全部验证通过。
- `SceneOwnerManager` 已完成完整重构（2026.08.18），RPC 直连模式，移除所有 `TaskCompletionSource` 异步机制。
- `SceneBase.SetupSceneAsHost` / `SetupSceneAsClient` 已移除，改为 `OnOwnershipGranted` 统一入口。
- `SceneData.Serialize` / `Deserialize` 已移除，场景数据不再独立序列化，由 `WorldManager.SaveSceneData` 从 `NetObjectRegistry` 直接导出。
- `NetObjectManager` 已重命名为 `NetObjectInstance`（2026.08.19），职责明确为实例管理，避免与 `NetObjectRegistry` 混淆。