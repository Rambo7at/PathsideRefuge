using System.Collections.Generic;
using Godot;

namespace 途畔归所.Dll.NetWork
{
    /// <summary>注：NetObj.Vars 中使用的额外变量类型枚举 </summary>
    public enum NetVarType
    {
        Float,
        Int,
        String,
        Vector3,
        Quaternion,
        ByteArray
    }

    /// <summary>
    /// 网络对象的核心同步数据容器，对应 Valheim 的 ZDO。
    /// 持有需要跨网络同步的所有状态，包括 Transform、所有权和自定义变量。
    /// </summary>
    public class NetObject
    {
        // ── 核心身份与元数据 ──
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

        // ── 额外变量 (按类型分桶) ──
        /// <summary>
        /// 注：额外同步变量。外层 Key 为变量类型（Float, Int 等），
        /// 内层 Key 为变量名的哈希（int），Value 为实际数据。
        /// </summary>
        public Dictionary<NetVarType, Dictionary<int, object>> Vars { get; private set; }

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
            Vars = new Dictionary<NetVarType, Dictionary<int, object>>();
        }

        // ── 便捷判断与方法 ──
        /// <summary>注：判断指定 PeerID 是否为当前所有者 </summary>
        public bool IsOwner(long localPeerID) => OwnerPeerID == localPeerID;

        /// <summary>注：递增数据修订号，表明同步字段已发生变化 </summary>
        public void IncreaseDataRevision() => DataRevision++;

        /// <summary>注：递增所有权修订号，表明所有者已发生变化 </summary>
        public void IncreaseOwnerRevision() => OwnerRevision++;

        //══════════════════════════════════════════════════════════
        //  Vars 读写方法（按类型分桶，自动递增 DataRevision）
        //══════════════════════════════════════════════════════════

        /// <summary>注：获取稳定哈希码（不依赖运行时和引擎版本） </summary>
        private static int Hash(string key)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in key)
                    hash = hash * 31 + c;
                return hash;
            }
        }

        /// <summary>注：获取指定类型的变量桶，若不存在则自动创建 </summary>
        private Dictionary<int, object> GetBucket(NetVarType type)
        {
            if (!Vars.TryGetValue(type, out var bucket))
            {
                bucket = new Dictionary<int, object>();
                Vars[type] = bucket;
            }
            return bucket;
        }

        // ── 写入方法 ──
        /// <summary>注：写入一个 Float 类型变量 </summary>
        public void SetFloat(string key, float value)
        {
            var bucket = GetBucket(NetVarType.Float);
            int hash = Hash(key);
            if (!bucket.TryGetValue(hash, out var old) || (float)old != value)
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        /// <summary>注：写入一个 Int 类型变量 </summary>
        public void SetInt(string key, int value)
        {
            var bucket = GetBucket(NetVarType.Int);
            int hash = Hash(key);
            if (!bucket.TryGetValue(hash, out var old) || (int)old != value)
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        /// <summary>注：写入一个 String 类型变量 </summary>
        public void SetString(string key, string value)
        {
            var bucket = GetBucket(NetVarType.String);
            int hash = Hash(key);
            if (!bucket.TryGetValue(hash, out var old) || (string)old != value)
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        /// <summary>注：写入一个 Vector3 类型变量 </summary>
        public void SetVector3(string key, Vector3 value)
        {
            var bucket = GetBucket(NetVarType.Vector3);
            int hash = Hash(key);
            if (!bucket.TryGetValue(hash, out var old) || !((Vector3)old).Equals(value))
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        /// <summary>注：写入一个 Quaternion 类型变量 </summary>
        public void SetQuaternion(string key, Quaternion value)
        {
            var bucket = GetBucket(NetVarType.Quaternion);
            int hash = Hash(key);
            if (!bucket.TryGetValue(hash, out var old) || !((Quaternion)old).Equals(value))
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        /// <summary>注：写入一个 ByteArray 类型变量 </summary>
        public void SetByteArray(string key, byte[] value)
        {
            var bucket = GetBucket(NetVarType.ByteArray);
            int hash = Hash(key);
            // byte[] 比较需逐字节
            if (!bucket.TryGetValue(hash, out var old) || !ByteArrayEqual((byte[])old, value))
            {
                bucket[hash] = value;
                IncreaseDataRevision();
            }
        }

        private static bool ByteArrayEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        // ── 读取方法（带默认值） ──
        /// <summary>注：读取一个 Float 类型变量，不存在则返回默认值 </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (Vars.TryGetValue(NetVarType.Float, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (float)val;
            return defaultValue;
        }

        /// <summary>注：读取一个 Int 类型变量，不存在则返回默认值 </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (Vars.TryGetValue(NetVarType.Int, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (int)val;
            return defaultValue;
        }

        /// <summary>注：读取一个 String 类型变量，不存在则返回默认值 </summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (Vars.TryGetValue(NetVarType.String, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (string)val;
            return defaultValue;
        }

        /// <summary>注：读取一个 Vector3 类型变量，不存在则返回默认值 </summary>
        public Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            if (Vars.TryGetValue(NetVarType.Vector3, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (Vector3)val;
            return defaultValue;
        }

        /// <summary>注：读取一个 Quaternion 类型变量，不存在则返回默认值 </summary>
        public Quaternion GetQuaternion(string key, Quaternion defaultValue = default)
        {
            if (Vars.TryGetValue(NetVarType.Quaternion, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (Quaternion)val;
            return defaultValue;
        }

        /// <summary>注：读取一个 ByteArray 类型变量，不存在则返回默认值 </summary>
        public byte[] GetByteArray(string key, byte[] defaultValue = null)
        {
            if (Vars.TryGetValue(NetVarType.ByteArray, out var bucket) &&
                bucket.TryGetValue(Hash(key), out var val))
                return (byte[])val;
            return defaultValue;
        }
    }
}