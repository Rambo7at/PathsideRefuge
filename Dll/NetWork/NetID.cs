using System;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：网络对象的唯一标识符，包含拥有者 PeerID、本地序列号及场景哈希</summary>
public struct NetID : IEquatable<NetID>
{
    public long PeerID;   // 登记此网络对象的对等端 ID（创建者）
    public uint LocalSeqId ;     // 该对等端自增的本地序列号（每注册一个对象 +1）
    public int SceneHash;        // 所属场景哈希

    public bool IsNone => PeerID == default && SceneHash == default;


    public NetID(long peerID, uint localSeqId, int sceneHash)
    {
        PeerID = peerID;
        LocalSeqId = localSeqId;
        SceneHash = sceneHash;
    }

    public override string ToString() => $"Owner={PeerID}:{LocalSeqId} sceneHash={SceneHash}";

    public bool Equals(NetID other) => PeerID == other.PeerID && LocalSeqId == other.LocalSeqId && SceneHash == other.SceneHash;

    public static readonly NetID None = new NetID(0, 0, 0);
    public static bool operator ==(NetID a, NetID b) => a.Equals(b);
    public static bool operator !=(NetID a, NetID b) => !a.Equals(b);

    public override bool Equals(object obj) => obj is NetID other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(PeerID, LocalSeqId, SceneHash);



}
