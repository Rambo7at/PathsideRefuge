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

            LoadAsset("res://Prefab/Piece/et_板条箱.tscn");



            LoadAsset("res://Prefab/UI/HUD/hud.tscn");
            LoadAsset("res://Prefab/UI/ESC/esc_ui.tscn");
            LoadAsset("res://Prefab/UI/ConsoleUI.tscn");
            LoadAsset("res://Prefab/UI/背包/InventoryUI.tscn");
            LoadAsset("res://Prefab/UI/格子/slot_ui.tscn");
            LoadAsset("res://Prefab/UI/主菜单/存档界面/存档信息.tscn");
            LoadAsset("res://Prefab/UI/容器/ContainerUI.tscn");
            RegisterNetObjectManager();

            CatLog.Ok("[ResourceManager] 已完成初始化");
        }

        /// <summary> 注: 从指定路径加载资源 </summary>
        private void LoadAsset(string res)
        {
            var scene = ResourceLoader.Load<PackedScene>(res);

            if (scene != null)
            {
                _resourceList.Add(scene);
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

                var info = asset.GetState();

                if (info == null) continue;

                var nodeName = info.GetNodeName(0);
                var nodeType = info.GetNodeType(0);

                if (nodeType == "Control")
                {
                    m_UIAssetList.Add(asset);
                    continue;
                }

                if (nodeType == "RigidBody3D")
                {
                    m_ItemAssetList.Add(asset);
                    continue;
                }

                if (string.IsNullOrEmpty(nodeName))
                {
                    CatLog.Warn($"[ResourceManager.RegisterNetObjManager]：执行发现未有预制名的资源，文件地址: {asset.ResourcePath}，已跳过");
                    continue;
                }

                asset.ResourceName = nodeName;
                int hash = CatUtils.GetStableHashCode(nodeName);

                if (NetObjectManager.Instance.m_PrefabDict.ContainsKey(hash))
                {
                    CatLog.Warn($"预制体名 '{nodeName}' 哈希冲突，请检查是否有重名根节点在: {asset.ResourcePath}，已跳过");
                    continue;
                }

                NetObjectManager.Instance.m_PrefabDict.Add(hash, asset);
            }
        }
    }
}
