using System;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>注：网络对象的唯一标识符，由创建者 PeerID + 自增序号组成。 </summary>
    public struct NetID : IEquatable<NetID>
    {
        /// <summary>注：创建此对象的 Peer ID </summary>
        public long UserID;

        /// <summary>注：该 Peer 下的自增序号 </summary>
        public uint ID;

        /// <summary>注：使用指定的 PeerID 和序号构造 NetID </summary>
        public NetID(long userID, uint id)
        {
            UserID = userID;
            ID = id;
        }

        /// <summary>注：返回 "UserID:ID" 格式的字符串，方便调试与日志 </summary>
        public override string ToString() => $"{UserID}:{ID}";

        /// <summary>注：判断两个 NetID 是否完全相同（PeerID 与序号均相等） </summary>
        public bool Equals(NetID other) => UserID == other.UserID && ID == other.ID;

        /// <summary>注：与 object 比较，若非 NetID 类型则直接返回 false </summary>
        public override bool Equals(object obj) => obj is NetID other && Equals(other);

        /// <summary>注：获取该标识符的哈希值，用于在字典中快速查找 </summary>
        public override int GetHashCode() => HashCode.Combine(UserID, ID);

        /// <summary>注：重载 == 运算符，内部调用 Equals 比较两标识符 </summary>
        public static bool operator ==(NetID a, NetID b) => a.Equals(b);

        /// <summary>注：重载 != 运算符，与 == 相反 </summary>
        public static bool operator !=(NetID a, NetID b) => !a.Equals(b);

        /// <summary>注：定义一个统一的“空”标识符，PeerID=0, ID=0 </summary>
        public static readonly NetID None = new NetID(0, 0);
    }
}