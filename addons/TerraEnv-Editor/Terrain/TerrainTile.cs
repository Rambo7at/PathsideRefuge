using Godot;

namespace TerraEnvEditor.Terrain;

/// <summary>注：地形瓦片，单块网格生成，高度与材质由 Region 统一管理</summary>
public partial class TerrainTile : Node3D
{
	/// <summary>注：网格精度，顶点数 = Precision + 1</summary>
	public int Precision = 64;

	/// <summary>注：瓦片在全局网格中的坐标</summary>
	public int GridX;
	public int GridZ;

	private TerrainRegion _region;
	private MeshInstance3D _meshInstance;
	private StaticBody3D _body;
	private CollisionShape3D _collisionShape;
	private ShaderMaterial _paintShader;
	private NavigationRegion3D _navRegion;

	/// <summary>注：转发到内部 MeshInstance3D，保持外部调用兼容</summary>
	public Mesh Mesh
	{
		get => _meshInstance?.Mesh;
		set { if (_meshInstance != null) _meshInstance.Mesh = value; }
	}

	/// <summary>注：顶点总数</summary>
	private int VertexCount => (Precision + 1) * (Precision + 1);
	private Vector3[] _cachedNormals;

	/// <summary>注：初始化瓦片，创建渲染、碰撞、导航子节点</summary>
	public void Init(int precision, int gridX, int gridZ, Material paintMaterial, TerrainRegion region)
	{
		Precision = precision;
		GridX = gridX;
		GridZ = gridZ;
		_region = region;

		CreateNodes();

		if (paintMaterial == null) return;
		if (paintMaterial.Duplicate() is not Material mat) return;
		if (mat is not ShaderMaterial shader) return;

		_meshInstance.MaterialOverride = mat;
		_paintShader = shader;
	}

	/// <summary>注：创建内部子节点——MeshInstance3D、StaticBody3D、NavigationRegion3D</summary>
	private void CreateNodes()
	{
		// 渲染节点（放在 NavigationRegion3D 下面，方便烘焙扫描）
		_meshInstance = new MeshInstance3D();

		// 碰撞体
		_body = new StaticBody3D();
		_collisionShape = new CollisionShape3D();
		_body.AddChild(_collisionShape);
		_body.CollisionLayer = 1;
		_body.CollisionMask = 1;
		AddChild(_body);

		// 导航区域（MeshInstance3D 作为其子节点，默认 RootNodeChildren 模式直接扫描）
		_navRegion = new NavigationRegion3D();
		var navMesh = new NavigationMesh
		{
			AgentRadius = 0f,
			AgentHeight = 1.8f,
			AgentMaxSlope = 45f,
			BorderSize = 0f,
			SamplePartitionType = NavigationMesh.SamplePartitionTypeEnum.Monotone,
			GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.MeshInstances,
			GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.RootNodeChildren
		};
		_navRegion.NavigationMesh = navMesh;
		_navRegion.AddChild(_meshInstance);
		AddChild(_navRegion);
	}

	/// <summary>注：生成地形网格、碰撞体和导航网格</summary>
	public void GenMesh(bool updateCollision = true)
	{
		UpdateSplatTexture();

		var sur = new SurfaceTool();
		sur.Begin(Mesh.PrimitiveType.Triangles);
		_cachedNormals = CalculateNormals();

		for (int z = 0; z < Precision + 1; z++)
		{
			for (int x = 0; x < Precision + 1; x++)
			{
				float localX = ((float)x / Precision - 0.5f) * 64f;
				float localZ = ((float)z / Precision - 0.5f) * 64f;
				float height = GetHeight(x, z);
				sur.SetUV(new Vector2((float)x / Precision, (float)z / Precision));
				sur.SetNormal(_cachedNormals[z * (Precision + 1) + x]);
				sur.AddVertex(new Vector3(localX, height, localZ));
			}
		}

		for (int z = 0; z < Precision; z++)
		{
			for (int x = 0; x < Precision; x++)
			{
				int v0 = z * (Precision + 1) + x;
				int v1 = z * (Precision + 1) + (x + 1);
				int v2 = (z + 1) * (Precision + 1) + (x + 1);
				int v3 = (z + 1) * (Precision + 1) + x;
				sur.AddIndex(v0); sur.AddIndex(v1); sur.AddIndex(v2);
				sur.AddIndex(v0); sur.AddIndex(v2); sur.AddIndex(v3);
			}
		}

		_meshInstance.Mesh = sur.Commit();
		sur.Clear();
		if (updateCollision) UpdateCollision();
		BakeNavigation();
	}

	/// <summary>注：从 Region 采样材质纹理，生成本瓦片的 splat 图</summary>
	private void UpdateSplatTexture()
	{
		if (_paintShader == null) return;

		var img = Image.CreateEmpty(Precision + 1, Precision + 1, false, Image.Format.Rgba8);

		for (int z = 0; z < Precision + 1; z++)
		{
			for (int x = 0; x < Precision + 1; x++)
			{
				int gx = GridX * Precision + x;
				int gz = GridZ * Precision + z;
				img.SetPixel(x, z, _region.GetSplatPixel(gx, gz));
			}
		}

		_paintShader.SetShaderParameter("splat_map", ImageTexture.CreateFromImage(img));
	}

	/// <summary>注：计算顶点法线，边缘从全局高度图取邻居，跨瓦片连续</summary>
	private Vector3[] CalculateNormals()
	{
		Vector3[] normals = new Vector3[VertexCount];
		float cell = 64f / (float)Precision;

		for (int z = 0; z < Precision + 1; z++)
			for (int x = 0; x < Precision + 1; x++)
			{
				float hL = GetHeight(x - 1, z);
				float hR = GetHeight(x + 1, z);
				float hD = GetHeight(x, z - 1);
				float hU = GetHeight(x, z + 1);
				Vector3 normal = new Vector3(hL - hR, 2f * cell, hD - hU).Normalized();
				normals[z * (Precision + 1) + x] = normal;
			}
		return normals;
	}

	/// <summary>注：重建高度图碰撞体</summary>
	private void UpdateCollision()
	{
		if (_collisionShape == null) return;

		var shape = new HeightMapShape3D
		{
			MapWidth = Precision + 1,
			MapDepth = Precision + 1
		};

		var heights = new float[(Precision + 1) * (Precision + 1)];
		for (int z = 0; z < Precision + 1; z++)
		{
			for (int x = 0; x < Precision + 1; x++)
			{
				heights[z * (Precision + 1) + x] = GetHeight(x, z);
			}
		}

		shape.MapData = heights;
		_collisionShape.Shape = shape;
	}

	/// <summary>注：烘焙导航网格，从子 MeshInstance3D 的 Mesh 解析几何并简化</summary>
	private void BakeNavigation()
	{
		if (_navRegion == null || _meshInstance?.Mesh == null || Engine.IsEditorHint()) return;
		_navRegion.BakeNavigationMesh();
	}

	/// <summary>注：从全局高度图读取瓦片局部坐标对应的高度</summary>
	public float GetHeight(int x, int z) => _region.GetHeightAt(GridX * Precision + x, GridZ * Precision + z);

	/// <summary>注：卸载网格和碰撞，保留节点结构，下次 GenMesh 恢复</summary>
	public void Unload()
	{
		_meshInstance.Mesh = null;
		if (_collisionShape != null)
			_collisionShape.Shape = null;
		if (_navRegion?.NavigationMesh != null)
			_navRegion.NavigationMesh.Clear();
	}
}
