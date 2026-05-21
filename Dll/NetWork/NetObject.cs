using System.Collections.Generic;
using Godot;

namespace 途畔归所.Dll.NetWork
{
    public partial class NetObject : Resource
    {
        public NetID Id { get; private set; }
        [Export] public int PrefabHash { get; set; }
        [Export] public long OwnerPeerID { get; set; }
        [Export] public Vector3 Position { get; set; }
        [Export] public Vector3 Rotation { get; set; }

        public NetObject(NetID id, Vector3 position, Vector3 rotation, int prefabHash, long ownerPeerID)
        {
            Id = id;
            Position = position;
            Rotation = rotation;
            PrefabHash = prefabHash;
            OwnerPeerID = ownerPeerID;
        }

        public bool IsOwner(long localPeerID) => OwnerPeerID == localPeerID;
    }
}