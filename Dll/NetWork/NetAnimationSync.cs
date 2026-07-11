using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Creature.Npc.Npc;

namespace 途畔归所.Dll.NetWork
{
    [GlobalClass]
    public partial class NetAnimationSync : Node
    {
        private ISyncStateMachine _stateMachine;
        private NetSyncBase _netSync;

        public override void _Ready()
        {
            if (GetParent() is not Node3D parent)
            {
                CatUtils.StopAndExit(this);
                return;
            }

            foreach (var node in parent.GetChildren())
            {
                if (node is ISyncStateMachine syncStateMachine) _stateMachine = syncStateMachine;
                if (node is NetSyncBase netSync) _netSync = netSync;
            }

            if (_stateMachine == null || _netSync == null)
            {
                CatUtils.StopAndExit(this);
                return;
            }

            if (!_netSync.IsOwner) return;

            _stateMachine.OnAnimStateChanged += OnStateChanged;
            _stateMachine.OnOneShotChanged += OnOneShotTriggered;
            _stateMachine.OnComboRequested += OnComboTriggered;  // 新增
        }

        private void OnStateChanged()
        {
            int state = _stateMachine.GetAnimState();
            if (NetCore.Instance.IsHost)
                Rpc(nameof(Rpc_SyncAnimationState), state, -1);
            else
                RpcId(1, nameof(Rpc_ClientAnimReport), state, NetCore.Instance.LocalPeerID);
        }

        private void OnOneShotTriggered()
        {
            if (NetCore.Instance.IsHost)
                Rpc(nameof(Rpc_SyncOneShot), -1);
            else
                RpcId(1, nameof(Rpc_ClientOneShotReport), NetCore.Instance.LocalPeerID);
        }

        private void OnComboTriggered()
        {
            if (NetCore.Instance.IsHost)
                Rpc(nameof(Rpc_SyncCombo), -1);
            else
                RpcId(1, nameof(Rpc_ClientComboReport), NetCore.Instance.LocalPeerID);
        }



        // 移动状态同步
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_ClientAnimReport(int state, long ignoreID)
        {
            if (NetCore.Instance.IsClient) return;
            Rpc(nameof(Rpc_SyncAnimationState), state, ignoreID);
            _stateMachine.SetAnimState(state);
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_SyncAnimationState(int state, long ignoreID = -1)
        {
            if (NetCore.Instance.LocalPeerID == ignoreID) return;
            if (_netSync.IsOwner) return;
            _stateMachine.SetAnimState(state);
        }

        // OneShot 同步
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void Rpc_ClientOneShotReport(long ignoreID)
        {
            if (NetCore.Instance.IsClient) return;
            Rpc(nameof(Rpc_SyncOneShot), ignoreID);
            _stateMachine.TriggerOneShot();   // 直接触发
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void Rpc_SyncOneShot(long ignoreID = -1)
        {
            if (NetCore.Instance.LocalPeerID == ignoreID) return;
            if (_netSync.IsOwner) return;
            _stateMachine.TriggerOneShot();
        }

        // 连段同步 RPC
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void Rpc_ClientComboReport(long ignoreID)
        {
            if (NetCore.Instance.IsClient) return;
            Rpc(nameof(Rpc_SyncCombo), ignoreID);
            _stateMachine.TriggerCombo();
        }

        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        private void Rpc_SyncCombo(long ignoreID = -1)
        {
            if (NetCore.Instance.LocalPeerID == ignoreID) return;
            if (_netSync.IsOwner) return;
            _stateMachine.TriggerCombo();
        }
    }
}