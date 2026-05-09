using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>
    /// 网络视图组件，负责绑定 NetObj、所有权判断以及对象 RPC 的收发。
    /// 注意：本类继承 Node3D 以便直接获取全局 Transform（适用于直接作为根节点的场景）。
    /// 若是作为子组件挂载，请改用 Node 并通过父节点获取坐标。
    /// </summary>
    public partial class NetSyncBase : Node3D
    {
        /// <summary> 绑定的网络对象数据 </summary>
        public NetObject NetObj { get; private set; }

        /// <summary> 预制体路径（手工摆放时填写，用于自我网络化） </summary>
        [Export] public string PrefabPath { get; set; }

        /// <summary> 是否为本地所有者 </summary>
        public bool IsOwner => NetObj != null && NetObj.IsOwner(NetCore.Instance.LocalPeerID);

        /// <summary> 该对象的唯一网络标识 </summary>
        public NetID NetID => NetObj?.Id ?? NetID.None;

        /// <summary> 存储注册的对象 RPC 方法，Key 为方法名哈希 </summary>
        private readonly Dictionary<int, Action<long, byte[]>> _rpcHandlers = new();

        public override void _Ready()
        {
            // 如果已绑定 NetObj（由 Spawn 管线产生），跳过主动网络化逻辑
            if (NetObj != null)
                return;

            // ── 手工摆放模式 ──
            if (string.IsNullOrEmpty(PrefabPath))
            {
                GD.PrintErr($"节点 {Name} 的 NetSyncBase 缺少 PrefabPath 导出字段");
                return;
            }

            ItemManager itemMgr = ItemManager.Instance;
            if (itemMgr == null)
            {
                GD.PrintErr("ItemManager 未初始化，无法进行自我网络化");
                return;
            }

            if (!itemMgr.HasPrefab(PrefabPath))
            {
                GD.PrintErr($"PrefabPath \"{PrefabPath}\" 未在 ItemManager 中注册，当作静态装饰");
                return;
            }

            if (NetCore.Instance.IsHost)
            {
                // 主机：自我 Spawn 并销毁当前占位节点
                Transform3D gt = GlobalTransform;
                NetCore.Instance.Spawn(PrefabPath, gt.Origin, gt.Basis.GetRotationQuaternion());

                // 安全移除自己（延迟销毁，避免在树遍历中直接删除）
                GetParent()?.RemoveChild(this);
                QueueFree();
            }
            else
            {
                // 客户端：直接销毁占位节点，等待主机 ObjCreate 生成真正的网络对象
                GetParent()?.RemoveChild(this);
                QueueFree();
            }
        }

        /// <summary> 绑定网络对象，注册到 NetCore 映射表并监听数据变化 </summary>
        public void Setup(NetObject netobj)
        {
            NetObj = netobj;
            NetCore.Instance.RegisterNetSyncBase(this);

            NetCore.Instance.OnDataChanged += OnNetObjDataChanged;
        }

        public override void _ExitTree()
        {
            if (NetCore.Instance != null)
            {
                NetCore.Instance.OnDataChanged -= OnNetObjDataChanged;
                NetCore.Instance.UnregisterNetSyncBase(this);
            }
        }

        /// <summary> 注册对象 RPC 处理方法 </summary>
        public void RegisterRpc(string methodName, Action<long, byte[]> handler)
        {
            int hash = methodName.GetStableHashCode();
            _rpcHandlers[hash] = handler;
        }

        /// <summary> 向所有者发送 RPC </summary>
        public void InvokeRpc(string methodName, byte[] args)
        {
            byte[] payload = BuildObjectRpcPayload(methodName, args);
            NetCore.Instance.SendRpcToPeer(NetObj.OwnerPeerID, "ObjRpc", payload);
        }

        /// <summary> 向所有客户端广播 RPC </summary>
        public void InvokeRpcToAll(string methodName, byte[] args)
        {
            byte[] payload = BuildObjectRpcPayload(methodName, args);
            NetCore.Instance.BroadcastRpc("ObjRpc", payload);
        }

        /// <summary> 由 NetCore 调用，分发到达的对象 RPC </summary>
        public void ReceiveObjectRpc(long senderId, string methodName, byte[] args)
        {
            int hash = methodName.GetStableHashCode();
            if (_rpcHandlers.TryGetValue(hash, out var handler))
                handler(senderId, args);
        }

        /// <summary> 当 NetObj 数据更新时触发，子类可重写以驱动动画等 </summary>
        protected virtual void OnNetObjDataChanged(NetID id)
        {
            if (id == NetID)
            {
                // 子组件（如 NetTransformSync）可通过此事件更新自己
            }
        }

        /// <summary> 构建对象 RPC 负载（含 NetID + 方法名 + 参数） </summary>
        private byte[] BuildObjectRpcPayload(string methodName, byte[] args)
        {
            using var stream = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(stream);
            writer.Write(NetID.UserID);
            writer.Write(NetID.ID);
            writer.Write(methodName);
            writer.Write(args.Length);
            writer.Write(args);
            return stream.ToArray();
        }
    }
}