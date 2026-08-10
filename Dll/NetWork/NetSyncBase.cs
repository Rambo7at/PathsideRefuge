using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static System.Collections.Specialized.BitVector32;

namespace 途畔归所.Dll.NetWork;

[GlobalClass]
/// <summary>注：网络同步组件，管理RPC注册与调用分发、对象身份注册</summary>
public partial class NetSyncBase : Node
{
    private Node3D _node3D;
    private int _nodeHash;

    public NetObject NetObj { get; set; }
    public bool IsOwner => NetObj != null && NetObj.netId.OwnerPeerID == NetCore.Instance.LocalPeerID;

    public bool IsInit = false;

    public event Action OnSaveState;

    // ─── 每个 NetSyncBase 独立管理自己的 RPC 注册表 ──────────
    public Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];

    public override void _EnterTree()
    {
        var node = GetParent();
        if (node is not Node3D node3D)
        {
            CatLog.Err("[NetSyncBase._EnterTree]：挂载的组件对象，不是Node3D类型，已删除");
            node.QueueFree();
            return;
        }

        var scene = WorldManager.Instance.GetCurrentScene();
        RegisterManual(scene, node3D);
    }

    public override void _ExitTree()
    {
        if (NetObj == null) return;
        NetObjectRegistry.Instance.RemoveNet(NetObj.netId);
    }

    private void RegisterManual(SceneBase sceneBase, Node3D node3D)
    {
        _node3D = node3D;
        _nodeHash = CatUtils.GetStableHashCode(node3D.Name);

        if (NetObj == null)
        {
            if (sceneBase.SceneData.IsNewScene == false)
            {
                CatUtils.StopAndExit(node3D);
                return;
            }
            if (NetCore.Instance.IsHost)
            {
                var ID = NetObjectRegistry.Instance.RegisterObject(_nodeHash, _node3D.GlobalPosition, _node3D.GlobalRotation);
                var netobj = NetObjectRegistry.Instance.GetNetObject(ID);
                NetObj = netobj;
                CatLog.Ok("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
            }
            else
            {
                CatLog.Warn($"[NetSyncBase._Ready]：对象是客户端，已销毁{node3D.Name}");
                node3D.QueueFree();
                return;
            }

            // 场景哈希现在存储在 netId 中，不需要单独赋值
            // NetObj.sceneHash 已移除
        }

        sceneBase.OnSaveState += () => OnSaveState?.Invoke();
        IsInit = true;
    }

    // ─── RPC 注册（注册到自己的 RpcDict，而不是全局） ────────

    public void RegisterRpc(string name, Action action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc(string name, Action<long> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc<[MustBeVariant] T1>(string name, Action<long, T1> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2>(string name, Action<long, T1, T2> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(string name, Action<long, T1, T2, T3> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(string name, Action<long, T1, T2, T3, T4> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }

    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(string name, Action<long, T1, T2, T3, T4, T5> action)
    {
        if (!CheckBeforeRegisterRpc(name)) return;
        RpcDict[name] = RpcGateway.Instance.MakeRpcHandler(action);
    }


    /// <summary>注：公共注册前置检查，返回true代表可以继续注册</summary>
    private bool CheckBeforeRegisterRpc(string name)
    {
        if (RpcDict.ContainsKey(name))
        {
            CatLog.Warn($"[NetSyncBase.RegisterRpc]：重名RPC 方法{name}");
            return false;
        }
        return true;
    }



    // ─── RPC 发送（携带自己的 NetID 作为路由目标） ────────────

    public void SendRpcToHost(string name, params Variant[] args) => RpcGateway.Instance.SendRpcToHost(NetObj.netId, name, true, args);

    /// <summary>注：发送 RPC 给主机（指定可靠性）</summary>
    public void SendRpcToHost(string name, bool reliable, params Variant[] args) => RpcGateway.Instance.SendRpcToHost(NetObj.netId, name, reliable, args);

    /// <summary>注：发送 RPC 给指定对等端（默认可靠）</summary>
    public void SendRpcToPeer(string name, long targetPeerId, params Variant[] args) => RpcGateway.Instance.SendRpcToPeer(NetObj.netId, name, targetPeerId, true, args);

    /// <summary>注：发送 RPC 给指定对等端（指定可靠性）</summary>
    public void SendRpcToPeer(string name, long targetPeerId, bool reliable, params Variant[] args) => RpcGateway.Instance.SendRpcToPeer(NetObj.netId, name, targetPeerId, reliable, args);

    /// <summary>注：广播 RPC 给所有客户端（默认可靠）</summary>
    public void SendRpcBroadcast(string name, params Variant[] args) => RpcGateway.Instance.SendRpcBroadcast(NetObj.netId, name, true, args);

    /// <summary>注：广播 RPC 给所有客户端（指定可靠性）</summary>
    public void SendRpcBroadcast(string name, bool reliable, params Variant[] args) => RpcGateway.Instance.SendRpcBroadcast(NetObj.netId, name, reliable, args);


    // ─── RPC 分发（由 RpcGateway 调用） ──────────────────────

    public void DispatchRpc(string name, Variant variant)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        if (RpcDict.TryGetValue(name, out var action))
        {
            action?.Invoke(senderId, variant);
        }
        else
        {
            CatLog.Warn($"[NetSyncBase] 未注册的 RPC：{name}，目标：{NetObj?.netId}");
        }
    }
}