using Godot;
using Godot.Collections;
namespace 途畔归所.Dll.Data
{

	public partial class SaveData : Resource
	{
		[Export] public Dictionary<int, CreatureData> m_playerDataDict { get; set; } = [];

        [Export] public Dictionary<int, WorldData> m_worldDataDict { get; set; } = [];


        [Export] public int m_selPlayerIndex = default;

        [Export] public int m_selworldIndex = default;


        public int TryGetValidWorldDataKey() => GetFirstValidKey(m_worldDataDict);

        public int TryGetValidPlayerDataKey() => GetFirstValidKey(m_playerDataDict);


        private int GetFirstValidKey<[MustBeVariant] T>(Dictionary<int,T> keyValuePairs)
        {
            if (m_playerDataDict == null) return default;

            foreach (var data in m_playerDataDict)
            {
                if (data.Key == default || data.Value == null) continue;

                return data.Key;
            }

            return default;

        }

    }
}
