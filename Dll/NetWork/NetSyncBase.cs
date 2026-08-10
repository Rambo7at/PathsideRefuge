using Godot;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

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
        => RpcDict[name] = (id, _) => action();

    public void RegisterRpc(string name, Action<long> action)
        => RpcDict[name] = (id, _) => action(id);

    public void RegisterRpc<[MustBeVariant] T>(string name, Action<long, T> action)
        => RpcDict[name] = (id, value) => action(id, value.As<T>());

    public void RegisterRpc<[MustBeVariant] T>(string name, Action<T> action)
        => RpcDict[name] = (id, value) => action(value.As<T>());

    public void RegisterRpc<[MustBeVariant] T1, [MustBeVariant] T2>(string name, Action<long, T1, T2> action)
    {
        RpcDict[name] = (id, value) =>
        {
            var arr = value.As<Godot.Collections.Array>();
            if (arr == null || arr.Count < 2) return;
            action(id, arr[0].As<T1>(), arr[1].As<T2>());
        };
    }

    // ─── RPC 发送（携带自己的 NetID 作为路由目标） ────────────

    public void CallRpc(string name, bool reliable = true)
        => RpcGateway.Instance.CallRpc(NetObj.netId, name, reliable);

    public void CallRpc(string name, Variant value, bool reliable = true)
        => RpcGateway.Instance.CallRpc(NetObj.netId, name, value, reliable);

    public void CallRpc(string name, Variant value, long targetPeerId, bool reliable = true)
        => RpcGateway.Instance.CallRpc(NetObj.netId, name, value, targetPeerId, reliable);

    public void CallRpc(string name, Variant v1, Variant v2, bool reliable = true)
        => RpcGateway.Instance.CallRpc(NetObj.netId, name, v1, v2, reliable);

    public void CallRpc(string name, Variant v1, Variant v2, long targetPeerId, bool reliable = true)
        => RpcGateway.Instance.CallRpc(NetObj.netId, name, v1, v2, reliable);

    public void CallAllRpc(string name, bool reliable = true)
        => RpcGateway.Instance.CallAllRpc(NetObj.netId, name, reliable);

    public void CallAllRpc(string name, Variant value, bool reliable = true)
        => RpcGateway.Instance.CallAllRpc(NetObj.netId, name, value, reliable);

    public void CallAllRpc(string name, Variant v1, Variant v2, bool reliable = true)
        => RpcGateway.Instance.CallAllRpc(NetObj.netId, name, v1, v2, reliable);

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