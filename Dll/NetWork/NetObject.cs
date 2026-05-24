using System.Collections.Generic;
using Godot;

namespace 途畔归所.Dll.NetWork
{
    [GlobalClass]
    public partial class NetObject : Resource
    {
        public NetID Id { get; set; }
        [Export] public int PrefabHash { get; set; }
        [Export] public long OwnerPeerID { get; set; }
        [Export] public int sceneHash { get; set; }
        [Export] public Vector3 Position { get; set; }
        [Export] public Vector3 Rotation { get; set; }
        [Export] public Variant m_customData { get; set; }

        public NetObject() { }
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