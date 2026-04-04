using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll;
using 维修公司.Dll.Manager;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Core
{
	public partial class GameCore : Node
	{

		public static GameCore Instance;

		/// <summary> 注：时间管理器 </summary>
		public TimeManager m_TimeManager;

		/// <summary> 注：物品资源管理器 </summary>
		public ItemManager m_ItemManager;

		/// <summary> 注：资源管理器 </summary>
		public ResourceManager m_ResourceManager;

		/// <summary> 注：UI资源管理器 </summary>
		public UIManager m_UIManager;

		/// <summary> 注：控制台管理器 </summary>
		public ConsoleManager m_ConsoleManager;

		/// <summary> 注：玩家管理器 </summary>
		public PlayerManager m_PlayerManager;

		public NetworkCore m_NetworkCore;

		public override void _Ready()
		{
			Instance = this;

			InitManagers();
			ResourceLoad();

			GD.Print("[GameCore]：初始化完成");
		}


		/// <summary>注：初始化全部管理器 </summary>
		private void InitManagers()
		{
			RegisterManager(ref m_ResourceManager);
			RegisterManager(ref m_ItemManager);
			RegisterManager(ref m_ConsoleManager);
			RegisterManager(ref m_TimeManager);
			RegisterManager(ref m_UIManager);
			RegisterManager(ref m_PlayerManager);
			RegisterManager(ref m_NetworkCore);


			AddChild(m_TimeManager);
			AddChild(m_NetworkCore);
			AddChild(m_ConsoleManager);
		}






		/// <summary>
		///  注：资源加载
		/// </summary>
		public void ResourceLoad()
		{
			if (m_ItemManager == null){ GD.PrintErr("[GameCore]：加载资源时[m_ItemManager]是空的"); return;}
			if (m_ResourceManager.ResourceList.Count == 0) { GD.PrintErr("[GameCore]：加载资源时[m_ResourceManager.ResourceList]是空的"); return; }

			foreach (var item in m_ResourceManager.ResourceList)
			{
				m_ItemManager.Init(item);
				m_UIManager.Init(item);
				m_PlayerManager.Init(item);
			}


		}

		#region 辅助方法

		/// <summary> 泛型管理器注册，约束：必须是类 + 有无参构造 </summary>
		/// <typeparam name="T">管理器类型</typeparam>
		/// <param name="manager">管理器实例</param>
		public void RegisterManager<T>(ref T manager) where T : class, new()
		{
			if (manager == null) manager = new();
		}






		#endregion

	}
}
