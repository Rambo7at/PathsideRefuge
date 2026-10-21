using Godot;
using Godot.Collections;
using System.Collections.Generic;
using TerraEnvEditor.Data;
using TerraEnvEditor.Terrain;

namespace TerraEnvEditor.Decoration;

/// <summary>注：杂物管理器——多 Clutter 类型+固定种子+材质权重过滤+海拔/坡度/分形成簇，MultiMesh 批量渲染，地形修改后自动更新 Y</summary>
[Tool]
[GlobalClass]
public partial class ClutterManager : Node3D
{
	/// <summary>注：单个实例的静态数据（XZ 位置固定，Y 从高度图实时取）</summary>
	private struct InstanceData
	{
		public float X;
		public float Z;
		public float Yaw;
		public float Scale;
	}

	/// <summary>注：已注册的杂物配置列表，Inspector 里拖 .tres 资源</summary>
	[Export] public Array<ClutterData> Clutters = [];

	/// <summary>注：绑定的地形区域，提供高度/权重采样和 HeightChanged 事件</summary>
	[Export] public TerrainRegion Region;

	[ExportGroup("区块管理")]
	/// <summary>注：区块边长（米），每个区块独立管理 MultiMesh</summary>
	[Export] public float ChunkSize = 64f;

	/// <summary>注：相机可见半径（米），超出的区块隐藏</summary>
	[Export] public float ViewRadius = 80f;

	[ExportGroup("调试")]
	/// <summary>注：勾选后下一帧重新生成全部杂物</summary>
	[Export] private bool _regen = false;

	/// <summary>注：区块实例数据缓存：(cx,cz) → (clutterIndex → 实例列表)</summary>
	private System.Collections.Generic.Dictionary<(int cx, int cz), System.Collections.Generic.Dictionary<int, List<InstanceData>>> _chunkData = new();

	/// <summary>注：区块渲染节点缓存：(cx,cz) → (clutterIndex → MultiMeshInstance3D)</summary>
	private System.Collections.Generic.Dictionary<(int cx, int cz), System.Collections.Generic.Dictionary<int, MultiMeshInstance3D>> _chunks = new();

	/// <summary>注：分形噪声，用于杂物成簇分布</summary>
	private FastNoiseLite _fractalNoise;

	public override void _Ready()
	{
		GD.Print($"[ClutterManager] _Ready 执行, 节点={Name}");
		GD.Print($"  Region = {(Region == null ? "null" : Region.Name)}");
		GD.Print($"  Clutters.Count = {Clutters.Count}");
		for (int i = 0; i < Clutters.Count; i++)
		{
			var c = Clutters[i];
			GD.Print($"  [{i}] 类型={c?.GetType().Name}, Mesh={c?.Mesh}, Enabled={c?.Enabled}, Amount={c?.Amount}");
		}

		if (Region != null)
		{
			Region.HeightChanged += OnHeightChanged;
		}

		_fractalNoise = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			FractalOctaves = 3,
			FractalLacunarity = 2.0f,
			FractalGain = 0.5f,
			Frequency = 0.01f
		};

		if (!Engine.IsEditorHint())
		{
			Generate(); // 游戏运行时自动生成，编辑器里仍手动点 _regen
		}

	}

	public override void _ExitTree()
	{
		if (Region != null)
			Region.HeightChanged -= OnHeightChanged;
	}

	public override void _Process(double delta)
	{
		if (_regen)
		{
			_regen = false;
			Generate();
		}
		UpdateChunkVisibility();
	}

	/// <summary>注：代码动态注册一个杂物配置（跟 Inspector 配置不冲突，共用同一个列表）</summary>
	public void Register(ClutterData clutter) => Clutters.Add(clutter);

	/// <summary>注：按区块生成所有 Clutter 的所有实例，每区块固定种子确保位置不变</summary>
	public void Generate()
	{
		if (Region == null) { GD.Print("[ClutterManager] 未绑定 TerrainRegion"); return; }
		if (Clutters.Count == 0) { GD.Print("[ClutterManager] 没有注册任何 Clutter"); return; }
		if (!Region.HasHeightMap()) { GD.Print("[ClutterManager] 地形未生成高度图"); return; }

		ClearChunks();

		float sizeX = (float)Region.RegionSizeX;
		float sizeZ = (float)Region.RegionSizeZ;
		int chunksX = Mathf.CeilToInt(sizeX / ChunkSize);
		int chunksZ = Mathf.CeilToInt(sizeZ / ChunkSize);

		int total = 0;
		for (int cz = 0; cz < chunksZ; cz++)
		{
			for (int cx = 0; cx < chunksX; cx++)
			{
				GenerateChunk(cx, cz, sizeX, sizeZ);
				total++;
			}
		}

		GD.Print($"[ClutterManager] 生成完成：{total} 个区块，{Clutters.Count} 种杂物");
	}

	/// <summary>注：生成单个区块，遍历所有 Clutter，固定种子+层层过滤（权重/海拔/坡度/分形）</summary>
	private void GenerateChunk(int cx, int cz, float sizeX, float sizeZ)
	{
		float minX = -sizeX * 0.5f + cx * ChunkSize;
		float maxX = Mathf.Min(minX + ChunkSize, sizeX * 0.5f);
		float minZ = -sizeZ * 0.5f + cz * ChunkSize;
		float maxZ = Mathf.Min(minZ + ChunkSize, sizeZ * 0.5f);

		var dataDict = new System.Collections.Generic.Dictionary<int, List<InstanceData>>();

		for (int ci = 0; ci < Clutters.Count; ci++)
		{
			var clutter = Clutters[ci];
			if (!clutter.Enabled || clutter.Mesh == null)
			{
				GD.Print($"  跳过 Clutter[{ci}] Enabled={clutter.Enabled} Mesh={clutter.Mesh}");
				continue;
			}

			var rng = new RandomNumberGenerator();
			rng.Seed = (ulong)((long)cx * 73856093 ^ (long)cz * 19349663 ^ (long)ci * 9321);

			var data = new List<InstanceData>(clutter.Amount);
			float maxTiltDot = Mathf.Cos(Mathf.DegToRad(clutter.MaxTilt));

			for (int i = 0; i < clutter.Amount; i++)
			{
				float x = rng.RandfRange(minX, maxX);
				float z = rng.RandfRange(minZ, maxZ);

				int gx = (int)Mathf.Round(x + sizeX * 0.5f);
				int gz = (int)Mathf.Round(z + sizeZ * 0.5f);

				// 材质权重过滤
				if (clutter.GrassWeightMin >= 0f || clutter.GrassWeightMax >= 0f)
				{
					float gw = Region.GetGrassWeight(gx, gz);
					if (clutter.GrassWeightMin >= 0f && gw < clutter.GrassWeightMin) continue;
					if (clutter.GrassWeightMax >= 0f && gw > clutter.GrassWeightMax) continue;
				}

				// 海拔过滤
				float y = Region.GetHeightAt(gx, gz);
				if (y < clutter.MinAltitude || y > clutter.MaxAltitude) continue;

				// 坡度过滤
				float hL = Region.GetHeightAt(gx - 1, gz);
				float hR = Region.GetHeightAt(gx + 1, gz);
				float hD = Region.GetHeightAt(gx, gz - 1);
				float hU = Region.GetHeightAt(gx, gz + 1);
				var normal = new Vector3(hL - hR, 2f, hD - hU).Normalized();
				if (normal.Y < maxTiltDot) continue;

				// 分形成簇过滤
				if (clutter.FractalScale > 0f)
				{
					float fbm = _fractalNoise.GetNoise2D(x * 0.01f * clutter.FractalScale, z * 0.01f * clutter.FractalScale);
					fbm = (fbm + 1f) * 0.5f;
					if (fbm < clutter.FractalThresholdMin || fbm > clutter.FractalThresholdMax) continue;
				}

				data.Add(new InstanceData
				{
					X = x,
					Z = z,
					Yaw = rng.Randf() * Mathf.Tau,
					Scale = rng.RandfRange(clutter.ScaleMin, clutter.ScaleMax)
				});
			}

			GD.Print($"  区块({cx},{cz}) Clutter[{ci}] 实例数={data.Count}");

			if (data.Count > 0)
			{
				dataDict[ci] = data;
				_chunkData[(cx, cz)] = dataDict; // 必须先存，RebuildChunkMesh 才能从 _chunkData 取到数据
				RebuildChunkMesh(cx, cz, ci, sizeX, sizeZ);
			}
		}

		if (dataDict.Count > 0)
			_chunkData[(cx, cz)] = dataDict;
	}

	/// <summary>注：根据缓存数据重建指定 Clutter 的 MultiMesh（Y 从高度图实时取，简单旋转+缩放，跟成功版本一致）</summary>
	private void RebuildChunkMesh(int cx, int cz, int clutterIndex, float sizeX, float sizeZ)
	{
		if (!_chunkData.TryGetValue((cx, cz), out var dataDict)) return;
		if (!dataDict.TryGetValue(clutterIndex, out var data)) return;

		var clutter = Clutters[clutterIndex];
		if (clutter?.Mesh == null) return;

		var mm = new MultiMesh();
		mm.Mesh = clutter.Mesh;
		mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		mm.InstanceCount = data.Count;

		for (int i = 0; i < data.Count; i++)
		{
			var inst = data[i];
			int gx = (int)Mathf.Round(inst.X + sizeX * 0.5f);
			int gz = (int)Mathf.Round(inst.Z + sizeZ * 0.5f);
			float y = Region.GetHeightAt(gx, gz);

			var t = Transform3D.Identity;
			t.Origin = new Vector3(inst.X, y, inst.Z);
			// 跟成功版本完全一致的简单 Basis：绕 Y 轴旋转 + 统一缩放
			t.Basis = Basis.Identity.Rotated(Vector3.Up, inst.Yaw)
				.Scaled(new Vector3(inst.Scale, inst.Scale, inst.Scale));

			mm.SetInstanceTransform(i, t);
		}

		if (!_chunks.TryGetValue((cx, cz), out var mmiDict))
		{
			mmiDict = new System.Collections.Generic.Dictionary<int, MultiMeshInstance3D>();
			_chunks[(cx, cz)] = mmiDict;
		}

		if (!mmiDict.TryGetValue(clutterIndex, out var mmi))
		{
			mmi = new MultiMeshInstance3D { Name = $"Chunk_{cx}_{cz}_{clutter.Name}" };
			AddChild(mmi);
			mmiDict[clutterIndex] = mmi;
			GD.Print($"  创建 MultiMeshInstance3D: {mmi.Name}, 实例数={data.Count}");
		}

		mmi.Multimesh = mm;
		mmi.Visible = true; // 强制可见，调试用
	}

	/// <summary>注：地形高度变化时，只更新受影响范围内实例的 Y 坐标和可见性（权重不够则缩放为0隐藏）</summary>
	public void UpdateHeightsInRange(Vector3 worldPos, float radius)
	{
		if (Region == null || _chunkData.Count == 0) return;

		Vector3 localPos = Region.ToLocal(worldPos);
		float sizeX = (float)Region.RegionSizeX;
		float sizeZ = (float)Region.RegionSizeZ;
		float effectRadius = radius + 2f;
		float r2 = effectRadius * effectRadius;

		int minCX = (int)Mathf.Floor((localPos.X - effectRadius + sizeX * 0.5f) / ChunkSize);
		int maxCX = (int)Mathf.Floor((localPos.X + effectRadius + sizeX * 0.5f) / ChunkSize);
		int minCZ = (int)Mathf.Floor((localPos.Z - effectRadius + sizeZ * 0.5f) / ChunkSize);
		int maxCZ = (int)Mathf.Floor((localPos.Z + effectRadius + sizeZ * 0.5f) / ChunkSize);

		for (int cz = minCZ; cz <= maxCZ; cz++)
			for (int cx = minCX; cx <= maxCX; cx++)
			{
				if (!_chunkData.TryGetValue((cx, cz), out var dataDict)) continue;
				if (!_chunks.TryGetValue((cx, cz), out var mmiDict)) continue;

				foreach (var kv in dataDict)
				{
					int ci = kv.Key;
					var data = kv.Value;
					if (!mmiDict.TryGetValue(ci, out var mmi) || mmi.Multimesh == null) continue;

					var clutter = Clutters[ci];
					var mm = mmi.Multimesh;

					for (int i = 0; i < data.Count; i++)
					{
						var inst = data[i];
						float dx = inst.X - localPos.X;
						float dz = inst.Z - localPos.Z;
						if (dx * dx + dz * dz > r2) continue;

						int gx = (int)Mathf.Round(inst.X + sizeX * 0.5f);
						int gz = (int)Mathf.Round(inst.Z + sizeZ * 0.5f);

						var t = mm.GetInstanceTransform(i);

						bool hide = clutter.GrassWeightMin >= 0f && Region.GetGrassWeight(gx, gz) < clutter.GrassWeightMin;
						if (hide)
						{
							t.Basis = Basis.Scaled(Vector3.Zero);
						}
						else
						{
							float newY = Region.GetHeightAt(gx, gz);
							t.Origin = new Vector3(inst.X, newY, inst.Z);
							t.Basis = Basis.Identity.Rotated(Vector3.Up, inst.Yaw)
								.Scaled(new Vector3(inst.Scale, inst.Scale, inst.Scale));
						}

						mm.SetInstanceTransform(i, t);
					}
				}
			}
	}

	/// <summary>注：地形高度变化事件回调，转发到 UpdateHeightsInRange</summary>
	private void OnHeightChanged(Vector3 worldPos, float radius) => UpdateHeightsInRange(worldPos, radius);

	/// <summary>注：根据摄像机位置更新区块可见性（超出 ViewRadius 的区块隐藏）</summary>
	private void UpdateChunkVisibility()
	{
		if (_chunks.Count == 0 || Region == null) return;

		var cam = GetActiveCamera();
		if (cam == null) return;

		Vector3 localCam = Region.ToLocal(cam.GlobalPosition);
		float sizeX = (float)Region.RegionSizeX;
		float sizeZ = (float)Region.RegionSizeZ;

		int camChunkX = (int)Mathf.Floor((localCam.X + sizeX * 0.5f) / ChunkSize);
		int camChunkZ = (int)Mathf.Floor((localCam.Z + sizeZ * 0.5f) / ChunkSize);
		int viewChunks = Mathf.CeilToInt(ViewRadius / ChunkSize);

		foreach (var kv in _chunks)
		{
			bool visible = Mathf.Abs(kv.Key.cx - camChunkX) <= viewChunks
						 && Mathf.Abs(kv.Key.cz - camChunkZ) <= viewChunks;
			foreach (var mmi in kv.Value.Values)
				mmi.Visible = visible;
		}
	}

	/// <summary>注：清空所有区块的实例数据和渲染节点</summary>
	public void Clear() => ClearChunks();

	/// <summary>注：内部清空——释放所有 MultiMeshInstance3D 并清除缓存</summary>
	private void ClearChunks()
	{
		foreach (var kv in _chunks)
			foreach (var mmi in kv.Value.Values)
				mmi.QueueFree();
		_chunks.Clear();
		_chunkData.Clear();
	}

	/// <summary>注：获取当前活动相机（编辑器模式下取编辑器视口相机，运行时取视口相机）</summary>
	private Camera3D GetActiveCamera()
	{
		if (Engine.IsEditorHint())
			return EditorInterface.Singleton?.GetEditorViewport3D(0)?.GetCamera3D();
		return GetViewport()?.GetCamera3D();
	}
}
