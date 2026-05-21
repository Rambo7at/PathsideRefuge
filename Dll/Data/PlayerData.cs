using Godot;
using System;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Data
{
    public partial class PlayerData : Resource
    {

        [Export] private string _Name;

        [Export] private int _playerID;

        [Export] public float m_Speed = 5.0f;

        [Export] public float m_Jump = 4.5f;

        [Export] public InventoryData m_InventoryData ;


        public string m_Name { get => _Name; set { _Name = value; SetPlayerID(); } }

        public int m_PlayerID { get => _playerID; }

        private int SetPlayerID() => _playerID = (_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _playerID;

        public int GetInventoryItemCount()
        {
            if (m_InventoryData == null || m_InventoryData.m_SlotDatas.Count == 0) return 0;
            return m_InventoryData.m_SlotDatas.Count;
        }

        public PlayerData DeepCopy() => this.DuplicateDeep() as PlayerData;

    }
}
