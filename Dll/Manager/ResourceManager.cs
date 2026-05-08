using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Manager
{
	/// <summary>注：资源管理器</summary>
	public class ResourceManager
	{

		private static ResourceManager _instance;

		public static ResourceManager Instance => _instance ??= new ResourceManager();


		private Array<PackedScene> _ResourceList = new Array<PackedScene>();

		public PackedScene m_PlayerPrefab { get;  set; }
		public Array<PackedScene> m_ItemAssetList { get; private set; } = [];
		public Array<PackedScene> m_UIAssetList { get; private set; } = [];
        public Array<PackedScene> m_PlacedAssetList { get; private set; } = [];

        private ResourceManager() { }


		public void Init()
		{
			LoadAsset("res://Prefab/Player/player.tscn");

			LoadAsset("res://Prefab/Item/et_牛奶罐.tscn");
            LoadAsset("res://Prefab/Item/7at_匕首.tscn");





            LoadAsset("res://Prefab/UI/ESC/esc_ui.tscn");
			LoadAsset("res://Prefab/UI/ConsoleUI.tscn");
			LoadAsset("res://Prefab/UI/背包/InventoryUI.tscn");
			LoadAsset("res://Prefab/UI/格子/slot_ui.tscn");
			LoadAsset("res://Prefab/UI/主菜单/存档界面/存档信息.tscn");
            LoadAsset("res://Prefab/UI/容器/ContainerUI.tscn");
            CategorizeAssets();
			GD.Print("[ResourceManager] 初始化完成");
		}

		public void CategorizeAssets()
		{
			foreach (var asset in _ResourceList)
			{

				if (asset == null) continue;

				Node instance = asset.Instantiate();
				if (instance == null) continue;

				if (instance is Player)
				{
					m_PlayerPrefab = asset;           // 只有一个玩家预设
				}
				else if (instance is ItemComp)
				{
                    m_ItemAssetList.Add(asset);
				}
				else if (instance is UIPanelBase)    // 所有 UI 都继承 UIPanelBase
				{
                    m_UIAssetList.Add(asset);
				}
				else if (instance is PlacedComp)
				{
                    m_PlacedAssetList.Add(asset);

                }
				else
				{
					GD.Print($"[ResourceManager] 未能归类的资源：{asset.ResourcePath}");
				}

				if (!instance.IsQueuedForDeletion()) instance.QueueFree();

			}

		}



		private void LoadAsset(string res)
		{
			var scene = ResourceLoader.Load<PackedScene>(res);

			if (scene != null) _ResourceList.Add(scene);
			else GD.PrintErr($"[ResourceManager.LoadAsset]：资源加载失败资源检查路径：{res}");
		}

	}
}
