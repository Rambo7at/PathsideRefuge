# RPC 跨场景同步改造 - 实施大纲
更新日期：2026.08.18 | 当前阶段：单机 ✅ → 双人联机 ✅ → 多场景拥有者转移 ✅

## 1. 总目标
- 场景切换后，RPC 同步仅在同一场景内生效
- 跨场景玩家互相屏蔽 RPC 调用，杜绝异常报错
- 支持场景拥有者转移（主机离开 → 客户端接管）
- 数据常驻内存，返回场景时从内存恢复

## 2. 场景拥有者管理（SceneOwnerManager）
### 2.1 核心数据结构
```csharp
private Dictionary<int, long> _sceneOwners = [];  // SceneHash → OwnerPeerID
```

### 2.2 核心 RPC 协议
| RPC | 方向 | 职责 |
|---|---|---|
| Rpc_RequestSceneOwnership | 客户端 → 服务器 | 请求拥有权（无主则分配，有主则通知） |
| Rpc_GrantSceneOwnership | 服务器 → 客户端 | 授予拥有权，触发 OnOwnershipGranted |
| Rpc_TransferOwnership | 客户端 → 服务器 | 拥有者离开，触发转移流程 |
| Rpc_QuerySceneActive | 服务器 → 客户端 | 询问场景中是否还有存活者 |

### 2.3 拥有权转移流程
```text
拥有者离开 → TransferOwnership → Rpc_TransferOwnership（服务器）
  → _sceneOwners.Remove(sceneHash)
  → BroadcastQuerySceneActive → 询问其他客户端
  → 存活者收到 Rpc_QuerySceneActive → RequestSceneOwnership
  → 无主场景 → 分配新拥有者
```

### 2.4 主机夺取逻辑
| 场景 | 行为 |
|---|---|
| 无主场景 | 第一个请求者成为拥有者 |
| 有主 + 主机申请 | 主机夺取拥有权 → 询问存活者 |
| 有主 + 普通客户端 | 回复当前拥有者（不覆盖） |

## 3. 场景数据管理（NetObjectRegistry）
### 3.1 数据结构
```csharp
private readonly Dictionary<NetID, NetObject> _netObjects = [];           // 所有数据
private readonly Dictionary<int, List<NetID>> _sceneNetObjectsList = [];  // 场景索引
```

### 3.2 数据常驻策略
- 数据在 NetObjectRegistry 中全局常驻
- 场景切换时数据不删除
- 返回场景时从内存恢复（LoadNetObjects）
- 新场景从磁盘存档加载（LoadSceneData）

### 3.3 数据恢复优先级
```text
1. 内存恢复：LoadNetObjects（_sceneNetObjectsList 索引）
2. 磁盘存档：LoadSceneData（WorldData.SceneDataDict）
3. 新场景：按场景预设生成
```

## 4. 场景生命周期
### 4.1 进入场景
```text
_EnterTree
  → SetCurrentSceneType（设置当前场景）
  → RequestSceneOwnership（申请拥有权）
  → Rpc_GrantSceneOwnership
  → OnOwnershipGranted
  → 主机：RestoreNetObjects（恢复数据）
  → 客户端：RequestSceneData（向服务器请求数据）
```

### 4.2 离开场景
```text
_ExitTree
  → SaveDataToNetObject（保存数据）
  → RemoveObject（清理实例）
  → TransferOwnership（转移拥有权）
```

### 4.3 返回场景
```text
返回场景时
  → LoadNetObjects（从内存恢复）
  → 数据已存在 → 跳过生成
  → 数据缺失 → 从磁盘加载
```

## 5. RPC 场景隔离
| 隔离点 | 实现方式 |
|---|---|
| 对象级 RPC | Rpc_Reliable / Rpc_Unreliable 校验 sceneHash |
| 场景级 RPC | Rpc_SceneReliable 通过 sceneHash 校验 |
| 对象生成 | HandleSpawned 跨场景检查，过滤异场景对象 |

## 6. 已完成验收
| 测试项 | 状态 |
|---|---|
| 单机场景切换 | ✅ |
| 主机 + 客户端同场景联机 | ✅ |
| 主机离开场景 → 客户端接管 | ✅ |
| 主机返回场景 → 从内存恢复 | ✅ |
| 主机先离开 → 客户端再加入 | ✅ |
| 跨场景 RPC 隔离 | ✅ |
| 对象销毁广播同步 | ✅ |

## 7. 后续待办
| 优先级 | 任务 | 状态 |
|---|---|---|
| P1 | NetObjectManager 命名优化 | ⬜ |
| P1 | 箱子数据同步在联机场景下的完整验证 | ⬜ |
| P2 | 玩家断线时的所有权处理 | ⬜ |
| P2 | 可交互对象扩展（门/电梯/工作台） | ⬜ |
| P2 | 地面物品场景切换保存 | ⬜ |
| P2 | 玩家数据服务器持久化 | ⬜ |