using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    [Export] public SceneData SceneData { get; set; }          
    [Export] public E_SceneType SceneType { get; set; }
    private bool IsGameScene => SceneType == E_SceneType.GameScene;
    private bool IsViewScene => SceneType == E_SceneType.ViewScene;
    /// <summary>注：场景是否已完成初始化/加载 </summary>
    public bool IsReady { get; private set; } = false;
    /// <summary>注：场景拥有者的PeerID </summary>
    public long OwnerPeerID { get;  set; }

    public long SyncDataTargetPeer { get; set; }

    public System.Collections.Generic.Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];
                      
    public override  void _EnterTree()
    {
        WorldManager.Instance.SetCurrentSceneType(this);

        if (IsViewScene)
        {
            IsReady = true;
            return;
        }

        SceneOwnerManager.Instance.RequestSceneOwnership(SceneData.SceneHash, NetCore.Instance.LocalPeerID);
    }

    public override void _ExitTree()
    {
        if (IsViewScene) return;
        if (OwnerPeerID != NetCore.Instance.LocalPeerID) return;

        SceneOwnerManager.Instance.TransferOwnership(SceneData.SceneHash, NetCore.Instance.LocalPeerID);
    }

    public void OnOwnershipGranted(long ownerPeer)
    {
        OwnerPeerID = ownerPeer;

        if (NetCore.Instance.IsHost)
        {
            RestoreNetObjects();
            return;
        }

        NetObjectRegistry.Instance.RequestSceneData(SceneData.SceneHash);
    }

    /// <summary>注：从场景存档中恢复网络对象，跳过玩家对象（由 PlayerManager 独立管理）。</summary>
    private void RestoreNetObjects()
    {

        if (NetObjectRegistry.Instance.LoadNetObjects(SceneData.SceneHash))
        {
            IsReady = true;
            SceneData.IsNewScene = false;
            CatLog.Warn($"[RestoreNetObjects] 从内存恢复加载");
            return;
        }

        if (WorldManager.Instance.LoadSceneData(this) is not SceneData sceneData)
        {
            IsReady = true;
            return;
        }

        SceneData = sceneData;

        if (SceneData.NetObjectList.Count == 0)  // 标记：这里可能游玩过程中 场景内确实没东西
        {
            IsReady = true;
            SceneData.IsNewScene = false;
            return;
        }

        foreach (var netObject in SceneData.NetObjectList)
        {
            if (netObject.PrefabHash == PlayerManager.Instance.PlayerHash) continue;
            NetObjectManager.Instance.SpawnObject(netObject, netObject.Position, netObject.Rotation);
        }

        IsReady = true;
        SceneData.IsNewScene = false;
    }

    public void OnSceneDataReady(bool isNewScene)
    {
        SceneData.IsNewScene = isNewScene;
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


}
