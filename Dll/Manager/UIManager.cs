using Godot;
using Godot.Collections;
using System;
using 维修公司.Utils;
using 途畔归所.Dll.Base;

namespace 维修公司.Dll
{
    /// <summary>UI资源管理器</summary>
    public class UIManager
    {

        public Dictionary<string, PackedScene> m_UiDict = [];



        /// <summary>初始化UI资源</summary>
        /// <param name="packedScene">UI预制件列表</param>
        public void Init(PackedScene packedScene)
        {
            if (packedScene == null)
            {
                GD.PrintErr("[UIManager.Init]：跳过空的预制件");
                return;
            }

            if (!(packedScene.Instantiate() is UIPanelBase)) return;

            string uiName = ToolUtils.GetResourceName(packedScene.ResourcePath);

            if (uiName == null) return;

            if (m_UiDict.ContainsKey(uiName))
            {
                GD.PrintErr($"[UIManager.Init]：UI资源： {uiName} 已存在，跳过");
                return;
            }

            m_UiDict.Add(uiName, packedScene);
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
