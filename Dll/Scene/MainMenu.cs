using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Core.GameCore;

public partial class MainMenu : Node3D
{

	[Export] private Control m_Start;
	[Export] private Control m_Lobby;
	[Export] private Label m_RoomInfo;

	private string m_ip { get => NetCore.Instance.m_RoomIP; }

	private readonly SceneType _sceneType = SceneType.MainMenu;
	public override void _Ready()
	{

		GameCore.Instance.SetCurrentSceneType(_sceneType,this);
		ReturnToStart();
	}


	public override void _Process(double delta)
	{
		if (string.IsNullOrEmpty(m_ip)) return;

        m_RoomInfo.Text = m_ip;
    }

	/// <summary>回调函数：进入大厅菜单</summary>
	private void StartGame()
	{
		m_Start.Visible = false;
		m_Lobby.Visible = true;
	}
	/// <summary>回调函数：退出游戏</summary>
	public void QuitGame() => GetTree().Quit();

	/// <summary>回调函数：返回开始主界面</summary>
	public void ReturnToStart()
	{
		m_Start.Visible = true;
		m_Lobby.Visible = false;
	}

	/// <summary>回调函数：本地游戏 </summary>
	public void LocalGame()
	{
		if (SaveManager.Instance.IsValidPlayerSaveData() == false)
		{
			GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
			return;
		}

		GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
	}

	/// <summary>回调函数：在线游戏 </summary>
	public void MultiplayerGame()
	{
		if (SaveManager.Instance.IsValidPlayerSaveData() == false)
		{
			GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
			return;
		}

 		NetCore.Instance.StartLANHost();
		GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
	}

    /// <summary>回调函数：搜索大厅 </summary>
    public void FindLobby()
	{
		if (SaveManager.Instance.IsValidPlayerSaveData() == false)
		{
			GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
			return;
		}

		NetCore.Instance.FindLANRoom();
	}


	public void JoinRoom()
	{
		//NetCore.Instance.StopListening();
		var OK = NetCore.Instance.JoinLAN(m_ip);
		NetCore.Instance.StopLANDiscovery();

        m_RoomInfo.Text = $"正在连接 {m_ip}...";

		if (OK == Error.Ok) WaitForConnectionAndRequest();
		else
		{
            CatLog.Warn($"连接 {m_ip}...失败");
            m_RoomInfo.Text = string.Empty;
        }


    }
    private async void WaitForConnectionAndRequest()
    {
        // 等待直到 peer 状态真正变为 Connected
        while (Multiplayer.MultiplayerPeer == null || Multiplayer.MultiplayerPeer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
        {
            await ToSignal(GetTree(), "process_frame");
        }
        GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
        GD.Print("[MainWorld] 连接已就绪，发送玩家请求");
    }
}
