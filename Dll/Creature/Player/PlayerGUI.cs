using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.UI;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Creature
{
	[GlobalClass]
	public partial class PlayerGUI : CanvasLayer
	{
		private const int layerValue = 2000;


		// 拥有者
		private Player player;

		// 便捷属性
		private StateMachine StateMachine => player.m_StateMachine;
		private InventoryView InventoryView => player.InventoryView;
		private ConsoleView ConsoleView => player.ConsoleView;
		private EscView EscView => player.EscView;
		private HudView HudView => player.HudView;
		private DialogView DialogView => player.DialogView;

		private	 EquipmentView EquipmentView => player.m_Equipment.EquipmentView;


		// 这里后续处理，需要内嵌化
		public SlotUI CurrentDragSource { get; set; }
		public TextureRect CurrentDragIcon { get; set; }


		public override void _Ready()
		{

			if (GetParent() is not Player pl)
			{
				CatLog.Err($"[PlayerGUI._Ready]：挂载对象非 Player 类，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}
			Layer = layerValue;

			player = pl;

			if (pl.IsOwner == false)
			{
				CatUtils.StopAndExit(this);
				CatLog.Net($"[PlayerGUI._Ready]：当前player 属于镜像，已销毁");
				return;
			}

			if (InventoryView != null) AddChild(InventoryView);
			if (ConsoleView != null) AddChild(ConsoleView);
			if (EscView != null) AddChild(EscView);
			if (HudView != null) AddChild(HudView);
			if (DialogView != null) AddChild(DialogView);
			if (EquipmentView != null) AddChild(EquipmentView);
		}

		public override void _Process(double delta)
		{
			ProcessUIInputs();
			UpdateMouseMode();
		}


		/// <summary>注：处理与 UI 相关的按键输入。</summary>
		private void ProcessUIInputs()
		{
			if (Input.IsActionJustPressed("cat_Console")) ConsoleView.ToggleUI();
			if (Input.IsActionJustPressed("cat_Tab"))
			{
				InventoryView.ToggleUI();
				EquipmentView.ToggleUI();

			}
			if (Input.IsActionJustPressed("cat_Esc")) EscView.ToggleUI();
		}

		/// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
		private void UpdateMouseMode()
		{
			// 用视图的 Visible 替代原来的 Ui_Visible 委托
			if (ConsoleView.Visible || EscView.Visible || InventoryView.Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				StateMachine.SwitchPlayerState(StateMachine.PlayerState.Menu);
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				StateMachine.SwitchPlayerState(StateMachine.PlayerState.Idle);
			}
		}

		#region 接口实现

		public Vector3 DropPos => DropPos;

		public Equipment Equipment { get => player.m_Equipment; set => player.m_Equipment = value; }

		#endregion
	}
}
