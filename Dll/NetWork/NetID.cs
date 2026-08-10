using System;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：网络对象的唯一标识符，包含拥有者 PeerID、本地序列号及场景哈希</summary>
public struct NetID(long ownerPeerID, uint localSeqId, int sceneHash) : IEquatable<NetID>
{
    public long OwnerPeerID = ownerPeerID;   // 拥有此网络对象的对等端 ID
    public uint LocalSeqId = localSeqId;     // 该对等端自增的本地序列号（每注册一个对象 +1）
    public int SceneHash = sceneHash;        // 所属场景哈希

    public override string ToString() => $"Owner={OwnerPeerID}:{LocalSeqId} sceneHash={SceneHash}";

    public bool Equals(NetID other) => OwnerPeerID == other.OwnerPeerID && LocalSeqId == other.LocalSeqId && SceneHash == other.SceneHash;

    public static readonly NetID None = new NetID(0, 0, 0);
    public static bool operator ==(NetID a, NetID b) => a.Equals(b);
    public static bool operator !=(NetID a, NetID b) => !a.Equals(b);

    public override bool Equals(object obj) => obj is NetID other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(OwnerPeerID, LocalSeqId, SceneHash);
}
