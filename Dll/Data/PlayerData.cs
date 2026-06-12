using Godot;
using System;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Data
{
    public partial class PlayerData : Resource
    {
        [Export] private int _playerID;

        public int m_playerID => _playerID = (_playerID == default) ? Math.Abs(Guid.NewGuid().GetHashCode()) : _playerID;

    }
}
