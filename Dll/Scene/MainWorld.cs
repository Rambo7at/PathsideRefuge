using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using static 途畔归所.Dll.Core.GameCore;

public partial class MainWorld : Node3D
{
    [Export] public Node3D SpawnPian;

    private readonly GameCore.SceneType _sceneType = SceneType.MainWorld;
    public override void _Ready()
    {
        GameCore.Instance.SetCurrentScene(_sceneType);

        if (NetCore.Instance.IsHost)
        {
            PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition);
        }
        else
        {

            // 这里写一个请求，我们做一个简单的设计，客户端 在控制台中打一段GD日志，我向服务器发送了一个生成请求
            // 服务器如果接收到 这个RPC 包裹，那么控制台输出一个 GD 日志，我收到了 客户端的请求


        }
    }


}