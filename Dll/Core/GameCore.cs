using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Core
{
    /// <summary>注：游戏核心类，管理游戏场景、相机及初始化各类管理器。</summary>
    public partial class GameCore : Node
    {
        private static GameCore _instance;
        public static GameCore Instance { get => _instance; private set => _instance ??= value; }

        public override void _Ready()
        {
            Instance = this;
            InitManagers();

            CatLog.Ok("[GameCore]：初始化完成");
        }


        /// <summary>注：初始化全部管理器 </summary>
        private void InitManagers()
        {
            SaveManager.Instance.Init();

            AddChild(NetCore.Instance);
            AddChild(NetObjectRegistry.Instance);

            ResourceManager.Instance.Init();
            AddChild(NetObjectManager.Instance);


            
            ItemManager.Instance.Init();
            UIManager.Instance.Init();


            TimeManager timeMgr = new();
            TimeManager.Instance = timeMgr;
            AddChild(timeMgr);

            ConsoleManager consoleMgr = new();
            ConsoleManager.Instance = consoleMgr;
            AddChild(consoleMgr);
        }

    }
}