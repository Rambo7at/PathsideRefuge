# RPC 跨场景同步改造 - 实施大纲

> **更新日期**：2026.08.12 | **当前阶段**：单机验证 ✅ → 多人改造 ✅ → 场景拥有者机制完成 🚀


## 1. 总目标

- 场景切换后，RPC 同步仅在同一场景内生效
- 跨场景玩家互相屏蔽 RPC 调用，杜绝异常报错
- 单机阶段已全部完成，现已进入多人跨场景同步改造阶段


## 2. 已完成（单机阶段 ✅）

- 单机场景切换全链路（`WorldManager.ChangeScene` + `ScenePortalComp` + 相机归还）
- 玩家数据持久化（`LastSceneHash` / `LastPosition` / `LastRotation`）
- 管理器架构重构（`SaveManager` 降级 IO，`WorldManager` / `PlayerManager` 数据自治）
- 装备 / 背包 / 武器跨场景完整保留验证通过
- 双向场景切换（主场景 ↔ 测试房屋）稳定运行

> 详细成果见开发计划书阶段五~七


## 3. 当前主线：多人跨场景 RPC 同步改造

### 3.1 场景拥有者管理

**实际实现**：`SceneOwnerManager` 维护 `Dictionary<int, long> _sceneOwners`（场景 → 拥有者映射）

| 操作 | 说明 |
|------|------|
| 主机进入场景 | 自动成为拥有者 |
| 客户端进入场景 | 向主机请求拥有者，主机分配或回复现有拥有者 |
| 无主场景 | 第一个请求的客户端成为拥有者 |
| 已有拥有者 | 新客户端记录拥有者并向其请求数据 |

### 3.2 场景拥有者转移机制 ✅

| 步骤 | 操作 |
|------|------|
| 1 | 拥有者离开场景 → 自动触发 `TransferOwnership` |
| 2 | 主机移除所有权 → 广播 `Rpc_RequestOccupants` 询问占据者 |
| 3 | 客户端回复 `Rpc_ReplyOccupant` → 主机分配新拥有者 |
| 4 | 主机广播 `Rpc_TakeOwnershipNotification` → 客户端更新本地 `OwnerPeerID` |

### 3.3 主机返回有主场景 ✅

- 主机回到场景时，检查 `NeedSyncData(sceneHash)`
- 如果场景已有其他拥有者 → 向该拥有者请求场景数据
- 场景无主或自己是拥有者 → 正常接管

### 3.4 快照同步机制

- 场景数据请求：客户端/主机 → `Rpc_GetSceneObject` → 拥有者
- 拥有者调用 `SaveAllStates()` 收集场景网络对象
- 拥有者回复 `Rpc_SendSceneObject`（携带 `SceneData` 序列化数据）
- 请求者反序列化 → `RegisterObjectLocal` 生成对象

### 3.5 RPC 场景隔离 ✅

| 隔离点 | 实现方式 |
|--------|----------|
| 场景级 RPC | `Rpc_SceneReliable` 通过 `sceneHash` 校验 |
| 对象级 RPC | 通过 `NetID.SceneHash` 路由，天然隔离 |
| 对象生成 | `HandleSpawned` 跨场景检查，过滤异场景对象 |


## 4. 核心 RPC 协议设计

### 4.1 场景拥有者请求

| RPC | 方向 | 职责 |
|-----|------|------|
| `Rpc_RequestOwners` | 客户端 → 主机 | 请求场景拥有者 |
| `Rpc_ReceiveAllOwners` | 主机 → 客户端 | 回复拥有者 PeerID |

### 4.2 场景所有权转移

| RPC | 方向 | 职责 |
|-----|------|------|
| `Rpc_NotifyLeave` | 客户端 → 主机 | 通知主机自己离开场景 |
| `Rpc_RequestOccupants` | 主机 → 所有客户端 | 询问谁还在场景里 |
| `Rpc_ReplyOccupant` | 客户端 → 主机 | 回复"我还在场景里" |
| `Rpc_TakeOwnershipNotification` | 主机 → 所有客户端 | 广播新拥有者接管 |

### 4.3 场景数据同步

| RPC | 方向 | 职责 |
|-----|------|------|
| `Rpc_GetSceneObject` | 请求者 → 拥有者 | 请求场景完整数据 |
| `Rpc_SendSceneObject` | 拥有者 → 请求者 | 发送序列化场景数据 |


## 5. 已完成验收 ✅

| 测试项 | 状态 |
|--------|------|
| 主机进入场景 → 成为拥有者 | ✅ |
| 客户端进入场景 → 获取拥有者 | ✅ |
| 客户端请求场景数据 → 生成对象 | ✅ |
| 主机离开场景 → 转移所有权 | ✅ |
| 客户端接管场景 → 成为新拥有者 | ✅ |
| 主机返回有主场景 → 向拥有者请求数据 | ✅ |
| 跨场景 RPC 隔离 | ✅ |
| 多个场景独立拥有者 | ✅ |
| `m_customData` 基础传输 | ✅ |


## 6. 后续待办

| 优先级 | 任务 | 状态 |
|--------|------|------|
| P1 | `m_customData` 完整验证（箱子库存等自定义数据） | ⬜ |
| P1 | 玩家断线时的所有权处理（`Multiplayer.PeerDisconnected`） | ⬜ |
| P2 | 传送过渡效果（淡入淡出 / 加载遮罩） | ⬜ |
| P2 | 地面物品场景切换保存（注册为 `NetObject`） | ⬜ |