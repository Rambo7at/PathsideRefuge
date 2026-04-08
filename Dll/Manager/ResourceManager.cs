using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Manager
{
    /// <summary>
    ///  注：资源管理器
    /// </summary>
    public class ResourceManager
    {

        public List<PackedScene> ResourceList = new List<PackedScene>();

        public ResourceManager()
        {
            LoadAsset("res://Prefab/Player/player.tscn");

            LoadAsset("res://Prefab/Item/et_牛奶罐.tscn");

            LoadAsset("res://Prefab/UI/ConsoleUI.tscn");
            LoadAsset("res://Prefab/UI/背包/InventoryUI.tscn");
            GD.Print("[ResourceManager] 初始化完成");
        }


        private void LoadAsset(string res)
        {
            var scene = ResourceLoader.Load<PackedScene>(res);

            if (scene != null) ResourceList.Add(scene);
            else GD.PrintErr($"[ResourceManager.LoadAsset]：资源加载失败资源检查路径：{res}");
        }

    }
}
