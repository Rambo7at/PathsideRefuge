using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Comp
{
	public partial class ContainerComp : PlacedComp, IInventoryHolder, IInteractable
	{

		public InventoryComp m_ContainerComp;

		public bool m_IsOpen { get=> m_ContainerComp.IsInsideTree(); }

		private CanvasLayer m_CanvasLayer;


		public override void _Ready()
		{
			InitEntityBase();


			if (SaveManager.Instance.GetPlacedRuntimeData() == null) return;
			GD.Print("不是空的");

			GD.Print($"长度是{SaveManager.Instance.GetPlacedRuntimeData().Count}");
			foreach (var item in SaveManager.Instance.GetPlacedRuntimeData())
			{
				GD.Print("找到了数据："+ item.Key);
			}



			if (SaveManager.Instance.GetPlacedRuntimeData().TryGetValue(m_EntityGUID, out var saved))
			{
				CustomData = saved.AsGodotDictionary<string, Variant>();
				GD.Print("有对应目标存档");
			}

			InitInventory();

		}

		public override void _Process(double delta)
		{
			if (m_IsOpen)
			{
				if (m_CanvasLayer.GetOwner() is not Node3D node) return;
		   

				float distance = GlobalPosition.DistanceTo(node.GlobalPosition);

				if (distance < 3) return;


				m_ContainerComp.GetParent().RemoveChild(m_ContainerComp);
				SaveManager.Instance.SavePlacedRuntimeData(m_EntityGUID, CustomData);

				GD.Print("成功写入了数据");
				if (SaveManager.Instance.GetPlacedRuntimeData() == null) return;
				GD.Print("不是空的");

				GD.Print($"长度是{SaveManager.Instance.GetPlacedRuntimeData().Count}");

			}
		
		
		}


		private void InitInventory()
		{
			if (m_ContainerComp != null) return;

			var UI = UIManager.Instance.GetUI("ContainerUI");
			if (UI == null) return;


			if (UI is not InventoryComp script) return;


			script.Holder = this;
			m_ContainerComp = script;
		}


		public void OpenContainer(Player player)
		{
			if (player == null) return;
			if (m_IsOpen)
			{

				m_ContainerComp.GetParent().RemoveChild(m_ContainerComp);
				SaveManager.Instance.SavePlacedRuntimeData(m_EntityGUID, CustomData);

				GD.Print("成功写入了数据");
				if (SaveManager.Instance.GetPlacedRuntimeData() == null) return;
				GD.Print("不是空的");

				GD.Print($"长度是{SaveManager.Instance.GetPlacedRuntimeData().Count}");

			}
			else
			{
				m_CanvasLayer = player.m_CanvasLayer;
				player.m_CanvasLayer.AddChild(m_ContainerComp);

			}
		}


		public CanvasLayer GetCanvasLayer()
		{
			if (m_CanvasLayer == null) return null;

			return m_CanvasLayer;
		}

		public Vector3 GetDropPosition() => new(GlobalPosition.X, GlobalPosition.Y + 2, GlobalPosition.Z);


		public Dictionary<int, ItemData> LoadInventory()
		{
			if (CustomData == null) return [];
			if (!CustomData.ContainsKey("container_items")) return [];
			var slotdata = (Dictionary<int, ItemData>)CustomData["container_items"];
			if (slotdata == null) return [];

			return slotdata;
		}

		public void SaveInventory(Array<SlotComp> slotComps)
		{
			Dictionary<string, Variant> itemsDict = [];
			Dictionary<int, ItemData> slotData = [];
			foreach (var slot in slotComps)
			{
				if (slot.IsSlotEmpty) continue;
				slotData[slot.m_SlotID] = slot.m_ItemData.DeepCopy();
			}

			// 存入 PlacedBase 的通用容器
			CustomData["container_items"] = slotData;



		}

		public void PlayerInteract(bool InputE, bool InputF, Player player)
		{
			if (InputE)
			{
				OpenContainer(player);

			}

			
		}
	}
}
