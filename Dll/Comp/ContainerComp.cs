using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Comp;

/// <summary>注：容器交互组件，挂载于箱子/容器类放置物上，处理打开/关闭、库存同步与网络通信</summary>
public partial class ContainerComp : PlacedBase, IInteractable, IInventoryHolder
{
	[Export] private InventoryData m_inventoryData;
	[Export] private Node3D m_dropPos;
	private InventoryView _inventoryView;
	private NetSyncBase _netSyncBase;

	public const long NoInteractPeer = -1;

	/// <summary>注：容器是否处于打开状态</summary>
	public bool IsOpen { get => m_inventoryData.IsOpen; set => m_inventoryData.IsOpen = value; }

	/// <summary>注：当前占用容器的玩家 PeerID</summary>
	public long InteractPeer { get => m_inventoryData.InteractPeer; set => m_inventoryData.InteractPeer = value; }

	InventoryData IInventoryHolder.InventoryData { get => m_inventoryData; set => m_inventoryData = value; }
	Vector3 IInventoryHolder.DropPos => m_dropPos.GlobalPosition;

	/// <summary>注：容器名称</summary>
	public string ObjectName => m_placedData.m_Name;

	private uint _dataRevision;

	public override void _Ready()
	{
		if (ValidateDeps())
		{
			CatUtils.StopAndExit(this);
			return;
		}

		if (_netSyncBase.IsOwner)
		{
			LoadFromNetObject();
		}




		_netSyncBase.RegisterRpc(nameof(Rpc_ToggleContainer), Rpc_ToggleContainer);
		_netSyncBase.RegisterRpc<bool, long>(nameof(Rpc_ContainerInteract), Rpc_ContainerInteract);
	}

	/// <summary>注：校验运行必需依赖，依赖缺失返回 true，校验成功返回 false</summary>
	private bool ValidateDeps()
	{
		if (CatUtils.FindChildNode<NetSyncBase>(this) is not NetSyncBase sync)
		{
			CatLog.Err("[ContainerComp.ValidateDeps]：缺少 NetSyncBase 组件");
			return true;
		}
		if (m_inventoryData == null)
		{
			CatLog.Err("[ContainerComp.ValidateDeps]：缺少 InventoryData 数据");
			return true;
		}
		if (GUIManager.Instance.GetView(m_inventoryData.m_UIname) is not InventoryView view)
		{
			CatLog.Err("[ContainerComp.ValidateDeps]：InventoryView 加载失败");
			return true;
		}
		_inventoryView = view;
		_inventoryView.m_holder = this;
		_inventoryView.Visible = false;
		_netSyncBase = sync;
		return false;
	}

	/// <summary>注：玩家交互入口，按 E 触发容器开关操作</summary>
	public void PlayerInteract(bool InputE, bool InputF, CreatureBase creature)
	{
		if (creature is not Player) return;
		if (InputE)
		{
			_netSyncBase.SendRpcToPeer(nameof(Rpc_ToggleContainer), _netSyncBase.OwnedPeer);
		}
	}

	/// <summary>注：RPC 入口，接收容器开关请求，主机直接执行，客户端先请求数据再执行</summary>
	private void Rpc_ToggleContainer(long sendPeer)
	{
		if (NetCore.Instance.IsHost)
		{
			ResolveContainerToggle(sendPeer);
			return;
		}
		CatLog.Err($"非服务器已发送数据请求");
		_netSyncBase.RequestCustomData(() => ResolveContainerToggle(sendPeer));
	}

	/// <summary>注：核心决策方法，根据当前容器状态决定执行打开或关闭操作</summary>
	private void ResolveContainerToggle(long sendPeer)
	{
		LoadFromNetObject();

		CatLog.Ok($"拥有者：读取目前箱子状态：开关{IsOpen}/操作人{InteractPeer}");

		if (IsOpen)
		{
			if (InteractPeer != sendPeer)
			{
				CatLog.Warn($"拥有者：拒绝{sendPeer}请求目前箱子非申请人操作-状态：开关{IsOpen}/操作人{InteractPeer}");
				return;
			}

			_netSyncBase.SendRpcToPeer(nameof(Rpc_ContainerInteract), sendPeer, true, InteractPeer);
			CatLog.Ok($"拥有者：回复用户关闭操作-状态：操作方式{true}/操作人{InteractPeer}");
			return;
		}


		IsOpen = true;
		InteractPeer = sendPeer;
		SaveToNetObject();
		CatLog.Ok($"拥有者：回复用户开箱操作-状态：操作方式{false}/操作人{InteractPeer}");

 
		if (NetCore.Instance.IsHost)
		{
			
			_netSyncBase.SendRpcToPeer(nameof(Rpc_ContainerInteract), sendPeer, false, InteractPeer);
			return;
		}
		else
		{
			_netSyncBase.SubmitCustomData(() => _netSyncBase.SendRpcToPeer(nameof(Rpc_ContainerInteract), sendPeer, false, InteractPeer) );
		}
	   
	}

	/// <summary>注：RPC 回传入口，接收权威端执行结果，打开或关闭容器 UI</summary>
	private void Rpc_ContainerInteract(long sendPeer, bool open, long interactPeer)
	{
		CatLog.Debug($"用户：收到服务端操作指令-状态：操作方式{open}/操作人{interactPeer}");

		if (open)
		{
			if (interactPeer != _netSyncBase.LocalPeer)
			{
				CatLog.Debug($"用户：操作指令送达的，操作人不符，停止操作");
				return;
			}

			if (NetCore.Instance.IsHost)
			{
				CloseContainerView();
				CatLog.Debug($"操作指令：操作身份是服务器，已直接存入数据");
				return;
			}

			CloseContainerView();
			_netSyncBase.SubmitCustomData();
			CatLog.Ok($"用户：提交修改数据信息-状态：开关{IsOpen}/操作人{InteractPeer}");
			return;
		}

		if (interactPeer != _netSyncBase.LocalPeer && interactPeer != NoInteractPeer)
		{
			CatLog.Debug($"用户：操作指令送达的，操作人不符，停止操作");
			return;
		}

		if (NetCore.Instance.IsHost)
		{
			OpenContainerView();
			CatLog.Debug($"操作指令：操作身份是服务器，直接打开箱子");
			return;
		}

		_netSyncBase.RequestCustomData(() =>
		{
			OpenContainerView();
			CatLog.Debug($"用户：成功打开容器");
		});
	}


	/// <summary>注：打开容器 UI，加载数据并刷新显示</summary>
	private void OpenContainerView()
	{
		LoadFromNetObject();
		PlayerManager.Instance.LocalPlayer.GUI.AddChild(_inventoryView);
		_inventoryView.Visible = true;
		_inventoryView.RefreshAllSlots();
	}

	/// <summary>注：关闭容器 UI，重置状态并保存数据</summary>
	private void CloseContainerView()
	{
		_inventoryView.GetParent()?.RemoveChild(_inventoryView);
		IsOpen = false;
		InteractPeer = NoInteractPeer;
		SaveToNetObject();
	}

	/// <summary>注：将 m_inventoryData 序列化写入 NetObject.CustomData</summary>
	private void SaveToNetObject()
	{
		byte[] data = m_inventoryData.Serialize();
		_netSyncBase.CustomData = data;
		_dataRevision += 1;
	}

	/// <summary>注：从 NetObject.CustomData 反序列化读取数据到 m_inventoryData</summary>
	private void LoadFromNetObject()
	{
		if (_netSyncBase.CustomData == null) return;

		var data = new InventoryData();
		data.Deserialize(_netSyncBase.CustomData);

		if (data.m_itemArr.Count == 0) return;

		if (_dataRevision >= _netSyncBase.NetObj.DataRevision) return;
		_dataRevision = _netSyncBase.NetObj.DataRevision;

		m_inventoryData.IsOpen = data.IsOpen;
		m_inventoryData.InteractPeer = data.InteractPeer;
		m_inventoryData.m_itemArr = data.m_itemArr;
	}

	/// <summary>注：在指定索引设置物品数据</summary>
	public bool TrySetInventoryItem(int index, ItemData data)
	{
		if (index < 0 || index >= m_inventoryData.m_capacity)
		{
			return false;
		}
		m_inventoryData.m_itemArr[index] = data;
		return true;
	}

	private void Debug_ItemArr(Array<ItemData> items)
	{
		foreach (var item in items)
		{
			if (item == null) continue;

			CatLog.Warn($"箱子里装有{item.Name}");

		}

	}

}
