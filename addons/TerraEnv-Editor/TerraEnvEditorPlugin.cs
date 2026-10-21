#if TOOLS
using Godot;
using TerraEnvEditor.Terrain;

namespace TerraEnvEditor;

[Tool]
public partial class TerraEnvEditorPlugin : EditorPlugin
{
    private TerraEditorDock TerrainEditorTool { get; set; }
    private TerraRegionInspectorPlugin TerrainTool { get; set; }

    private TerrainRegion _editingTerrain;

    public override void _EnterTree()
    {

        TerrainTool ??= new TerraRegionInspectorPlugin();
        TerrainEditorTool ??= new TerraEditorDock();

        TerrainEditorTool.Init();

        AddInspectorPlugin(TerrainTool);
        AddDock(TerrainEditorTool);

    }

    public override void _ExitTree()
    {
        if (TerrainEditorTool != null)
        {
            RemoveInspectorPlugin(TerrainTool);
            TerrainTool = null;
        }

        RemoveDock(TerrainEditorTool);
        TerrainEditorTool?.QueueFree();
    }

    // 声明插件处理 TerrainRegion 类型
    public override bool _Handles(GodotObject @object) => @object is TerrainRegion;

    // 选中 TerrainRegion 时调用，保存引用
    public override void _Edit(GodotObject @object)
    {
        if (@object is TerrainRegion region)
        {
            _editingTerrain = region;
        }
        else
        {
            _editingTerrain = null;
        }
    }

    public override void _MakeVisible(bool visible)
    {
        // 后续控制笔刷 UI 的显示隐藏
    }

    public override int _Forward3DGuiInput(Camera3D viewportCamera, InputEvent @event)
    {
        if (_editingTerrain == null) return 0; // AFTER_GUI_INPUT_PASS

        TerrainEditorTool.HandleInput(viewportCamera, @event);

        return 0; // 0 = PASS，事件继续传给编辑器相机控制
    }

}
#endif
