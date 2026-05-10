using System.Collections.Generic;
using Godot;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>
    /// 网络对象的核心同步数据容器，对应 Valheim 的 ZDO。
    /// 持有需要跨网络同步的所有状态，包括 Transform、所有权和自定义变量。
    /// </summary>
    public class NetObject
    {
        /// <summary>注：网络对象的唯一标识符（NetID） </summary>
        public NetID Id { get; private set; }

        /// <summary>注：预制体路径的稳定哈希值，用于生成时查找场景文件 </summary>
        public int PrefabHash { get; set; }

        /// <summary>注：当前拥有该对象的 Peer ID（所有权可动态转移） </summary>
        public long OwnerPeerID { get; set; }

        // ── 版本号 ──
        /// <summary>注：数据修订号，任何同步字段变化时递增，用于检测更新 </summary>
        public uint DataRevision { get; set; }

        /// <summary>注：所有权修订号，所有者改变时递增，用于同步所有权变更 </summary>
        public ushort OwnerRevision { get; set; }

        // ── 同步字段 (Transform) ──
        /// <summary>注：世界坐标位置，会随移动同步更新 </summary>
        public Vector3 Position { get; set; }

        /// <summary>注：世界旋转，会随转向同步更新 </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>注：构造一个新的 NetObj，初始化所有字段并设置初始版本号 </summary>
        public NetObject(NetID id, Vector3 position, Quaternion rotation, int prefabHash, long ownerPeerID)
        {
            Id = id;
            Position = position;
            Rotation = rotation;
            PrefabHash = prefabHash;
            OwnerPeerID = ownerPeerID;
            DataRevision = 1;
            OwnerRevision = 1;
        }

        // ── 便捷判断与方法 ──
        /// <summary>注：判断指定 PeerID 是否为当前所有者 </summary>
        public bool IsOwner(long localPeerID) => OwnerPeerID == localPeerID;

        /// <summary>注：递增数据修订号，表明同步字段已发生变化 </summary>
        public void IncreaseDataRevision() => DataRevision++;

        /// <summary>注：递增所有权修订号，表明所有者已发生变化 </summary>
        public void IncreaseOwnerRevision() => OwnerRevision++;
    }
}