using System;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>注：网络对象的唯一标识符。 </summary>
    public struct NetID(long userID, uint id) : IEquatable<NetID>
    {
        public long UserID = userID;
        public uint ID = id;

        public override string ToString() => $"{UserID}:{ID}";

        public bool Equals(NetID other) => UserID == other.UserID && ID == other.ID;

        public static readonly NetID None = new NetID(0, 0);
        public static bool operator ==(NetID a, NetID b) => a.Equals(b);
        public static bool operator !=(NetID a, NetID b) => !a.Equals(b);

        public override bool Equals(object obj) => obj is NetID other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(UserID, ID);
    }
}