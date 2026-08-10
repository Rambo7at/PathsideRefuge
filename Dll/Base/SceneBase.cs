using Godot;
using System;
using System.Collections.Generic;
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
    public bool IsReady { get; private set; } = false;  // 场景是否已完成初始化/加载
    public long OwnerPeerID { get;  set; }
    public bool HasOwner => OwnerPeerID != 0;

    public Dictionary<string, Action<long, Variant>> RpcDict { get; set; } = [];

    public event Action OnSaveState;  // 触发时，订阅者应将自身状态保存到 NetObj.m_customData                          

    public override void _EnterTree()
    {
        WorldManager.Instance.SetCurrentSceneType(this);

        if (IsViewScene)
        {
            IsReady = true;
            return;
        }

        var peers = Multiplayer.GetPeers();

        // 注册场景级 RPC
        RpcDict["Rpc_RequestOwner"] = RpcGateway.Instance.MakeRpcHandler<int>(Rpc_RequestOwner);
        RpcDict["Rpc_ReplyOwner"] = RpcGateway.Instance.MakeRpcHandler(Rpc_ReplyOwner);
        RpcDict["Rpc_TakeOwnership"] = RpcGateway.Instance.MakeRpcHandler(Rpc_TakeOwnership);

        RpcGateway.Instance.SendSceneRpcBroadcast("Rpc_RequestOwner", SceneData.SceneHash);


        if (NetCore.Instance.IsHost)
        {
            OwnerPeerID = NetCore.Instance.LocalPeerID; // 这里不等待 回复，直接先将自己赋值进去，不影响后续流程
            RestoreNetObjects();
            IsReady = true;        // 如果是服务端，那么直接开始自己恢复

            return;
        }

        if (NetCore.Instance.IsClient)
        { 
        
        


        }
    }



    public override void _Ready()
    {
        






    }


    /// <summary>注：广播询问场景拥有者</summary>
    private void Rpc_RequestOwner(long senderId, int sceneHash)
    {
        if (WorldManager.Instance.CurrentSceneHash != sceneHash) return;
        if (OwnerPeerID != NetCore.Instance.LocalPeerID) return;

        RpcGateway.Instance.SendSceneRpcToPeer("Rpc_ReplyOwner", sceneHash, senderId);
    }

    /// <summary>注：接收拥有者回复</summary>
    private void Rpc_ReplyOwner(long senderId)
    {
        if (NetCore.Instance.IsClient)
        {
            OwnerPeerID = senderId;
            return;
        }

        OwnerPeerID = NetCore.Instance.LocalPeerID;
        RpcGateway.Instance.SendSceneRpcBroadcast("Rpc_TakeOwnership", SceneData.SceneHash);
    }

    /// <summary>注：服务器通知客户端取回所有权</summary>
    private void Rpc_TakeOwnership(long senderId)
    {
        if (NetCore.Instance.IsHost) return;
        OwnerPeerID = senderId;
    }








    //////////////下方代码不动

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

    public void DispatchRpc(string name, Variant variant)
    { 
    
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
