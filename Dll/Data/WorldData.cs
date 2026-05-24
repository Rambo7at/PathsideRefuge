using Godot;
using Godot.Collections;
using System;

namespace 途畔归所.Dll.Data
{
    public partial class WorldData : Resource
    {
        [Export] private int _worldID;

        [Export] private string _name;
        [Export] public Dictionary<int, SceneData> m_sceneDataDict { get; set; } = [];

        public int m_WorldID { get => _worldID; }
        public string m_name { get => _name; set { _name = value; SetWorldID(); } }
        private int SetWorldID() => _worldID = (_worldID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _worldID;



        public WorldData DeepCopy() => this.DuplicateDeep() as WorldData;
    }
}
