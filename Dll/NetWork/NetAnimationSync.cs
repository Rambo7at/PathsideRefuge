using Godot;
using System.Linq;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.NetWork
{
    using global::途畔归所.Dll.Creature;
    using Godot;

    namespace 途畔归所.Dll.NetWork
    {
        [GlobalClass]
        public partial class NetAnimationSync : Node
        {
            [Export] private float _syncInterval = 0.1f;

            private float _timer;
            private NetSyncBase _sync;
            private AnimationTree _animTree;
            private StateMachine _stateMachine;

            public override void _Ready()
            {
                var parent = GetParent();
                if (parent == null || parent is not Node3D)
                {
                    GD.PrintErr("[NetAnimationSync] 父节点不是 Node3D");
                    SetProcess(false);
                    return;
                }

                _animTree = parent.GetNodeOrNull<AnimationTree>("AnimationTree");
                _sync = parent.GetNodeOrNull<NetSyncBase>("NetSyncBase");
                _stateMachine = parent.GetNodeOrNull<StateMachine>("StateMachine");

                if (_animTree == null)
                {
                    GD.PrintErr("[NetAnimationSync] 未找到 AnimationTree");
                    SetProcess(false);
                    return;
                }
                if (_sync == null)
                {
                    GD.PrintErr("[NetAnimationSync] 未找到 NetSyncBase");
                    SetProcess(false);
                    return;
                }
                // 注意：StateMachine 只在本地玩家存在，远程玩家可能没有，所以不加判死
            }

            public override void _Process(double delta)
            {
                if (_sync == null || !_sync.IsOwner) return;

                _timer += (float)delta;
                if (_timer < _syncInterval) return;
                _timer = 0f;

                if (!NetCore.Instance.IsHost) return;

                // 获取当前状态机的整数值（如果本地没有 StateMachine 则发 0）
                int state = _stateMachine != null ? (int)_stateMachine.s_PlayerState : 0;

                Rpc(nameof(Rpc_SyncAnimationState),
                    _sync.m_NetObj.Id.UserID,
                    _sync.m_NetObj.Id.ID,
                    state);
            }

            [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
            private void Rpc_SyncAnimationState(long userId, uint objId, int state)
            {
                NetID netID = new(userId, objId);
                var target = NetObjectManager.Instance.GetNetObject(netID);
                if (target == null) return;

                var animTree = target.GetNodeOrNull<AnimationTree>("AnimationTree");
                if (animTree == null) return;

                // 设置动画树的状态参数（需要你的动画树里有一个名为 "State" 的整数参数）
                animTree.Set("parameters/State/current", state);
            }
        }
    }
}