using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll;
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

        public override void _Ready()
		{
			Instance = this;

			InitManagers();
			ResourceLoad();

			GD.Print("[GameCore]：初始化完成");
		}
        

        /// <summary>
        ///  注：初始化管理器
        /// </summary>
        public void InitManagers()
		{

			if (m_ResourceManager == null)
			{
				m_ResourceManager = new ResourceManager();
			}

			if (m_ItemManager == null)
			{
				m_ItemManager = new ItemManager();
			}

			if (m_ConsoleManager == null)
			{
				m_UIManager = new();
            }


            if (m_TimeManager == null)
            {
                m_TimeManager = new ();
                AddChild(m_TimeManager);
            }
			if (m_ConsoleManager == null)
			{
				m_ConsoleManager = new();
				AddChild(m_ConsoleManager);

            }

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
			}


		}



	}
}
