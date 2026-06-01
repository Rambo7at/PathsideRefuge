using Godot;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Creature
{
	[GlobalClass]
	public partial class PlayerUIHandler : Node, IInventoryHolder
	{
		[Export] public InventoryComp m_InventoryComp;

		private Player m_player;
		private ConsoleComp m_ConsoleComp;
		private EscComp m_EscComp;
		private CanvasLayer m_CanvasLayer;
		private bool _IsOwner = false;


		private InventoryData m_InventoryData;

		InventoryData IInventoryHolder.m_HolderInventoryData { get => m_InventoryData ??= new InventoryData(); set => m_InventoryData = value; }

		public override void _Ready()
		{
			var node = GetParent();
			if (node == null)
			{
				CatLog.Err($"[PlayerUIHandler._Ready]：检测挂载对象是空，已返回");
				CatUtils.StopAndExit(this);
				return;
			}
			if (node is not Player pl)
			{
				CatLog.Err($"[PlayerUIHandler._Ready]：检测挂载对象并非 player ，已返回");
				CatUtils.StopAndExit(this);
				return;
			}
			m_player = pl;



			foreach (var comp in m_player.GetChildren())
			{
				if (comp is NetSyncBase netSyncBase) _IsOwner = netSyncBase.IsOwner;

				if (comp is CanvasLayer canvasLayer) m_CanvasLayer = canvasLayer;
			}



			if (_IsOwner == false)
			{
				CatLog.Warn($"[PlayerUIHandler._Ready]：检测player对象并非 本地所有，已销毁");
				CatUtils.StopAndExit(this);
				return;
			}

			m_InventoryData = m_player.m_PlayerData.m_InventoryData ?? new();


			int indxe = m_InventoryComp.m_maxCol * m_InventoryComp.m_maxRow;


			if (m_InventoryData.m_SlotDatas.Count != indxe)
			{
                m_InventoryData.UpdetaSlotData(indxe);
            }




			InitInventory();
			InitConsole();
			InitEsc();

		}


		public override void _Process(double delta)
		{
			if (_IsOwner == false) return;
			ProcessUIInputs();
			UpdateMouseMode();
		}

		private void InitInventory()
		{
			if (m_InventoryComp == null)
			{
				CatLog.Warn($"[PlayerUIHandler.InitInventory]：未挂载 InventoryComp 组件 ");
				CatUtils.StopAndExit(this);
				return;
			}

			var UI = UIManager.Instance.GetUI("InventoryUI");
			if (UI is not InventoryView view) return;
			view.BindData(m_InventoryComp);
			view.Visible = false;
			m_CanvasLayer.AddChild(view);
		}

		private void InitConsole()
		{
			if (m_ConsoleComp != null) return;

			var UI = UIManager.Instance.GetUI("ConsoleUI");
			if (UI == null) return;
			if (UI is not ConsoleComp script) return;

			m_ConsoleComp = script;
			m_ConsoleComp.GetPlayer(m_player);
			UI.Visible = false;
			m_CanvasLayer.AddChild(UI);
		}

		private void InitEsc()
		{
			if (m_EscComp != null) return;

			var UI = UIManager.Instance.GetUI("esc_ui");
			if (UI == null) return;
			if (UI is not EscComp script) return;

			m_EscComp = script;
			UI.Visible = false;
			m_CanvasLayer.AddChild(UI);
		}



		/// <summary>注：处理与 UI 相关的按键输入。</summary>
		private void ProcessUIInputs()
		{
			if (Input.IsActionJustPressed("cat_Console")) m_ConsoleComp.ToggleUI();
			if (Input.IsActionJustPressed("cat_Tab"))
			{
				m_InventoryComp.OnChanged.Invoke();
				m_InventoryComp.OnToggle.Invoke();
			}
			if (Input.IsActionJustPressed("cat_Esc")) m_EscComp.ToggleUI();
		}


		/// <summary>注：根据当前打开的 UI 面板自动切换鼠标模式与 UI 状态标志。</summary>
		private void UpdateMouseMode()
		{
			if (m_ConsoleComp.Visible || m_EscComp.Visible || m_InventoryComp.Ui_Visible.Invoke())
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
