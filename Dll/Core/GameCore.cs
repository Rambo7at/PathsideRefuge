using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;

namespace 途畔归所.Dll.Core
{
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

        // 相机引用
        private Camera3D m_GameCamera;

        public override void _Ready()
        {
            Instance = this;
            InitManagers();

            m_GameCamera = new Camera3D();
            m_GameCamera.Name = "GameCamera";
            m_GameCamera.Current = false;

            GD.Print("[GameCore]：初始化完成");
        }

        public override void _Process(double delta)
        {

        }


        /// <summary>注：由场景根节点（如主菜单、游戏世界）在 _Ready 时调用，汇报当前场景类型。</summary>
        public void SetCurrentScene(SceneType sceneType)
        {
            if (m_CurrentSceneType == sceneType) return;
            GD.Print($"[GameCore] 场景切换： {m_CurrentSceneType} -> {sceneType}");

            // 离开 MainWorld 时的清理
            if (m_CurrentSceneType == SceneType.MainWorld)
            {
                // 如果相机正被 PlayerCamera 持有，先摘下来
                Node parent = m_GameCamera.GetParent();
                if (parent != null)
                {
                    parent.RemoveChild(m_GameCamera);
                }
                m_GameCamera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            m_CurrentSceneType = sceneType;

            // 进入 MainWorld 时的准备
            if (sceneType == SceneType.MainWorld)
            {
                // 相机加入场景树（暂时作为 GameCore 子节点），稍后 PlayerCamera 会取走
                if (!m_GameCamera.IsInsideTree())
                {
                    AddChild(m_GameCamera);
                }
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

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

    }
}
