using Godot;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Core;

/// <summary>注：游戏核心类，管理游戏场景、相机及初始化各类管理器。</summary>
public partial class GameCore : Node
{
    private static GameCore _instance;
    public static GameCore Instance { get => _instance; private set => _instance ??= value; }

    public override void _Ready()
    {
        Instance = this;
        InitManagers();

        CatLog.Ok("[GameCore._Ready]：初始化完成");
    }

    public override void _Process(double delta)
    {

        if (Input.IsActionJustPressed("cat_F6"))
        {
            NetObjectRegistry.Instance.Debug_GetAllNetObjects();

        }
           
    }

    /// <summary>注：从主菜单进入游戏，加载玩家最后所在场景或默认场景</summary>
    public void EnterGame()
    {
        if (PlayerManager.Instance.LocalPlayerData == null) return;

        if (!WorldManager.Instance.ChangeScene(PlayerManager.Instance.LocalPlayerData.LastSceneHash))
        {
            WorldManager.Instance.LoadDefaultScene();
        }
    }






    /// <summary>注：初始化全部管理器 </summary>
    private void InitManagers()
    {
        SaveManager.Instance.Init();

        AddChild(NetCore.Instance);
        AddChild(SceneOwnerManager.Instance);
        AddChild(RpcGateway.Instance);
        AddChild(NetObjectRegistry.Instance);
        ResourceManager.Instance.Init();
        AddChild(NetObjectManager.Instance);



        TimeManager timeMgr = new();
        TimeManager.Instance = timeMgr;
        AddChild(timeMgr);

        ConsoleManager consoleMgr = new();
        ConsoleManager.Instance = consoleMgr;
        AddChild(consoleMgr);
    }

}
