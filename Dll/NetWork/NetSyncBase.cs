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
		[Export] public Node3D prefab { get; set; }

		/// <summary> 是否为本地所有者 </summary>
		public bool IsOwner => NetObj != null && NetObj.IsOwner(NetCore.Instance.LocalPeerID);

		/// <summary> 该对象的唯一网络标识 </summary>
		public NetID NetID => NetObj?.Id ?? NetID.None;

		/// <summary> 存储注册的对象 RPC 方法，Key 为方法名哈希 </summary>
		private readonly Dictionary<int, Action<long, byte[]>> _rpcHandlers = new();

		public override void _Ready()
		{
			if (NetObj != null)return;
			if (prefab == null)
			{
				GD.PrintErr($"节点 {Name} 的 NetSyncBase 缺少 prefab");
				return;
			}

			int hash = CatUtils.GetStableHashCode(prefab.Name);

			if (!NetObjectManager.Instance.IsContainsPrefab(prefab.Name))
			{
				GD.PrintErr($"[NetSyncBase._Ready]： {prefab.Name} 没有在 NetObjectManager 中注册过 ");
				return;
			}

			if (NetCore.Instance.IsHost)
			{
				Transform3D gt = GlobalTransform;
				// ✅ 改用 NetObjectRegistry
				NetObjectRegistry.Instance.Spawn(hash, gt.Origin, gt.Basis.GetRotationQuaternion());

				GetParent()?.RemoveChild(this);
				QueueFree();
			}
			else
			{
				GetParent()?.RemoveChild(this);
				QueueFree();
			}
		}

		public void Setup(NetObject netobj)
		{
			NetObj = netobj;
			// ✅ 注册到 NetObjectRegistry
			NetObjectRegistry.Instance.RegisterNetSyncBase(this);

			// ✅ 监听 NetObjectRegistry 的数据变化事件
			//NetObjectRegistry.Instance.OnDataChanged += OnNetObjDataChanged;
		}

		public override void _ExitTree()
		{
			if (NetObjectRegistry.Instance != null)
			{
				//NetObjectRegistry.Instance.OnDataChanged -= OnNetObjDataChanged;
				NetObjectRegistry.Instance.UnregisterNetSyncBase(this);
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
            // 暂时禁用
            // byte[] payload = BuildObjectRpcPayload(methodName, args);
            // NetCore.Instance.SendRpcToPeer(NetObj.OwnerPeerID, "ObjRpc", payload);
            GD.PrintErr("[NetSyncBase] InvokeRpc 暂时不可用");
        }

		/// <summary> 向所有客户端广播 RPC </summary>
		public void InvokeRpcToAll(string methodName, byte[] args)
		{
            // 暂时禁用
            // byte[] payload = BuildObjectRpcPayload(methodName, args);
            // NetCore.Instance.BroadcastRpc("ObjRpc", payload);
            GD.PrintErr("[NetSyncBase] InvokeRpcToAll 暂时不可用");
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
