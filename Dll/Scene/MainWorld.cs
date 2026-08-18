using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Scene;

public partial class MainWorld : SceneBase
{
    [Export] public Node3D SpawnPian;

    public override void _Ready()
    {


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
    }
}
