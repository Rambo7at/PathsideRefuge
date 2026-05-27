using Godot;
using Godot.Collections;
using 途畔归所.Dll.Base;
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

        public Array<PackedScene> m_ItemAssetList { get; private set; } = [];
        public Array<PackedScene> m_UIAssetList { get; private set; } = [];

        private ResourceManager() { }

        public void Init()
        {
            LoadAsset("res://Prefab/Player/player.tscn");
            LoadAsset("res://Prefab/Item/et_牛奶罐.tscn");
            LoadAsset("res://Prefab/Item/7at_匕首.tscn");
            LoadAsset("res://Prefab/Item/et_木材.tscn");
            LoadAsset("res://Prefab/Npc/Npc.tscn");
            LoadAsset("res://Prefab/Piece/et_板条箱.tscn");


            LoadAsset("res://Prefab/View/HUD/hud.tscn");
            LoadAsset("res://Prefab/View/ESC/esc_ui.tscn");
            LoadAsset("res://Prefab/View/ConsoleUI.tscn");
            LoadAsset("res://Prefab/View/储物/InventoryUI.tscn");
            LoadAsset("res://Prefab/View/格子/slot_ui.tscn");
            LoadAsset("res://Prefab/View/Button/Button_A1.tscn");
            LoadAsset("res://Prefab/View/容器/ContainerUI.tscn");


            LoadAsset("res://Scenes/主菜单.tscn");
            LoadAsset("res://Scenes/测试场景.tscn");
            LoadAsset("res://Scenes/角色创建.tscn");


            RegisterNetObjectManager();
       
            CatLog.Ok("[ResourceManager] 已完成初始化");
        }

        /// <summary> 注: 从指定路径加载资源 </summary>
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

        /// <summary> 注: 注册网络对象管理器并处理资源列表 </summary>
        private void RegisterNetObjectManager()
        {
            NetObjectManager netObj = new();
            NetObjectManager.Instance = netObj;

            foreach (var asset in _resourceList)
            {
                if (asset == null) continue;

                Node node = asset.Instantiate();

                if (node is Control)
                {
                    m_UIAssetList.Add(asset);
                    continue;
                }

                if (node is RigidBody3D)
                {
                    m_ItemAssetList.Add(asset);
                }



                
                if (string.IsNullOrEmpty(node.Name))
                {
                    if (!string.IsNullOrEmpty(node.Name)) 
                    CatLog.Warn($"[ResourceManager.RegisterNetObjManager]：执行发现未有预制名的资源，文件地址: {asset.ResourcePath}，已跳过");
                    continue;
                }

                asset.ResourceName = node.Name;
                int hash = CatUtils.GetStableHashCode(node.Name);

                if (node is SceneBase)
                {
                    WorldManager.Instance.SceneDict[hash] = asset;
                }

                if (NetObjectManager.Instance.m_PrefabDict.ContainsKey(hash))
                {
                    CatLog.Warn($"预制体名 '{node.Name}' 哈希冲突，请检查是否有重名根节点在: {asset.ResourcePath}，已跳过");
                    continue;
                }

                NetObjectManager.Instance.m_PrefabDict.Add(hash, asset);
            }

        }


    }
}
