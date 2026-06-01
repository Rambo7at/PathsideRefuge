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

            if (_netSync.IsOwner == false) SetProcess(false);
        }

        public override void _Process(double delta)
        {

            if (NetCore.Instance.IsHost)
            {
                Rpc(nameof(Rpc_SyncAnimationState), _stateMachine.GetState());
            }
            else
            {
                RpcId(1, nameof(Rpc_ClientAnimReport), _stateMachine.GetState(), NetCore.Instance.LocalPeerID);
            }
        }


        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_ClientAnimReport(int state, long ignoreID)
        {
            if (NetCore.Instance.IsClient) return;

            Rpc(nameof(Rpc_SyncAnimationState), state, ignoreID);
        }


        [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        private void Rpc_SyncAnimationState(int state, long ignoreID = -1)
        {
            if (NetCore.Instance.IsHost || NetCore.Instance.LocalPeerID == ignoreID) return;

            _stateMachine.SetState(state);
        }
    }
}