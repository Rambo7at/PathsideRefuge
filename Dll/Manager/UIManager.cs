using Godot;
using Godot.Collections;
using System;
using 维修公司.Utils;

namespace 途畔归所.Dll.Manager
{
    /// <summary>UI资源管理器</summary>
    public class UIManager
    {

        private static UIManager _instance;
        public static UIManager Instance => _instance ??= new UIManager();


        private UIManager() { }


        public Dictionary<string, PackedScene> m_UiDict = [];



        /// <summary>初始化UI资源</summary>
        /// <param name="packedScene">UI预制件列表</param>
        public void Init()
        {
            if (ResourceManager.Instance.m_UIAssetList == null) return;

            foreach (var ui in ResourceManager.Instance.m_UIAssetList)
            {
                string uiName = ToolUtils.GetResourceName(ui.ResourcePath);
                if (uiName == null) continue;
                if (m_UiDict.ContainsKey(uiName))
                {
                    GD.PrintErr($"[UIManager.Init] UI资源 {uiName} 已存在，跳过");
                    continue;
                }
                m_UiDict.Add(uiName, ui);
            }
        }


        /// <summary>获取UI预制件</summary>
        /// <param name="uiName">UI预制件名称</param>
        /// <returns>独立的Control实例，失败返回null</returns>
        public Control GetUI(string uiName)
        {
            if (!m_UiDict.TryGetValue(uiName, out var prefabUi))
            {
                GD.PrintErr($"[UIManager.GetUI] UI {uiName} 不存在");
                return null;
            }

            Node uiNode = prefabUi.Instantiate();

            if (uiNode == null)
            {
                GD.PrintErr($"[UIManager.GetUI] UI {uiName} 实例化失败");
                return null;
            }

            Control uiInstance = uiNode as Control;
            if (uiInstance == null)
            {
                GD.PrintErr($"[UIManager.GetUI] UI {uiName} 根节点不是 Control");
                uiNode.QueueFree(); 
                return null;
            }
            return uiInstance;
        }

    }
}
