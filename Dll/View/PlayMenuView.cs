using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.View
{
	public partial class PlayMenuView : Control
	{
		[Export] private Control m_MainMenuView;
		[Export] private Label m_RoomInfo;
		[Export] public Control 输入框;
		private string m_ip { get => NetCore.Instance.m_RoomIP; }

		public override void _Ready()
		{
			this.Visible = false;
			输入框.Visible = false;
		}

		/// <summary>回调函数：返回开始主界面</summary>
		private void ReturnToStart()
		{
			m_MainMenuView.Visible = true;
			this.Visible = false;
		}


		/// <summary>回调函数：本地游戏 </summary>
		public void LocalGame()
		{
			if (CheckSaveDataBeforeAction() == false) return;

			WorldManager.Instance.ChangeScene(this, "测试场景");
		}

		/// <summary>回调函数：在线游戏 </summary>
		public void MultiplayerGame()
		{
            if (CheckSaveDataBeforeAction() == false) return;

            NetCore.Instance.StartLANHost();
            WorldManager.Instance.ChangeScene(this, "测试场景");
        }


		/// <summary>回调函数：搜索大厅 </summary>
		public void FindLobby()
		{
            if (CheckSaveDataBeforeAction() == false) return;


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


		private bool CheckSaveDataBeforeAction()
		{
            if (SaveManager.Instance.HasValidPlayerSaveData() == false)
            {
                WorldManager.Instance.ChangeScene(this, "角色创建");
				return false;
            }

			if (SaveManager.Instance.HasValidWorldSaveData() == false)
			{
				输入框.Visible = true;
                return false;
            }

            return true;
        }





	}
}
