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

        public override void _Ready()
        {
            Instance = this;
            InitManagers();
            GD.Print("[GameCore]：初始化完成");
        }

        /// <summary>注：初始化全部管理器 </summary>
        private void InitManagers()
        {
            NetCore netCore = new NetCore();
            NetCore.Instance = netCore;
            AddChild(netCore);


            ResourceManager.Instance.Init();
            SaveManager.Instance.Init();
            ItemManager.Instance.Init();
            UIManager.Instance.Init();
            PlayerManager.Instance.Init();


            TimeManager timeMgr = new TimeManager();
            TimeManager.Instance = timeMgr;   // 确保在 AddChild 前可用
            AddChild(timeMgr);

            ConsoleManager consoleMgr = new ConsoleManager();
            ConsoleManager.Instance = consoleMgr;
            AddChild(consoleMgr);
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
