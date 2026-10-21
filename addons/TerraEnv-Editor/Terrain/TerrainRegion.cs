using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerraEnvEditor.Data;
using Color = Godot.Color;

namespace TerraEnvEditor.Terrain;

/// <summary>注：地形区域显示层——持有 RegionData 数据，创建/刷新 Tile 渲染网格，桥接编辑刷子</summary>
[Tool]
[GlobalClass]
public partial class TerrainRegion : Node3D
{
    /// <summary>注：高度被修改时发射，参数为世界坐标和影响半径，供装饰物等系统实时更新</summary>
    public event Action<Vector3, float> HeightChanged;

    public enum E_BiomeType
    {
        Grassland = 0,
        Forest = 1,
    }

    [Export] public E_BiomeType CurrentBiome = E_BiomeType.Grassland;
    [Export] public Material PaintMaterial;
    [Export] public NoiseTexture2D NoiseTexture;

    /// <summary>注：地形数据（高度图+材质图），可持久化 Resource</summary>
    [Export] public RegionData Data { get; set; }

    [ExportGroup("流式加载")]
    /// <summary>注：启用后只加载相机周围的 Tile，远处自动卸载（编辑器模式下强制全量加载）</summary>
    [Export] public bool StreamLoading = false;
    /// <summary>注：加载半径（单位 Tile，3 = 相机周围 7×7=49 块）</summary>
    [Export] public int LoadRadius = 3;
    /// <summary>注：卸载延迟（秒），Tile 离开视野后多久真正卸载，避免频繁加载卸载</summary>
    [Export] public float UnloadDelay = 5f;

    /// <summary>注：区域宽度（米），转发自 Data</summary>
    public int RegionSizeX => (int)(Data?.RegionSize ?? 0);

    /// <summary>注：区域深度（米），转发自 Data</summary>
    public int RegionSizeZ => (int)(Data?.RegionSize ?? 0);

    private const int TilePrecision = 64;
    private int _tilesX;
    private int _tilesZ;
    private TerrainTile[,] _grid;
    /// <summary>注：待卸载的 Tile 记录（坐标 → 进入待卸载状态的时间戳）</summary>
    private Dictionary<(int x, int z), double> _pendingUnload = new();
    /// <summary>注：流式加载待加载队列，每帧只处理前几个避免卡顿</summary>
    private readonly List<TerrainTile> _loadQueue = new();


    public override void _Ready()
    {
        if (Data == null) return;

        LoadFromData(Data);




    }

    /// <summary>注：地形是否已生成高度图</summary>
    public bool HasHeightMap() => Data?.HeightImage != null;

    /// <summary>注：世界坐标转高度图全局索引</summary>
    private (int x, int z) WorldToMapIndex(Vector3 worldPos)
    {
        int half = (int)Data.RegionSize / 2;
        int gx = (int)Mathf.Round(worldPos.X + half);
        int gz = (int)Mathf.Round(worldPos.Z + half);
        return (gx, gz);
    }

    /// <summary>注：创建所有 Tile 节点（不生成 Mesh，流式加载由 UpdateStreaming 按需生成）</summary>
    private void CreateTiles()
    {
        int size = (int)Data.RegionSize;
        _tilesX = size / TilePrecision;
        _tilesZ = size / TilePrecision;
        float half = size * 0.5f;

        _grid = new TerrainTile[_tilesX, _tilesZ];

        for (int gz = 0; gz < _tilesZ; gz++)
        {
            for (int gx = 0; gx < _tilesX; gx++)
            {
                var tile = new TerrainTile();
                tile.Init(TilePrecision, gx, gz, PaintMaterial, this);
                tile.Position = new Vector3(-half + 32f + gx * 64f, 0, -half + 32f + gz * 64f);
                AddChild(tile);
                _grid[gx, gz] = tile;
            }
        }
    }

    /// <summary>注：生成地形——创建 RegionData 并从噪声采样，再创建所有 Tile</summary>
    public void GenTerrain()
    {
        var sw = Stopwatch.StartNew();
        var total = Stopwatch.StartNew();

        ClearTiles();
        GD.Print($"[Profiler] ClearTiles: {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        Data ??= new RegionData();
        if (NoiseTexture != null)
        {
            Data.GenerateFromNoise(NoiseTexture);
        }
        else
        {
            Data.GenerateProcedural();
        }

        GD.Print($"[Profiler] 高度图数据生成: {sw.ElapsedMilliseconds}ms");
        sw.Restart();

        CreateTiles();
        GD.Print($"[Profiler] Tile节点创建 ({_tilesX}×{_tilesZ}): {sw.ElapsedMilliseconds}ms");

        // 流式加载：勾选后只创建 tile 节点不生成 mesh，由 UpdateStreaming 按需加载
        if (!StreamLoading)
        {
            sw.Restart();
            for (int gz = 0; gz < _tilesZ; gz++)
                for (int gx = 0; gx < _tilesX; gx++)
                    _grid[gx, gz].GenMesh();
            GD.Print($"[Profiler] 全部Tile GenMesh: {sw.ElapsedMilliseconds}ms");
        }
        else
        {
            _pendingUnload.Clear();
        }

        sw.Restart();
        GenerateDistant();
        GD.Print($"[Profiler] 远景地形: {sw.ElapsedMilliseconds}ms");

        GD.Print($"[Profiler] ===== GenTerrain 总耗时: {total.ElapsedMilliseconds}ms =====");
    }

    /// <summary>注：生成远景地形 Mesh（低精度、只渲染、无碰撞），Y 稍低避免与主地形 Z-fighting</summary>
    private void GenerateDistant()
    {
        // 先清除旧的远景
        var old = GetNodeOrNull("DistantTerrain");
        if (old != null) old.QueueFree();

        if (Data == null || !Data.EnableDistant) return;

        var mesh = Data.GenerateDistantMesh();
        if (mesh == null) return;

        var mmi = new MeshInstance3D();
        mmi.Name = "DistantTerrain";
        mmi.Mesh = mesh;
        mmi.MaterialOverride = PaintMaterial;  // 跟主地形用同一个材质
        AddChild(mmi);
    }

    // ---------- 数据转发（Tile 调这些方法，不直接碰 RegionData） ----------

    /// <summary>注：从 RegionData 读取指定全局坐标的高度</summary>
    public float GetHeightAt(int globalX, int globalZ) => Data?.GetHeightAt(globalX, globalZ) ?? 0f;

    /// <summary>注：写入 RegionData 指定全局坐标的高度</summary>
    public void SetHeightAt(int globalX, int globalZ, float h) => Data?.SetHeightAt(globalX, globalZ, h);

    /// <summary>注：从 RegionData 读取材质像素</summary>
    public Color GetSplatPixel(int globalX, int globalZ)
    {
        if (Data?.SplatImage == null) return new Color(0, 0, 0, 0);
        if (globalX < 0 || globalZ < 0 || globalX >= Data.MapWidth || globalZ >= Data.MapHeight)
            return new Color(0, 0, 0, 0);
        return Data.SplatImage.GetPixel(globalX, globalZ);
    }

    // ---------- 编辑操作（改 Data → 刷新 Tile → 发信号） ----------

    /// <summary>注：在世界坐标处设置高度（范围内所有顶点统一设为目标高度，用于平整）</summary>
    public void ModifyHeightPoint(Vector3 worldPos, float radius, float targetHeight)
    {
        var (cx, cz) = WorldToMapIndex(worldPos);
        int r = Mathf.CeilToInt(radius);

        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (new Vector2(dx, dz).Length() > radius) continue;
                SetHeightAt(cx + dx, cz + dz, targetHeight);
            }

        RefreshTilesInRange(cx, cz, r);
        HeightChanged?.Invoke(worldPos, radius);
    }

    /// <summary>注：在世界坐标处增减高度（每个顶点在当前高度基础上加减delta，用于增幅/降低）</summary>
    public void RaiseHeightPoint(Vector3 worldPos, float radius, float delta)
    {
        var (cx, cz) = WorldToMapIndex(worldPos);
        int r = Mathf.CeilToInt(radius);

        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (new Vector2(dx, dz).Length() > radius) continue;
                SetHeightAt(cx + dx, cz + dz, GetHeightAt(cx + dx, cz + dz) + delta);
            }

        RefreshTilesInRange(cx, cz, r);
        HeightChanged?.Invoke(worldPos, radius);
    }

    /// <summary>注：在世界坐标处平滑地形</summary>
    public void SmoothHeightPoint(Vector3 worldPos, float radius, float strength)
    {
        var (cx, cz) = WorldToMapIndex(worldPos);
        int r = Mathf.CeilToInt(radius);
        var newHeights = new Dictionary<(int x, int z), float>();

        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                float dist = new Vector2(dx, dz).Length();
                if (dist > radius) continue;

                float sum = 0f;
                for (int sz = -1; sz <= 1; sz++)
                    for (int sx = -1; sx <= 1; sx++)
                        sum += GetHeightAt(cx + dx + sx, cz + dz + sz);
                float avg = sum / 9f;
                float w = Mathf.Clamp(1f - dist / radius, 0f, 1f) * strength;
                newHeights[(cx + dx, cz + dz)] = Mathf.Lerp(GetHeightAt(cx + dx, cz + dz), avg, w);
            }

        foreach (var kv in newHeights)
            SetHeightAt(kv.Key.x, kv.Key.z, kv.Value);

        RefreshTilesInRange(cx, cz, r);
        HeightChanged?.Invoke(worldPos, radius);
    }

    /// <summary>注：在世界坐标处平整地形（范围内顶点向射线点高度靠拢）</summary>
    public void FlattenHeightPoint(Vector3 worldPos, float radius, float strength)
    {
        var (cx, cz) = WorldToMapIndex(worldPos);
        int r = Mathf.CeilToInt(radius);
        float targetHeight = worldPos.Y;

        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                float dist = new Vector2(dx, dz).Length();
                if (dist > radius) continue;
                float w = Mathf.Clamp(1f - dist / radius, 0f, 1f) * strength;
                SetHeightAt(cx + dx, cz + dz, Mathf.Lerp(GetHeightAt(cx + dx, cz + dz), targetHeight, w));
            }

        RefreshTilesInRange(cx, cz, r);
        HeightChanged?.Invoke(worldPos, radius);
    }

    // ---------- 内部 ----------

    /// <summary>注：刷新笔刷范围内的所有 Tile 网格</summary>
    private void RefreshTilesInRange(int centerX, int centerZ, int radiusCells)
    {
        if (_grid == null) return;
        int minTx = Mathf.Max(0, (centerX - radiusCells) / TilePrecision);
        int maxTx = Mathf.Min(_tilesX - 1, (centerX + radiusCells) / TilePrecision);
        int minTz = Mathf.Max(0, (centerZ - radiusCells) / TilePrecision);
        int maxTz = Mathf.Min(_tilesZ - 1, (centerZ + radiusCells) / TilePrecision);

        for (int tz = minTz; tz <= maxTz; tz++)
            for (int tx = minTx; tx <= maxTx; tx++)
                _grid[tx, tz]?.GenMesh();
    }

    /// <summary>注：读取草原权重（基础权重 × 坡度因子）</summary>
    public float GetGrassWeight(int globalX, int globalZ)
    {
        float baseWeight = Data?.GetGrassWeight(globalX, globalZ) ?? 0f;
        if (baseWeight <= 0.01f) return 0f;

        float hL = GetHeightAt(globalX - 1, globalZ);
        float hR = GetHeightAt(globalX + 1, globalZ);
        float hD = GetHeightAt(globalX, globalZ - 1);
        float hU = GetHeightAt(globalX, globalZ + 1);
        var normal = new Vector3(hL - hR, 2f, hD - hU).Normalized();
        float slope = Mathf.RadToDeg(Mathf.Acos(normal.Y));
        float slopeFactor = 1f - Mathf.SmoothStep(25f, 40f, slope);
        return baseWeight * slopeFactor;
    }

    /// <summary>注：清空所有 Tile 和远景</summary>
    public void ClearTiles()
    {
        _grid = null;
        _tilesX = 0;
        _tilesZ = 0;
        _pendingUnload.Clear();
        foreach (var tile in GetChildren())
            if (tile is TerrainTile) tile.QueueFree();

        // 清除远景
        var distant = GetNodeOrNull("DistantTerrain");
        if (distant != null) distant.QueueFree();
    }

    // ---------- 流式加载 ----------

    public override void _Process(double delta)
    {
        UpdateStreaming();
    }

    /// <summary>注：流式加载更新——每帧检查相机位置，按距离排序逐帧加载视野内 Tile，延迟卸载视野外 Tile</summary>
    private void UpdateStreaming()
    {
        if (!StreamLoading || _grid == null || Data == null) return;

        // 编辑器模式取编辑器视口相机，运行时取游戏视口相机
        Camera3D cam;
        if (Engine.IsEditorHint())
            cam = EditorInterface.Singleton?.GetEditorViewport3D(0)?.GetCamera3D();
        else
            cam = GetViewport()?.GetCamera3D();
        if (cam == null) return;

        float half = (int)Data.RegionSize * 0.5f;
        Vector3 pos = cam.GlobalPosition;

        // 计算相机所在 Tile 坐标
        int camTileX = Mathf.FloorToInt((pos.X + half) / TilePrecision);
        int camTileZ = Mathf.FloorToInt((pos.Z + half) / TilePrecision);

        // 视野范围
        int minTx = Mathf.Max(0, camTileX - LoadRadius);
        int maxTx = Mathf.Min(_tilesX - 1, camTileX + LoadRadius);
        int minTz = Mathf.Max(0, camTileZ - LoadRadius);
        int maxTz = Mathf.Min(_tilesZ - 1, camTileZ + LoadRadius);

        // 1. 收集视野内未加载的 Tile，按距离相机远近排序（近的先加载）
        _loadQueue.Clear();
        for (int tz = minTz; tz <= maxTz; tz++)
            for (int tx = minTx; tx <= maxTx; tx++)
            {
                var tile = _grid[tx, tz];
                if (tile != null && tile.Mesh == null)
                    _loadQueue.Add(tile);
            }

        _loadQueue.Sort((a, b) =>
        {
            float distA = a.Position.DistanceTo(pos);
            float distB = b.Position.DistanceTo(pos);
            return distA.CompareTo(distB);
        });

        // 2. 每帧只加载 2 个 Tile，避免单帧卡顿
        int loadCount = Mathf.Min(2, _loadQueue.Count);
        for (int i = 0; i < loadCount; i++)
        {
            var tile = _loadQueue[i];
            tile.GenMesh();
            _pendingUnload.Remove((tile.GridX, tile.GridZ));
        }

        // 3. 标记视野外的 Tile 为待卸载
        double now = Time.GetUnixTimeFromSystem();
        for (int tz = 0; tz < _tilesZ; tz++)
            for (int tx = 0; tx < _tilesX; tx++)
            {
                if (tx >= minTx && tx <= maxTx && tz >= minTz && tz <= maxTz) continue;
                var tile = _grid[tx, tz];
                if (tile != null && tile.Mesh != null && !_pendingUnload.ContainsKey((tx, tz)))
                    _pendingUnload[(tx, tz)] = now;
            }

        // 4. 卸载超时的 Tile
        var toUnload = new List<(int x, int z)>();
        foreach (var kv in _pendingUnload)
        {
            if (now - kv.Value >= UnloadDelay)
                toUnload.Add(kv.Key);
        }
        foreach (var key in toUnload)
        {
            _grid[key.x, key.z]?.Unload();
            _pendingUnload.Remove(key);
        }
    }


    /// <summary>注：从已有的 RegionData 恢复地形（加载存档用），不再从噪声生成</summary>
    public void LoadFromData(RegionData data)
    {
        ClearTiles();
        Data = data;
        CreateTiles();

        // 流式加载：勾选后只创建 tile 节点不生成 mesh，由 UpdateStreaming 按需加载
        if (!StreamLoading)
        {
            for (int gz = 0; gz < _tilesZ; gz++)
                for (int gx = 0; gx < _tilesX; gx++)
                    _grid[gx, gz].GenMesh();
        }
        else
        {
            _pendingUnload.Clear();
        }

        // 加载存档时也要生成远景
        GenerateDistant();
    }

}

