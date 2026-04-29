using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Core
{
	public partial class GameCore : Node
	{

		public static GameCore Instance;
		public TimeManager m_TimeManager;
		public ItemManager m_ItemManager;
		public ResourceManager m_ResourceManager;
		public UIManager m_UIManager;
		public ConsoleManager m_ConsoleManager;
		public PlayerManager m_PlayerManager;
		public SaveManager m_SaveManager;
		public NetworkCore m_NetworkCore;

		public SaveData m_SaveData { get => m_SaveManager.DATA; }
		public PlayerData m_LocalPlayerData { get => m_PlayerManager.m_LocalPlayerData; set=> m_PlayerManager.m_LocalPlayerData = value; }

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
            RegisterManager(ref m_SaveManager);
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

		/// <summary> 注：资源加载/ </summary>
		public void ResourceLoad()
		{
			if (m_ItemManager == null) { GD.PrintErr("[GameCore]：加载资源时[m_ItemManager]是空的"); return; }
			if (m_ResourceManager.ResourceList.Count == 0) { GD.PrintErr("[GameCore]：加载资源时[m_ResourceManager.ResourceList]是空的"); return; }

			foreach (var item in m_ResourceManager.ResourceList)
			{
				m_ItemManager.Init(item);
				m_UIManager.Init(item);
				m_PlayerManager.Init(item);
			}
		}



        public Control GetUIAsset(string assetName) => m_UIManager.GetUI(assetName);

		public void SaveGame() => m_SaveManager.SaveData();



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
