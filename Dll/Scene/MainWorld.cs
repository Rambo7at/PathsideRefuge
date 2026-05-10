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
            // 主机生成逻辑保持不变
            PlayerManager.Instance.SpawnLocalPlayer(SpawnPian.GlobalPosition);
            foreach (int peerId in NetCore.Instance.Multiplayer.GetPeers())
                if (peerId != NetCore.Instance.LocalPeerID)
                    PlayerManager.Instance.SpawnPlayerForClient(peerId, SpawnPian.GlobalPosition);
        }
        else
        {
            // 客户端：场景就绪，请求主机发来所有网络对象
            NetCore.Instance.SendRpcToPeer(1, "RequestFullSync", new byte[0]);
        }
    }
}