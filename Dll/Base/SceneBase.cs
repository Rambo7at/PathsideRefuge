using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static System.Collections.Specialized.BitVector32;

namespace 途畔归所.Dll.Base;

/// <summary>注：场景基类，所有游戏场景根节点需继承此类，负责场景的初始化、网络对象恢复与数据持久化</summary>
public partial class SceneBase : Node3D
{
    public enum E_SceneType
    {
        GameScene = 0,
        ViewScene = 1,
    }

    [Export] public SceneData SceneData { get; set; }          // 场景数据（场景名、哈希、网络对象列表等）
    [Export] public E_SceneType SceneType { get; set; }
    private bool IsGameScene => SceneType == E_SceneType.GameScene;
    private bool IsViewScene => SceneType == E_SceneType.ViewScene;
    public bool IsReady { get; private set; } = false;         // 场景是否已完成初始化/加载
    public long OwnerPeerID { get;  set; }

    public long SyncDataTargetPeer { get; set; }

    public System.Collections.Generic.Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];

    public event Action OnSaveState;  // 触发时，订阅者应将自身状态保存到 NetObj.m_customData                          

    public event Action OnOwnershipAcquired;

    public override async void _EnterTree()
    {
        WorldManager.Instance.SetCurrentSceneType(this);

        if (IsViewScene)
        {
            IsReady = true;
            return;
        }

        RpcDict[nameof(Rpc_GetSceneObject)] = RpcGateway.Instance.MakeRpcHandler(Rpc_GetSceneObject);
        RpcDict[nameof(Rpc_SendSceneObject)] = RpcGateway.Instance.MakeRpcHandler<byte[]>(Rpc_SendSceneObject);

        SetupSceneAsHost();
        SetupSceneAsClient();
    }

    private async void SetupSceneAsHost()
    {
        if (NetCore.Instance.IsClient) return;

        long host = await SceneOwnerManager.Instance.TryAcquireOwnership(SceneData.SceneHash, NetCore.Instance.LocalPeerID);
        OwnerPeerID = host;


        RestoreNetObjects();
        if (SyncDataTargetPeer != default)
        {
            RpcGateway.Instance.SendSceneRpcToPeer(nameof(Rpc_GetSceneObject), SceneData.SceneHash, SyncDataTargetPeer);
            SyncDataTargetPeer = default;
        }

        IsReady = true;
    }

    private async void SetupSceneAsClient()
    {
        if (NetCore.Instance.IsHost) return;
        long owner = await SceneOwnerManager.Instance.TryAcquireOwnership(SceneData.SceneHash, NetCore.Instance.LocalPeerID);

        RpcGateway.Instance.SendSceneRpcToPeer(nameof(Rpc_GetSceneObject), SceneData.SceneHash, owner);
        OwnerPeerID = owner;
        while (!IsReady)
        {
            await ToSignal(GetTree(), "process_frame");
        }
    }









    public override void _ExitTree()
    {
        if (IsViewScene) return;
        if (OwnerPeerID != NetCore.Instance.LocalPeerID) return;

        // 拥有者离开场景，转移所有权
        SceneOwnerManager.Instance.TransferOwnership(SceneData.SceneHash, NetCore.Instance.LocalPeerID);
        CatLog.Ok($"[SceneBase] 场景拥有者 {OwnerPeerID} 离开场景 {SceneData.SceneHash}，已触发所有权转移");
    }




    public void Rpc_GetSceneObject(long sendPeer)
    {
        SaveAllStates();
        RpcGateway.Instance.SendSceneRpcToPeer(nameof(Rpc_SendSceneObject), SceneData.SceneHash, sendPeer, SceneData.Serialize());
    }

    public void Rpc_SendSceneObject(long sendPeer, byte[] sceneData)
    {
        try
        {
            SceneData data = new();
            data.Deserialize(sceneData);
            NetObjectRegistry.Instance.RegisterObjectLocal(data.NetObjectList);
        }
        catch (Exception ex)
        {
            CatLog.Err($"[Rpc_SendSceneObject] 反序列化异常：{ex.Message}");
        }


        IsReady = true;
    }






    /// <summary>注：由 RpcGateway.Rpc_SceneReliable 调用，分发场景级 RPC</summary>
    public void DispatchRpc(string name, Variant variant)
    {
        long senderId = Multiplayer.GetRemoteSenderId();
        if (RpcDict.TryGetValue(name, out var action))
        {
            action?.Invoke(senderId, variant);
        }
        else
        {
            CatLog.Warn($"[SceneBase] 未注册的场景 RPC：{name}，场景：{SceneData?.SceneHash}");
        }
    }


    /// <summary>注：触发所有订阅者保存状态，并将场景标记为"非新场景"。</summary>
    public void SaveAllStates()
    {
        if (SceneType == E_SceneType.ViewScene) return;

        OnSaveState?.Invoke();

        var netObjects = NetObjectRegistry.Instance.GetNetObjectsForScene(SceneData.SceneHash);

        SceneData.NetObjectList.Clear();

        foreach (var obj in netObjects) SceneData.NetObjectList.Add(obj);

        SceneData.IsNewScene = false;
    }

    /// <summary>注：从场景存档中恢复网络对象，跳过玩家对象（由 PlayerManager 独立管理）。</summary>
    private void RestoreNetObjects()
    {
        if (WorldManager.Instance.LoadSceneData(this) is not SceneData sceneData) return;

        SceneData = sceneData.DeepCopy();

        if (SceneData.NetObjectList.Count == 0) return;

        foreach (var netObject in SceneData.NetObjectList)
        {
            if (netObject.PrefabHash == PlayerManager.Instance.PlayerHash) continue;
            NetObjectManager.Instance.SpawnObject(netObject, netObject.Position, netObject.Rotation);
        }

        IsReady = true;
    }

    /// <summary>注：调试打印场景数据详情（仅 debug 构建）</summary>
    private void DebugPrintSceneData()
    {
        if (SceneData == null)
        {
            CatLog.Debug("[SceneBase] SceneData 为空");
            return;
        }

        CatLog.Debug($"[SceneBase] ┌─ 场景数据详情 ──");
        CatLog.Debug($"[SceneBase] │ SceneName   : {SceneData.SceneName}");
        CatLog.Debug($"[SceneBase] │ SceneHash   : {SceneData.SceneHash}");
        CatLog.Debug($"[SceneBase] │ IsNewScene  : {SceneData.IsNewScene}");
        CatLog.Debug($"[SceneBase] │ NetObjectCount : {SceneData.NetObjectList.Count}");

        if (SceneData.NetObjectList.Count > 0)
        {
            for (int i = 0; i < SceneData.NetObjectList.Count; i++)
            {
                var obj = SceneData.NetObjectList[i];
                if (obj != null)
                {
                    CatLog.Debug($"[SceneBase] │ [{i}] PrefabHash={obj.PrefabHash}, {obj.netId}, Pos={obj.Position}");
                }
                else
                {
                    CatLog.Debug($"[SceneBase] │ [{i}] null");
                }
            }
        }
        CatLog.Debug($"[SceneBase] └─────────────────");
    }
}
