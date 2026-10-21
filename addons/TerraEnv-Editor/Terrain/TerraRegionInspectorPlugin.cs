
using Godot;

namespace TerraEnvEditor.Terrain;

[Tool]
internal partial class TerraRegionInspectorPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject @object)
	{
		return @object is TerrainRegion;
	}

	public override void _ParseBegin(GodotObject @object)
	{
		if (@object is not TerrainRegion region) return;

		// 标题
		AddCustomControl(new Label
		{
			Text = "—— 地形工具 ——",
			HorizontalAlignment = HorizontalAlignment.Center
		});

		// 重新生成
		var genBtn = new Button { Text = "重新生成" };
		genBtn.Pressed += () => region.GenTerrain();
		AddCustomControl(genBtn);

		// 保存
		var saveBtn = new Button { Text = "保存地形" };
		saveBtn.Pressed += () =>
		{
			if (region.Data == null) { GD.Print("[地形] Data 为空，无法保存"); return; }
			ResourceSaver.Save(region.Data, "res://terrain_data.tres");
			GD.Print("[地形] 已保存到 res://terrain_data.tres");
		};
		AddCustomControl(saveBtn);

		// 加载
		var loadBtn = new Button { Text = "加载地形" };
		loadBtn.Pressed += () =>
		{
			if (region.Data == null) { GD.Print("[地形] Data 为空，请先在 Inspector 指定 RegionData"); return; }
			region.LoadFromData(region.Data);
			GD.Print("[地形] 已从 Data 加载");
		};
		AddCustomControl(loadBtn);
	}
}
