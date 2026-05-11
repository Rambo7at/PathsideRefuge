using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{
    public partial class NetSyncBase : Node3D
    {

        private NetSyncBase _sync;

        public NetID m_NetID { get; set; }

        public long m_OwnerPeerID { get; set; }


        public bool IsOwner => m_OwnerPeerID == NetCore.Instance?.LocalPeerID;

        [Signal]
        public delegate void NetworkReadyEventHandler();

        public override void _Ready()
        {
            _sync = GetParent().GetNodeOrNull<NetSyncBase>("NetSyncBase");

            if (_sync == null)
            {
                SetProcess(false);
                return;
            }
        }

        public override void _Process(double delta)
        {

        }

        public void EmitNetworkReady()
        {
            GD.Print($"[NetSyncBase] NetworkReady emitted for NetID {m_NetID}");
            EmitSignal(SignalName.NetworkReady);
        }


    }
}
