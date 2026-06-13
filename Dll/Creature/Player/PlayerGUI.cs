using Godot;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Creature
{
	[GlobalClass]
	public partial class PlayerGUI : CanvasLayer, IInventoryHolder
	{
		[Export] private Node3D m_dropPos;

		private Player m_player;
		private InventoryView m_inventoryView;
		private ConsoleView m_consoleView;
		private EscView m_escView;
		private HudView m_hudView;

		public SlotView CurrentDragSource { get; set; }
		public TextureRect CurrentDragIcon { get; set; }



		public InventoryData InventoryData { get => m_player.m_data.m_inventoryData ??= new InventoryData(); set => m_player.m_data.m_inventoryData = value; }
		Vector3 IInventoryHolder.DropPos => m_dropPos.GlobalPosition;
		public override void _Ready()
		{

			if (GetParent() is not Player pl)
			{
				CatLog.Err($"[PlayerGUI._Ready]：检测挂载对象并非 player 或是空 ，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			m_player = pl;

			if (pl.m_IsOwner == false)
			{
				CatUtils.StopAndExit(this);
				CatLog.Net($"[PlayerGUI._Ready]：当前并非本地玩家，已销毁");
				return;
			}

			InitInventory();
			InitConsole();
			InitEsc();
			InitPlayerHUD();
		}


		public override void _Process(double delta)
		{
			ProcessUIInputs();
			UpdateMouseMode();
		}

		private void InitInventory()
		{
			// 直接创建 InventoryView 并通过 UIManager 获取预制体
			if (UIManager.Instance.GetUI(InventoryData.m_UIname) is not InventoryView view)
			{
				CatLog.Err("[PlayerGUI.InitInventory] 背包视图加载失败");
				return;
			}

			m_inventoryView = view;
			m_inventoryView.Visible = false;   
			AddChild(m_inventoryView);         
		}

		private void InitConsole()
		{
			if (m_consoleView != null) return;

			if (UIManager.Instance.GetUI("ConsoleUI") is not ConsoleView view) return;

			m_consoleView = view;
			m_consoleView.GetPlayer(m_player);
			view.Visible = false;
			AddChild(view);
		}

		private void InitEsc()
		{
			if (m_escView != null) return;

			if (UIManager.Instance.GetUI("esc_ui") is not EscView view) return;

			m_escView = view;
			view.Visible = false;
		   AddChild(view);
		}

		private void InitPlayerHUD()
		{
			if (m_hudView != null) return;
			if (UIManager.Instance.GetUI("hud") is not HudView hud) return;

			hud.m_maxHP = m_player.m_Health;
			hud.Visible = true;
			m_hudView = hud;
			AddChild(m_hudView);
		}



		/// <summary>注：处理与 UI 相关的按键输入。</summary>
		private void ProcessUIInputs()
		{
			if (Input.IsActionJustPressed("cat_Console")) m_consoleView.ToggleUI();
			if (Input.IsActionJustPressed("cat_Tab")) m_inventoryView.ToggleUI();
			if (Input.IsActionJustPressed("cat_Esc")) m_escView.ToggleUI();
		}


		/// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
		private void UpdateMouseMode()
		{
			// 用视图的 Visible 替代原来的 Ui_Visible 委托
			if (m_consoleView.Visible || m_escView.Visible || m_inventoryView.Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				m_player.m_OnUI = true;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				m_player.m_OnUI = false;
			}
		}


	}
}
