using Godot;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;


namespace 途畔归所.Dll.Comp
{
	public partial class ContainerComp : PlacedComp, IInteractable, IInventoryHolder
	{
		private InventoryComp m_InventoryComp;
		private InventoryData m_inventoryData;
		private InventoryView m_inventoryView;

		private CanvasLayer m_CanvasLayer;
		private NetSyncBase m_netSyncBase;

		public bool m_IsOpen { get; private set; }
		InventoryData IInventoryHolder.m_HolderInventoryData { get => m_inventoryData ??= new(); set => m_inventoryData = value; }

		public override void _Ready()
		{
			foreach (var node in GetChildren())
			{
				if (node is NetSyncBase comp) m_netSyncBase = comp;

				if (node is InventoryComp inventoryComp) m_InventoryComp = inventoryComp;
			}

			if (m_InventoryComp == null)
			{
				CatUtils.StopAndExit(this);
				CatLog.Err("[ContainerComp._Ready]：ContainerComp 未挂载 InventoryComp 组件，已销毁");
				return;
			}

			if (InitContainerNetSync(m_netSyncBase) == false)
			{
				CatUtils.StopAndExit(this);
				return;
			}


			m_netSyncBase.RegisterRpc("RequestOpenContainer", RPC_RequestOpenContainer);
			m_netSyncBase.RegisterRpc<byte[]>("ReceiveContainerInventory", RPC_ReceiveContainerInventory);
			m_netSyncBase.RegisterRpc<bool>("SyncContainerOpenState", RPC_SyncContainerOpenState);
			m_netSyncBase.RegisterRpc("RequestCloseContainer", RPC_RequestCloseContainer);
			m_netSyncBase.RegisterRpc("ReceiveCloseContainer", RPC_ReceiveCloseContainer);
			m_netSyncBase.RegisterRpc<byte[]>("SubmitFinalInventory", RPC_SubmitFinalInventory);
		}

		public override void _Process(double delta)
		{
			if (m_IsOpen && m_inventoryView.GetParent() == PlayerManager.Instance.m_CanvasLayer)
			{
				float distance = GlobalPosition.DistanceTo(PlayerManager.Instance.m_LocalPlayer.GlobalPosition);
				// 超3米关闭
				if (distance >= 3f)
				{
					CloseContainer();
				}
			}
		}

		public void OpenContainer(Player player)
		{
			if (player == null || m_inventoryView == null) return;

			if (m_IsOpen) return;

			if (NetCore.Instance.IsClient)
			{
				m_netSyncBase.CallRpc("RequestOpenContainer");
				return;
			}

			PlayerManager.Instance.m_CanvasLayer.AddChild(m_inventoryView);
			m_inventoryView.Visible = true;
			m_IsOpen = true;
			m_netSyncBase.CallAllRpc("SyncContainerOpenState", m_IsOpen);
		}

		public void CloseContainer()
		{
			if (m_IsOpen == false) return;

			bool istUser = m_inventoryView.GetParent() == PlayerManager.Instance.m_CanvasLayer;

			if (istUser == false) return;

			if (NetCore.Instance.IsClient)
			{
				m_netSyncBase.CallRpc("RequestCloseContainer");
				return;
			}

			m_inventoryView.GetParent()?.RemoveChild(m_inventoryView);
			m_IsOpen = false;
			m_netSyncBase.CallAllRpc("SyncContainerOpenState", m_IsOpen);
		}

		private void RPC_RequestOpenContainer(long requesterId)
		{
			if (NetCore.Instance.IsClient) return;

			if (m_IsOpen) return;

			byte[] bytes = m_inventoryData.Serialize();
			m_IsOpen = true;
			m_netSyncBase.CallAllRpc("SyncContainerOpenState", m_IsOpen);
			m_netSyncBase.CallRpc("ReceiveContainerInventory", bytes, requesterId);
		}

		private void RPC_RequestCloseContainer(long requesterId)
		{
			if (NetCore.Instance.IsClient) return;
			if (m_IsOpen == false) return;

			m_IsOpen = false;
			m_netSyncBase.CallAllRpc("SyncContainerOpenState", m_IsOpen);
			m_netSyncBase.CallRpc("ReceiveCloseContainer", requesterId);
		}

		private void RPC_ReceiveCloseContainer(long senderId)
		{
			if (senderId != 1 || NetCore.Instance.IsHost) return;

			m_inventoryView.GetParent()?.RemoveChild(m_inventoryView);
			m_IsOpen = false;

			byte[] finalInventoryData = m_inventoryData.Serialize();
			m_netSyncBase.CallRpc("SubmitFinalInventory", finalInventoryData);

			CatLog.Net("容器已关闭，已提交最终库存数据");
		}

		private void RPC_SubmitFinalInventory(long requesterId, byte[] data)
		{
			if (NetCore.Instance.IsClient) return;

			if (m_IsOpen)
			{
				CatLog.Warn($"[RPC_SubmitFinalInventory] 客户端{requesterId}在容器未关闭时提交数据，已拒绝");
				return;
			}

			if (data == null || data.Length == 0)
			{
				CatLog.Warn($"[RPC_SubmitFinalInventory] 客户端{requesterId}提交了空的库存数据");
				return;
			}


			InventoryData finalData = new InventoryData();
			finalData.Deserialize(data);
			m_inventoryData = finalData.DeepCopy();
			m_InventoryComp.OnChanged?.Invoke();


			CatLog.Net($"[RPC_SubmitFinalInventory] 客户端{requesterId}提交的最终库存数据已保存");
		}


		private void RPC_ReceiveContainerInventory(long senderId, byte[] data)
		{
			if (senderId != 1 || NetCore.Instance.IsHost) return;

			if (data == null)
			{
				CatLog.Warn("[RPC_ReceiveInventoryData] 数据包为空");
				return;
			}

			InventoryData inventoryData = new InventoryData();
			inventoryData.Deserialize(data);
			m_inventoryData = inventoryData.DeepCopy();


			PlayerManager.Instance.m_CanvasLayer.AddChild(m_inventoryView);
			m_inventoryView.Visible = true;

			m_InventoryComp.OnChanged?.Invoke();
			CatLog.Net("库存数据同步成功");
		}

		private void RPC_SyncContainerOpenState(bool b)
		{
			if (NetCore.Instance.IsHost) return;
			m_IsOpen = b;
			if (!b) m_inventoryView.GetParent()?.RemoveChild(m_inventoryView);
		}


		public void PlayerInteract(bool InputE, bool InputF, Player player)
		{
			if (InputE)
			{
				if (m_IsOpen) CloseContainer();
				else OpenContainer(player);
			}

		}

		private bool InitContainerNetSync(NetSyncBase netSync)
		{

			if (netSync == null || netSync.m_NetObj == null)
			{
				CatLog.Net("[ContainerComp.InitContainerNetSync] NetSyncBase 或 NetSyncBase.NetObj 为空，已销毁");
				return false;
			}
			NetObject netObject = netSync.m_NetObj;

			netSync.OnFlushNetState += () => FlushInventory(netSync.m_NetObj);

			var custdata = netObject.m_customData.As<PlacedData>();
			m_placedData = custdata != null ? custdata.DeepCopy() : m_placedData;

			var data = m_placedData.m_data.As<InventoryData>();

			if (data != null && data.m_SlotDatas != null && data.m_SlotDatas.Count > 0)
			{
				m_inventoryData = data.DeepCopy();
			}

			var UI = UIManager.Instance.GetUI("ContainerUI");
			if (UI is not InventoryView view)
			{
				CatLog.Err("[ContainerComp.InitContainerNetSync] 查找的UI 非对应UI 组件，已销毁");
				return false;
			}
			view.BindData(m_InventoryComp);
			view.Visible = false;
			m_inventoryView = view;

			return true;
		}

		private void FlushInventory(NetObject netObject)
		{
			base.m_placedData.m_data = m_inventoryData.DeepCopy();
			netObject.m_customData = base.m_placedData.DeepCopy();
		}
	}


}
