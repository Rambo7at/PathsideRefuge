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

        public enum SceneType
        {
            Unknown,
            MainMenu,
            PlayerCreator,
            MainWorld,
        }

        public SceneType m_CurrentSceneType { get; private set; } = SceneType.Unknown;

        private Node3D _currentScene;

        // 相机引用
        private Camera3D m_GameCamera;

        public override void _Ready()
        {
            Instance = this;
            InitManagers();

            m_GameCamera = new Camera3D
            {
                Name = "GameCamera",
                Current = false
            };

            CatLog.Ok("[GameCore]：初始化完成");
        }

        public override void _Process(double delta)
        {

        }


        /// <summary>注：场景根节点在 _Ready 时调用，汇报当前场景</summary>
        public void SetCurrentSceneType(SceneType sceneType, Node3D node3D)
        {
            if (m_CurrentSceneType == sceneType) return;

            CatLog.Info($"[GameCore] 场景切换： {m_CurrentSceneType} -> {sceneType}");


            if (m_CurrentSceneType == SceneType.MainWorld)
            {
                Node parent = m_GameCamera.GetParent();
                if (parent != null)
                {
                    parent.RemoveChild(m_GameCamera);
                }
                m_GameCamera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            m_CurrentSceneType = sceneType;
            _currentScene = node3D;
            if (sceneType == SceneType.MainWorld)
            {
                if (!m_GameCamera.IsInsideTree())
                {
                    AddChild(m_GameCamera);
                }
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }

        }

        /// <summary>注：获取相机</summary>
        public Camera3D GetCamera() => m_GameCamera;

        /// <summary>注：初始化全部管理器 </summary>
        private void InitManagers()
        {

            AddChild(NetCore.Instance);
            AddChild(NetObjectRegistry.Instance);

            ResourceManager.Instance.Init();
            AddChild(NetObjectManager.Instance);


            SaveManager.Instance.Init();
            ItemManager.Instance.Init();
            UIManager.Instance.Init();


            TimeManager timeMgr = new();
            TimeManager.Instance = timeMgr;
            AddChild(timeMgr);

            ConsoleManager consoleMgr = new();
            ConsoleManager.Instance = consoleMgr;
            AddChild(consoleMgr);
        }

        public void SetCurrentScene(Node3D node3D) => _currentScene = node3D;

        public Node3D GetCurrentScene() => _currentScene;
    }
}