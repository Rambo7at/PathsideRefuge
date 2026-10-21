using Godot;
using Godot.Collections;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>UI资源管理器</summary>
public class GUIManager
{

    private static GUIManager _instance;
    public static GUIManager Instance => _instance ??= new GUIManager();


    private GUIManager() { }


    private Dictionary<int, PackedScene> GUiDict = [];


    public void RegisterGUI(int hash, PackedScene packedScene)
    {
        if (packedScene == null) return;

        if (!GUiDict.ContainsKey(hash)) GUiDict[hash] = packedScene;
    }

    /// <summary>注：获取UI预制件</summary>
    public Control GetUI(string uiName)
    {
        int hash = CatUtils.GetStableHashCode(uiName);

        if (!GUiDict.TryGetValue(hash, out var prefabUi))
        {
            CatLog.Warn($"[GUIManager.GetUI] UI {uiName} 不存在，返回空");
            return null;
        }

        if (prefabUi.Instantiate() is not Control UI)
        {
            CatLog.Warn($"[GUIManager.GetUI] UI {uiName} 不属于 Control 请尝试 View 获取 ，返回空");
            return null;
        }

        return UI;
    }


    /// <summary>注：获取View预制件</summary>
    public CanvasLayer GetView(string uiName)
    {
        int hash = CatUtils.GetStableHashCode(uiName);

        if (!GUiDict.TryGetValue(hash, out var prefabUi))
        {
            CatLog.Warn($"[GUIManager.GetUI] CanvasLayer {uiName} 不存在，返回空");
            return null;
        }

        if (prefabUi.Instantiate() is not CanvasLayer layer)
        {
            CatLog.Warn($"[GUIManager.GetUI] CanvasLayer {uiName} 不属于 CanvasLayer 请尝试 UI 获取 ，返回空");
            return null;
        }

        return layer;
    }

}
