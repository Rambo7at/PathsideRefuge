using Godot;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.NetWork
{
    [GlobalClass]
    public partial class NetAnimationSync : Node
    {
        [Export] private float _syncInterval = 0.1f;

        private float _timer;
        private NetSyncBase _sync;
        private StateMachine _stateMachine;

        public override void _Ready()
        {
            var parent = GetParent();
            if (parent is not Node3D) return;

            _sync = parent.GetNodeOrNull<NetSyncBase>("NetSyncBase");
            _stateMachine = parent.GetNodeOrNull<StateMachine>("StateMachine");

            if (_sync == null || !NetObjectManager.Instance.ContainsNetObject(_sync.m_NetObj.Id))
            {
                SetProcess(false);
                return;
            }
        }

        public override void _Process(double delta)
        {
            if (_sync == null || !_sync.IsOwner) return;

            _timer += (float)delta;
            if (_timer < _syncInterval) return;
            _timer = 0f;

            if (_stateMachine == null) return;
            int state = (int)_stateMachine.s_PlayerState;

            if (NetCore.Instance.IsHost)
            {
                // 主机广播给所有客户端
                Rpc(nameof(Rpc_SyncAnimationState),
                    _sync.m_NetObj.Id.UserID,
                    _sync.m_NetObj.Id.ID,
                    state);
            }
            else
            {
                // 客户端上报给主机
                RpcId(NetCore.ServerID, nameof(Rpc_ClientAnimReport),
                    _sync.m_NetObj.Id.UserID,
                    _sync.m_NetObj.Id.ID,
                    state);
            }
        }

        // 客户端上报动画状态给主机
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_ClientAnimReport(long userId, uint objId, int state)
        {
            if (!NetCore.Instance.IsHost) return;

            NetID netID = new(userId, objId);
            var target = NetObjectManager.Instance.GetNetObject(netID);
            if (target == null) return;

            // ★ 1. 更新主机本地的远程玩家状态机（让主机也能看到动画）
            var remoteStateMachine = target.GetNodeOrNull<StateMachine>("StateMachine");
            remoteStateMachine?.SwitchState((StateMachine.PlayerState)state);

            // ★ 2. 转发给其他客户端（排除发送者）
            long senderId = Multiplayer.GetRemoteSenderId();
            foreach (long peerId in Multiplayer.GetPeers())
            {
                if (peerId != senderId && peerId != NetCore.ServerID)
                {
                    RpcId(peerId, nameof(Rpc_SyncAnimationState), userId, objId, state);
                }
            }
        }

        // 所有客户端接收动画状态
        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_SyncAnimationState(long userId, uint objId, int state)
        {
            NetID netID = new(userId, objId);
            var target = NetObjectManager.Instance.GetNetObject(netID);
            if (target == null) return;

            var remoteStateMachine = target.GetNodeOrNull<StateMachine>("StateMachine");
            remoteStateMachine?.SwitchState((StateMachine.PlayerState)state);
        }
    }
}