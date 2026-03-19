using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Utils;

namespace 维修公司.Dll
{
    /// <summary>UI资源管理器</summary>
    public class UIManager
    {
        private static Lazy<UIManager> _instance = new Lazy<UIManager>(() => new UIManager());

        public static UIManager Instance = _instance.Value;

        private UIManager() { }


        public System.Collections.Generic.Dictionary<string, PackedScene> m_UiDict = new System.Collections.Generic.Dictionary<string, PackedScene>();



        /// <summary>初始化UI资源</summary>
        /// <param name="uiAsset"></param>
        /// <summary>初始化UI资源</summary>
        /// <param name="packedScenes">UI预制件列表</param>
        public void InitUIManager(Array<PackedScene> packedScenes)
        {
            m_UiDict.Clear();

            foreach (var prefabUi in packedScenes)
            {
                if (prefabUi == null)
                {
                    GD.PrintErr("[UIManager.InitUIManager] 跳过空的预制件");
                    continue;
                }

                string uiName = ToolUtils.GetResourceName(prefabUi.ResourcePath);

                if (uiName == null) continue;

                if (m_UiDict.ContainsKey(uiName))
                {
                    GD.PrintErr($"[UIManager.InitUIManager] UI {uiName} 已存在，跳过");
                    continue;
                }

                m_UiDict.Add(uiName, prefabUi);
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
