using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static Godot.WebSocketPeer;

namespace 途畔归所.Dll.Manager
{
    /// <summary>注：资源管理器</summary>
    public class ResourceManager
    {
        private static ResourceManager _instance;

        public static ResourceManager Instance => _instance ??= new ResourceManager();

        private Array<PackedScene> _resourceList = [];
        public Array<PackedScene> m_UIAssetList { get; private set; } = [];

        private ResourceManager() { }

        public void Init()
        {
            LoadAsset("res://Prefab/Player/player.tscn");
            LoadAsset("res://Prefab/Item/et_牛奶罐.tscn");
            LoadAsset("res://Prefab/Item/7at_匕首.tscn");
            LoadAsset("res://Prefab/Item/et_木材.tscn");
            LoadAsset("res://Prefab/Item/7at_空拳头.tscn");
            LoadAsset("res://Prefab/Npc/Npc.tscn");
            LoadAsset("res://Prefab/Piece/et_板条箱.tscn");

            LoadAsset("res://Prefab/Vegetation/ET-树.tscn");

            LoadAsset("res://Prefab/View/HUD/hud.tscn");
            LoadAsset("res://Prefab/View/ESC/esc_ui.tscn");
            LoadAsset("res://Prefab/View/ConsoleUI.tscn");
            LoadAsset("res://Prefab/View/储物/InventoryUI.tscn");
            LoadAsset("res://Prefab/View/格子/slot_ui.tscn");
            LoadAsset("res://Prefab/View/Button/Button_A1.tscn");
            LoadAsset("res://Prefab/View/容器/ContainerUI.tscn");
            LoadAsset("res://Prefab/View/装备栏/EquipUI.tscn");


            LoadAsset("res://Scenes/主菜单.tscn");
            LoadAsset("res://Scenes/测试场景.tscn");
            LoadAsset("res://Scenes/角色创建.tscn");


            RegisterResource();
       
            CatLog.Ok("[ResourceManager] 已完成初始化");
        }

        /// <summary> 注: 注册网络对象管理器并处理资源列表 </summary>
        private void RegisterResource()
        {
            foreach (var prefab in _resourceList)
            {

                if (prefab.Instantiate() is not Node node) continue;

                if (string.IsNullOrEmpty(node.Name))
                {
                    CatLog.Warn($"[ResourceManager.RegisterResource]：执行发现未有预制名的资源，文件地址: {prefab.ResourcePath}，已跳过");
                    continue;
                }

                prefab.ResourceName = node.Name;
                int prefabHash = CatUtils.GetStableHashCode(node.Name);

                if (node is Control)
                {
                    m_UIAssetList.Add(prefab);
                    node.QueueFree();
                    continue;
                }

                if (node is SceneBase && !WorldManager.Instance.SceneDict.ContainsKey(prefabHash))
                {
                    WorldManager.Instance.SceneDict[prefabHash] = prefab;
                    node.QueueFree();
                    continue;
                }

                if (node is ItemComp item)
                {
                    ItemManager.Instance.RegisterItem(prefabHash,prefab, item.m_ItemData);
                }

                if (!NetObjectManager.Instance.m_PrefabDict.ContainsKey(prefabHash) && CatUtils.FindChildNode<NetSyncBase>(node) != null)
                {
                    NetObjectManager.Instance.m_PrefabDict.Add(prefabHash, prefab);
                }

                node.QueueFree();
            }

        }















        /// <summary> 辅助: 从指定路径加载资源 </summary>
        private void LoadAsset(string res)
        {
            var ps = ResourceLoader.Load<PackedScene>(res);

            if (ps != null)
            {
                _resourceList.Add(ps);
            }
            else
            {
                CatLog.Err($"[ResourceManager.LoadAsset]：资源加载失败资源检查路径：{res}");
            }
        }








    }
}
