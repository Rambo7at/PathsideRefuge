using Godot;
using Godot.Collections;
using System;

namespace 途畔归所.Dll.Data;

/// <summary>注：世界数据，包含世界名称、ID及所有场景数据</summary>
public partial class WorldData : Resource
{
    [Export] private int _worldID;         // 世界唯一ID，首次设置Name时自动生成
    [Export] private string _name;         // 世界名称

    [Export] public Dictionary<int, SceneData> SceneDataDict { get; set; } = [];  // 场景哈希 → 场景数据

    public int WorldID { get => _worldID; }                 // 只读，由 EnsureWorldID 生成
    public string Name { get => _name; set { _name = value; EnsureWorldID(); } }  // 设置名称时自动补全ID

    /// <summary>注：确保世界ID存在（首次设置Name时自动生成）</summary>
    private int EnsureWorldID() => _worldID = (_worldID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _worldID;

    /// <summary>注：深拷贝当前世界数据</summary>
    public WorldData DeepCopy() => this.DuplicateDeep() as WorldData;
}
