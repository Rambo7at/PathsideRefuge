using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using static 途畔归所.Dll.Core.GameCore;

public partial class MainMenu : Node3D
{

	[Export] private Control m_Start;
	[Export] private Control m_Lobby;
	[Export] private Label m_RoomInfo;

	private readonly GameCore.SceneType _sceneType = SceneType.MainMenu;
	public override void _Ready()
	{

		GameCore.Instance.SetCurrentScene(_sceneType);
		ReturnToStart();
	}


	public override void _Process(double delta) { }

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
		NetCore.Instance.StartBroadcast("我的房间", 1, NetCore.Max_Player);
	
		GetTree().ChangeSceneToFile("res://Scenes/测试场景.tscn");
	}


	private string foundIP = "";
	private int foundPort = 0;
	/// <summary>回调函数：搜索大厅 </summary>
	public void FindLobby()
	{
		if (SaveManager.Instance.IsValidPlayerSaveData() == false)
		{
			GetTree().ChangeSceneToFile("res://Scenes/角色创建.tscn");
			return;
		}

		// 信号只连接一次
		if (!NetCore.Instance.IsConnected("RoomFound", new Callable(this, nameof(OnRoomFound))))
		{
			NetCore.Instance.Connect("RoomFound", new Callable(this, nameof(OnRoomFound)));
		}

		NetCore.Instance.StartListening();
		// m_RoomInfo 保持为空，直到找到房间
	}

	private void OnRoomFound(string roomName, string ip, int port, int playerCount, int maxPlayers)
	{
		foundIP = ip;
		foundPort = port;
		m_RoomInfo.Text = roomName;   // 只显示房间名
	}

	public void JoinRoom()
	{
		if (string.IsNullOrEmpty(foundIP))
		{
			m_RoomInfo.Text = "没有可加入的房间";
			return;
		}

		NetCore.Instance.StopListening();
		var OK = NetCore.Instance.JoinLAN(foundIP, foundPort);

		m_RoomInfo.Text = $"正在连接 {foundIP}...";

		if (OK == Error.Ok) WaitForConnectionAndRequest();


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
