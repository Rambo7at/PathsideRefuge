using Godot;
using Godot.Collections;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Utils;
using static Godot.MultiplayerPeer;

namespace 途畔归所.Dll.Manager;

/// <summary>注：场景拥有者管理器，全局单例，负责管理各场景所有权归属</summary>
public partial class SceneOwnerManager : Node
{
    private static SceneOwnerManager _instance;
    public static SceneOwnerManager Instance => _instance ??= new();

    private Dictionary<int, long> _sceneOwners = []; // 场景拥有者字典：SceneHash → OwnerPeerID
    private bool _isSynced = false;                  // 客户端是否已完成首次全量同步

    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[SceneOwnerManager] 初始化完成");
    }


    /// <summary>注：尝试获取场景拥有者，若无主则由请求者自动占用</summary>
    public void TryAcquireOwnership(int sceneHash, long requestingPeer)
    {
        if (NetCore.Instance.IsHost)
        {
            _sceneOwners[sceneHash] = requestingPeer;
            CatLog.Ok($"[SceneOwnerManager] 主机直接接管{sceneHash}");
            return;
        }








    }

    /// <summary>注：清空所有场景拥有权（用于主机启动新游戏会话时重置）</summary>
    public void ClearAll()
    {
        _sceneOwners.Clear();
        _isSynced = false;
        CatLog.Debug("[SceneOwnerManager] 所有场景拥有权已清空");
    }



    /// <summary>注：客户端请求全量场景拥有者数据，异步等待回复</summary>
    public async Task WaitForAllOwnersAsync()
    {
        if (NetCore.Instance.IsHost) return;
        if (_isSynced) return;

        _isSynced = false;
        RpcId(NetCore.ServerID, nameof(Rpc_RequestAllOwners));

        // 等待主机回复（每帧检查，直到 _isSynced 为 true）
        while (!_isSynced)
        {
            await ToSignal(GetTree(), "process_frame");
        }

        CatLog.Ok($"[SceneOwnerManager] 场景拥有者数据同步完成，共 {_sceneOwners.Count} 个场景");
    }

    /// <summary>注：客户端请求全量场景拥有者数据（仅主机响应）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
    public void Rpc_RequestAllOwners()
    {
        if (NetCore.Instance.IsClient) return;

        long sendpeer = Multiplayer.GetRemoteSenderId();
        var data = new Dictionary<int, long>();

        foreach (var kvp in _sceneOwners)
        {
            data[kvp.Key] = kvp.Value;
        }

        RpcId(sendpeer, nameof(Rpc_ReceiveAllOwners), data);
        CatLog.Ok($"[SceneOwnerManager] 主机向 {sendpeer} 发送场景拥有者数据，共 {data.Count} 个场景");
    }

    /// <summary>注：主机发送全量场景拥有者数据给客户端（仅客户端接收）</summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    public void Rpc_ReceiveAllOwners(Dictionary<int, long> data)
    {
        if (NetCore.Instance.IsHost) return;

        _sceneOwners.Clear();

        foreach (var kvp in data)
        {
            _sceneOwners[kvp.Key] = kvp.Value;
        }

        _isSynced = true;
        CatLog.Ok($"[SceneOwnerManager] 客户端接收场景拥有者数据，共 {_sceneOwners.Count} 个场景");
    }

    /// <summary>注：主机宣告接管所有权（广播通知所有客户端）</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable)]
    public void Rpc_TakeOwnership(int sceneHash, long newOwner)
    {
        if (NetCore.Instance.IsHost) return;
        _sceneOwners[sceneHash] = newOwner;
        CatLog.Debug($"[SceneOwnerManager] 客户端接收场景 {sceneHash} 拥有者变更为 {newOwner}");
    }
}