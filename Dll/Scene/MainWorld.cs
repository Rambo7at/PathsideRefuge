using Godot;
using TerraEnvEditor.Env;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.System;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Scene;

public partial class MainWorld : SceneBase
{
    [Export] public Node3D SpawnPian;

    [Export] public SkyEditor skyEditor { get; set; }

    public override void _Ready()
    {
        if (SpawnPian == null || skyEditor == null)
        {
            CatLog.Err("[MainWorld]：核心游戏场景组件为空，请检查编辑器");
        }

        Vector3 spawnPos;
        Vector3 spawnrot;

        if (PlayerManager.Instance.CanUseSavedPosition())
        {
            spawnPos = PlayerManager.Instance.LocalPlayerData.LastPosition;
            spawnrot = PlayerManager.Instance.LocalPlayerData.LastRotation;
        }
        else
        {
            spawnPos = SpawnPian.GlobalPosition;
            spawnrot = SpawnPian.GlobalRotation;
        }
        PlayerManager.Instance.SpawnLocalPlayer(spawnPos, spawnrot);





        // 玩家进入世界开始行动 → 启动游戏时间（幂等）
        WorldManager.Instance.StartGame();


    }

    public override void _Process(double delta)
    {

        skyEditor.SetTime(GameTime.Instance.GetTime());

    }





}
