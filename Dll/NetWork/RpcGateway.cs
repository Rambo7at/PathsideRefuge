using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：RPC 网关，提供发送、接收与委托工厂能力</summary>
public partial class RpcGateway : Node
{
    private static RpcGateway _instance;
    public static RpcGateway Instance => _instance ??= new();

    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[RpcGateway] 初始化完成");
    }


    /// <summary>注：对象级可靠 RPC 接收入口（通过 NetID 路由到 NetSyncBase，供 NetObject 体系使用）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Rpc_Reliable(long ownerPeer, uint seqId, int sceneHash, string name, Variant variant)
    {
        NetID targetId = new(ownerPeer, seqId, sceneHash);

        var node = NetObjectManager.Instance.GetNetObject(targetId);
        if (node == null)
        {
            return;
        }

        if (CatUtils.FindChildNode<NetSyncBase>(node) is not NetSyncBase sync)
        {
            return;
        }

        sync.DispatchRpc(name, variant);
    }

    /// <summary>注：对象级不可靠 RPC 接收入口（通过 NetID 路由到 NetSyncBase，供 NetObject 体系使用）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void Rpc_Unreliable(long ownerPeer, uint seqId, int sceneHash, string name, Variant variant)
    {
        NetID targetId = new(ownerPeer, seqId, sceneHash);

        var node = NetObjectManager.Instance.GetNetObject(targetId);
        if (node == null)
        {
            return;
        }

        if (CatUtils.FindChildNode<NetSyncBase>(node) is not NetSyncBase sync)
        {
            return;
        }

        sync.DispatchRpc(name, variant);
    }

    /// <summary>注：场景级可靠 RPC 接收入口（直接路由到当前场景的 SceneBase，供场景自身使用）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void Rpc_SceneReliable(string name,int sceneHash, Variant variant)
    {
        if (WorldManager.Instance.CurrentSceneHash != sceneHash) return;


        if (WorldManager.Instance.GetCurrentScene() is not SceneBase scene) return;

        scene.DispatchRpc(name, variant);
    }

    // ─── RPC 委托工厂（生成网关可调用的 Action<long, Variant>） ──────────────────

    /// <summary>注：创建 0 参数 RPC 委托</summary>
    public Action<long, Variant> MakeRpcHandler(Action action) => (id, _) => action();

    /// <summary>注：创建 1 参数 RPC 委托（仅 senderId）</summary>
    public Action<long, Variant> MakeRpcHandler(Action<long> action) => (id, _) => action(id);

    /// <summary>注：创建 1 参数 RPC 委托（带值）</summary>
    public Action<long, Variant> MakeRpcHandler<[MustBeVariant] T1>(Action<long, T1> action) => (id, value) => action(id, value.As<T1>());

    /// <summary>注：创建 2 参数 RPC 委托</summary>
    public Action<long, Variant> MakeRpcHandler<[MustBeVariant] T1, [MustBeVariant] T2>(Action<long, T1, T2> action)
    {
        return (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();
            if (arr == null || arr.Count < 2) return;
            action(id, arr[0].As<T1>(), arr[1].As<T2>());
        };
    }

    /// <summary>注：创建 3 参数 RPC 委托</summary>
    public Action<long, Variant> MakeRpcHandler<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(Action<long, T1, T2, T3> action)
    {
        return (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();
            if (arr == null || arr.Count < 3) return;
            action(id, arr[0].As<T1>(), arr[1].As<T2>(), arr[2].As<T3>());
        };
    }

    /// <summary>注：创建 4 参数 RPC 委托</summary>
    public Action<long, Variant> MakeRpcHandler<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(Action<long, T1, T2, T3, T4> action)
    {
        return (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();
            if (arr == null || arr.Count < 4) return;
            action(id, arr[0].As<T1>(), arr[1].As<T2>(), arr[2].As<T3>(), arr[3].As<T4>());
        };
    }

    /// <summary>注：创建 5 参数 RPC 委托</summary>
    public Action<long, Variant> MakeRpcHandler<[MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(Action<long, T1, T2, T3, T4, T5> action)
    {
        return (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();
            if (arr == null || arr.Count < 5) return;
            action(id, arr[0].As<T1>(), arr[1].As<T2>(), arr[2].As<T3>(), arr[3].As<T4>(), arr[4].As<T5>());
        };
    }


    /// <summary>注：发送对象级 RPC 给主机（通过 NetID 定位）</summary>
    public void SendRpcToHost(NetID target, string name, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));

        if (reliable)
        {
            RpcId(1, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
        else
        {
            RpcId(1, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
    }

    /// <summary>注：发送对象级 RPC 给指定对等端（通过 NetID 定位）</summary>
    public void SendRpcToPeer(NetID target, string name, long targetPeerId, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));
        if (reliable)
        {
            RpcId(targetPeerId, nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
        else
        {
            RpcId(targetPeerId, nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
    }

    /// <summary>注：广播对象级 RPC 给所有客户端（通过 NetID 定位）</summary>
    public void SendRpcBroadcast(NetID target, string name, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));
        if (reliable)
        {
            Rpc(nameof(Rpc_Reliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
        else
        {
            Rpc(nameof(Rpc_Unreliable), target.OwnerPeerID, target.LocalSeqId, target.SceneHash, name, payload);
        }
    }

    /// <summary>注：发送场景级 RPC 给主机</summary>
    public void SendSceneRpcToHost(string name, int sceneHash, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));

        if (reliable)
        {
            RpcId(1, nameof(Rpc_SceneReliable), name, sceneHash, payload);
        }
        else
        {
            // 场景级目前只支持可靠传输，暂不实现不可靠
            RpcId(1, nameof(Rpc_SceneReliable), name, sceneHash, payload);
        }
    }

    /// <summary>注：发送场景级 RPC 给指定对等端</summary>
    public void SendSceneRpcToPeer(string name, int sceneHash, long targetPeerId, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));

        RpcId(targetPeerId, nameof(Rpc_SceneReliable), name, sceneHash, payload);
    }

    /// <summary>注：广播场景级 RPC 给所有客户端</summary>
    public void SendSceneRpcBroadcast(string name, int sceneHash, bool reliable = true, params Variant[] args)
    {
        Variant payload = args.Length == 0 ? default : (args.Length == 1 ? args[0] : new Godot.Collections.Array(args));

        Rpc(nameof(Rpc_SceneReliable), name, sceneHash, payload);
    }

}