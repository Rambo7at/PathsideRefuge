using Godot;

namespace TerraEnvEditor.Data;

/// <summary>注：地形数据资源——统一高度图+材质权重图，可序列化保存/加载，跟显示节点解耦</summary>
[GlobalClass]
[Tool]
public partial class RegionData : Resource
{
    public enum E_RegionSize : uint
    {
        _128m = 128,
        _256m = 256,
        _512m = 512,
        _1024m = 1024,
        _2048m = 2048,
        _4096m = 4096,
        _8192m = 8192,
    }


    /// <summary>注：区域尺寸（米）</summary>
    [Export] public E_RegionSize RegionSize { get; set; } = E_RegionSize._1024m;

    /// <summary>注：高度缩放（高度图像素值 × 此值 = 世界高度）</summary>
    [Export] public float HeightScale { get; set; } = 10f;

    /// <summary>注：高度图（R 通道 = 高度值，浮点格式）</summary>
    [Export] public Image HeightImage { get; set; }

    /// <summary>注：材质权重图（R = 草原权重，G = 森林权重...）</summary>
    [Export] public Image SplatImage { get; set; }

    /// <summary>注：世界种子，控制所有程序化噪声，同一种子生成完全相同的地形</summary>
    [Export] public int WorldSeed { get; set; } = 12345;

    /// <summary>注：基础海拔偏移（米），所有高度的基准面</summary>
    [Export] public float BaseHeight { get; set; } = 0f;

    /// <summary>注：基础地形噪声频率，值越小地形越平缓、特征越大</summary>
    [Export] public float TerrainFrequency { get; set; } = 0.0015f;

    /// <summary>注：基础地形起伏高度（米），全图平缓起伏的幅度</summary>
    [Export] public float TerrainAmplitude { get; set; } = 10f;

    /// <summary>注：山脉噪声频率，值越小山脉越少越大</summary>
    [Export] public float MountainFrequency { get; set; } = 0.0008f;

    /// <summary>注：山脉高度（米），山脉区域的额外高度</summary>
    [Export] public float MountainAmplitude { get; set; } = 30f;

    /// <summary>注：山脉阈值（0~1），噪声高于此值的区域才叠加山脉</summary>
    [Export] public float MountainThreshold { get; set; } = 0.6f;

    /// <summary>注：域扭曲强度（米），用低频噪声偏移采样坐标，打破噪声规律性让地形更自然，0=关闭</summary>
    [Export] public float WarpStrength { get; set; } = 40f;

    /// <summary>注：细节层振幅（米），高频低振幅噪声添加地表微小起伏，0=关闭</summary>
    [Export] public float DetailAmplitude { get; set; } = 1.5f;

    /// <summary>注：海平面高度（米），低于此值的地形钳制为海平面，默认-100即不启用海洋</summary>
    [Export] public float OceanLevel { get; set; } = -100f;

    [ExportGroup("远景")]
    /// <summary>注：是否启用远景地形（低精度、只渲染、无碰撞，用于遮住地平线）</summary>
    [Export] public bool EnableDistant { get; set; } = false;

    /// <summary>注：远景向外延伸距离（米），从主地形边缘算起</summary>
    [Export] public float DistantSize { get; set; } = 500f;

    /// <summary>注：远景顶点间距（米），值越大精度越低性能越好</summary>
    [Export] public int DistantVertexSpacing { get; set; } = 4;

    /// <summary>注：远景高度缩放，让远景山脉更高大更有气势，1=与主地形一致</summary>
    [Export] public float DistantHeightScale { get; set; } = 1.2f;

    /// <summary>注：远景边缘是否渐隐到海平面，避免远景边缘突然断掉</summary>
    [Export] public bool DistantFade { get; set; } = true;




    /// <summary>注：高度图宽度（顶点数 = RegionSize+1）</summary>
    public int MapWidth => (int)RegionSize + 1;

    /// <summary>注：高度图高度（顶点数 = RegionSize+1）</summary>
    public int MapHeight => (int)RegionSize + 1;




    /// <summary>注：初始化空白地形（从噪声图采样后调用）</summary>
    public void Init()
    {
        HeightImage = Image.CreateEmpty(MapWidth, MapHeight, false, Image.Format.Rf);
        SplatImage = Image.CreateEmpty(MapWidth, MapHeight, false, Image.Format.Rgba8);
        SplatImage.Fill(new Color(1, 0, 0, 1));  // 默认全覆盖草原
    }

    /// <summary>注：从噪声图采样填充高度图</summary>
    public void GenerateFromNoise(NoiseTexture2D noise)
    {
        Init();
        var noiseImg = noise.GetImage();
        int noiseW = noiseImg.GetWidth() - 1;
        int noiseH = noiseImg.GetHeight() - 1;

        for (int z = 0; z < MapHeight; z++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                float u = x / (float)RegionSize;
                float v = z / (float)RegionSize;
                int nx = Mathf.Clamp((int)(u * noiseW), 0, noiseW);
                int ny = Mathf.Clamp((int)((1f - v) * noiseH), 0, noiseH);
                float h = noiseImg.GetPixel(nx, ny).R;
                HeightImage.SetPixel(x, z, new Color(h, 0, 0));
            }
        }
    }

    /// <summary>注：噪声对象缓存，主地形和远景共用同一套噪声保证边缘衔接</summary>
    private FastNoiseLite _terrainNoise;
    private FastNoiseLite _mountainNoise;
    private FastNoiseLite _warpNoise;
    private FastNoiseLite _detailNoise;

    /// <summary>注：初始化所有噪声对象（主地形和远景共用，保证边缘衔接）</summary>
    private void InitNoises()
    {
        _terrainNoise = new FastNoiseLite();
        _terrainNoise.Seed = WorldSeed;
        _terrainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        _terrainNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        _terrainNoise.FractalOctaves = 3;
        _terrainNoise.FractalLacunarity = 2.0f;
        _terrainNoise.FractalGain = 0.5f;
        _terrainNoise.Frequency = TerrainFrequency;

        _mountainNoise = new FastNoiseLite();
        _mountainNoise.Seed = WorldSeed + 1000;
        _mountainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        _mountainNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
        _mountainNoise.FractalOctaves = 2;
        _mountainNoise.FractalLacunarity = 2.0f;
        _mountainNoise.FractalGain = 0.5f;
        _mountainNoise.Frequency = MountainFrequency;

        _warpNoise = WarpStrength > 0f ? new FastNoiseLite() : null;
        if (_warpNoise != null)
        {
            _warpNoise.Seed = WorldSeed + 2000;
            _warpNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
            _warpNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _warpNoise.FractalOctaves = 2;
            _warpNoise.FractalLacunarity = 2.0f;
            _warpNoise.FractalGain = 0.5f;
            _warpNoise.Frequency = TerrainFrequency * 0.3f;
        }

        _detailNoise = DetailAmplitude > 0f ? new FastNoiseLite() : null;
        if (_detailNoise != null)
        {
            _detailNoise.Seed = WorldSeed + 3000;
            _detailNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
            _detailNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _detailNoise.FractalOctaves = 3;
            _detailNoise.FractalLacunarity = 2.0f;
            _detailNoise.FractalGain = 0.5f;
            _detailNoise.Frequency = 0.05f;
        }
    }

    /// <summary>注：采样指定世界坐标的地形高度（包含域扭曲、基础地形、山脉、细节层），主地形和远景共用保证边缘衔接</summary>
    private float SampleHeight(float wx, float wz)
    {
        // Domain Warping：用扭曲噪声偏移采样坐标
        float sampleX = wx;
        float sampleZ = wz;
        if (_warpNoise != null)
        {
            sampleX += _warpNoise.GetNoise2D(wx, wz) * WarpStrength;
            sampleZ += _warpNoise.GetNoise2D(wx + 100f, wz + 100f) * WarpStrength;
        }

        // 第一层：基础地形（平缓起伏）
        float tNoise = (_terrainNoise.GetNoise2D(sampleX, sampleZ) + 1f) * 0.5f;
        float height = BaseHeight + tNoise * TerrainAmplitude;

        // 第二层：山脉（仅噪声高于阈值时叠加）
        float mNoise = (_mountainNoise.GetNoise2D(sampleX, sampleZ) + 1f) * 0.5f;
        if (mNoise > MountainThreshold)
        {
            float mountainFactor = Mathf.SmoothStep(MountainThreshold, 1f, mNoise);
            height += mountainFactor * MountainAmplitude;
        }

        // 细节层（用原始坐标不扭曲）
        if (_detailNoise != null)
            height += _detailNoise.GetNoise2D(wx, wz) * DetailAmplitude;

        return height;
    }

    /// <summary>注：程序化生成地形——两层噪声叠加 + Domain Warping + 细节层，同一种子结果确定</summary>
    public void GenerateProcedural()
    {
        Init();
        InitNoises();

        float half = (float)RegionSize * 0.5f;

        for (int z = 0; z < MapHeight; z++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                float wx = x - half;
                float wz = z - half;
                float height = SampleHeight(wx, wz);

                if (height < OceanLevel) height = OceanLevel;

                HeightImage.SetPixel(x, z, new Color(height / HeightScale, 0, 0));
            }
        }
    }

    /// <summary>注：生成远景 Mesh（低精度、只渲染、无碰撞），只生成主地形外围的环形区域，用于遮住地平线</summary>
    public ArrayMesh GenerateDistantMesh()
    {
        if (!EnableDistant) return null;
        InitNoises();

        float half = (float)RegionSize * 0.5f;
        float farHalf = half + DistantSize;
        int spacing = Mathf.Max(1, DistantVertexSpacing);

        // 远景网格覆盖从 -farHalf 到 farHalf 的完整方形区域
        int vertsPerSide = Mathf.CeilToInt(farHalf * 2f / spacing) + 1;
        int totalVerts = vertsPerSide * vertsPerSide;

        var vertices = new Vector3[totalVerts];
        var uvs = new Vector2[totalVerts];
        var normals = new Vector3[totalVerts];

        for (int z = 0; z < vertsPerSide; z++)
        {
            for (int x = 0; x < vertsPerSide; x++)
            {
                float wx = -farHalf + x * spacing;
                float wz = -farHalf + z * spacing;
                int idx = z * vertsPerSide + x;

                // 判断是否是内边缘顶点（距离主地形边缘在一个间距内）
                bool isInnerEdge = Mathf.Abs(Mathf.Abs(wx) - half) < spacing * 0.6f ||
                                   Mathf.Abs(Mathf.Abs(wz) - half) < spacing * 0.6f;

                float height;
                if (isInnerEdge && HeightImage != null)
                {
                    // 内边缘顶点：从主地形高度图直接采样，确保跟主地形完全一致
                    int gx = (int)Mathf.Round(wx + half);
                    int gz = (int)Mathf.Round(wz + half);
                    if (gx >= 0 && gx < MapWidth && gz >= 0 && gz < MapHeight)
                        height = GetHeightAt(gx, gz);
                    else
                        height = SampleHeight(wx, wz);
                }
                else
                {
                    // 外部顶点：用噪声采样
                    height = SampleHeight(wx, wz);
                }

                float distFromCenter = Mathf.Sqrt(wx * wx + wz * wz);

                // 远景高度缩放——从远景外部30%处开始，内边缘跟主地形完全一致
                float scaleStart = half + DistantSize * 0.3f;
                float scaleFactor = Mathf.SmoothStep(scaleStart, farHalf, distFromCenter);
                height = Mathf.Lerp(height, height * DistantHeightScale, scaleFactor);

                // 边缘渐隐到海平面——从远景中间50%处开始，避免主地形边缘就降低
                if (DistantFade)
                {
                    float fadeStart = half + DistantSize * 0.5f;
                    float fade = Mathf.SmoothStep(fadeStart, farHalf, distFromCenter);
                    height = Mathf.Lerp(height, OceanLevel, fade * 0.9f);
                }

                vertices[idx] = new Vector3(wx, height, wz);
                uvs[idx] = new Vector2((float)x / vertsPerSide, (float)z / vertsPerSide);

                // 法线用中心差分计算（跟主地形同一种算法），采样附近1m的4个点
                // 内边缘顶点也参考主地形方向的相邻点，确保法线连续
                float hL = SampleHeight(wx - 1f, wz);
                float hR = SampleHeight(wx + 1f, wz);
                float hD = SampleHeight(wx, wz - 1f);
                float hU = SampleHeight(wx, wz + 1f);
                normals[idx] = new Vector3(hL - hR, 2f, hD - hU).Normalized();
            }
        }

        // 先统计环形区域（主地形以外）的四边形数量
        int quadCount = 0;
        for (int z = 0; z < vertsPerSide - 1; z++)
            for (int x = 0; x < vertsPerSide - 1; x++)
            {
                float cx = -farHalf + (x + 0.5f) * spacing;
                float cz = -farHalf + (z + 0.5f) * spacing;
                if (Mathf.Abs(cx) < half && Mathf.Abs(cz) < half) continue;
                quadCount++;
            }

        // 生成三角形索引——只包含环形区域的四边形，避免覆盖主地形
        var indices = new int[quadCount * 6];
        int idxPtr = 0;
        for (int z = 0; z < vertsPerSide - 1; z++)
        {
            for (int x = 0; x < vertsPerSide - 1; x++)
            {
                float cx = -farHalf + (x + 0.5f) * spacing;
                float cz = -farHalf + (z + 0.5f) * spacing;
                if (Mathf.Abs(cx) < half && Mathf.Abs(cz) < half) continue;

                int i = z * vertsPerSide + x;
                // 逆时针顶点顺序（Godot 默认逆时针为正面，法线朝上）
                indices[idxPtr++] = i;
                indices[idxPtr++] = i + 1;
                indices[idxPtr++] = i + vertsPerSide;
                indices[idxPtr++] = i + 1;
                indices[idxPtr++] = i + vertsPerSide + 1;
                indices[idxPtr++] = i + vertsPerSide;
            }
        }

        // 用 SurfaceTool 创建 Mesh（支持 GenerateTangents，地形材质需要切线）
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < totalVerts; i++)
        {
            st.SetNormal(normals[i]);
            st.SetUV(uvs[i]);
            st.AddVertex(vertices[i]);
        }
        for (int i = 0; i < indices.Length; i++)
            st.AddIndex(indices[i]);
        st.GenerateTangents();
        GD.Print($"[Profiler] 远景网格: {vertsPerSide}×{vertsPerSide}={totalVerts}顶点, {quadCount}四边形={quadCount * 2}三角形");
        return st.Commit();
    }

    /// <summary>注：读取指定顶点的世界高度</summary>
    public float GetHeightAt(int x, int z)
    {
        if (HeightImage == null || !IsValid(x, z)) return 0f;
        return HeightImage.GetPixel(x, z).R * HeightScale;
    }

    /// <summary>注：写入指定顶点的世界高度</summary>
    public void SetHeightAt(int x, int z, float worldHeight)
    {
        if (HeightImage == null || !IsValid(x, z)) return;
        HeightImage.SetPixel(x, z, new Color(worldHeight / HeightScale, 0, 0));
    }

    /// <summary>注：读取草原权重（R 通道 0~1）</summary>
    public float GetGrassWeight(int x, int z)
    {
        if (SplatImage == null || !IsValid(x, z)) return 0f;
        return SplatImage.GetPixel(x, z).R;
    }

    /// <summary>注：设置草原权重（0=不长草，1=全覆盖）</summary>
    public void SetGrassWeight(int x, int z, float weight)
    {
        if (SplatImage == null || !IsValid(x, z)) return;
        var c = SplatImage.GetPixel(x, z);
        c.R = weight;
        SplatImage.SetPixel(x, z, c);
    }

    private bool IsValid(int x, int z) => x >= 0 && z >= 0 && x < MapWidth && z < MapHeight;
}


