using Godot;
using System.Collections.Generic;

public static class GeometryClipper
{
    /// <summary>
    /// 用 AABB 的六个面（作为平面）裁剪多边形，返回裁剪后位于 AABB 外侧的所有多边形（可能是多个）。
    /// 多边形顶点顺序为逆时针（或顺时针），裁剪后仍保持顺序。
    /// </summary>
    public static List<Vector3[]> ClipPolygonByAabb(Vector3[] polygon, Aabb aabb)
    {
        // 定义 AABB 的六个平面：法线朝内，即保留的是平面正侧（内侧），但我们想要外侧，所以需要反一下。
        // 为简化，我们定义平面为：左侧（x >= aabb.Position.X）、右侧（x <= aabb.End.X）、
        // 下（y >= aabb.Position.Y）、上（y <= aabb.End.Y）、后（z >= aabb.Position.Z）、前（z <= aabb.End.Z）。
        // 我们要丢弃位于 AABB 内的部分，即保留不在所有六个平面内侧的部分。
        // 但由于多边形可能部分在内部分在外，裁剪时通常用平面保留一侧，我们这里保留外侧，即平面判断：点在平面外侧则保留。

        // 平面列表：每个平面由法线（指向外侧）和 AABB 表面上的一个点定义。
        // 外侧平面：左面（x = min, 法线 -X），右面（x = max, 法线 +X），下面（y = min, 法线 -Y），上面（y = max, 法线 +Y），后面（z = min, 法线 -Z），前面（z = max, 法线 +Z）。
        var planes = new (Vector3 normal, float d)[]
        {
            (Vector3.Left,  aabb.Position.X),   // x >= min  ⇒  点积 Left·P >?  实际上平面方程：-x + min = 0, 保留区域：-x + min <= 0 即 x >= min
            (Vector3.Right, -aabb.End.X),       // x <= max  ⇒  x - max <= 0
            (Vector3.Down,  aabb.Position.Y),   // y >= min  ⇒ -y + min <= 0
            (Vector3.Up,   -aabb.End.Y),         // y <= max  ⇒ y - max <= 0
            (Vector3.Forward,  aabb.Position.Z), // z >= min  ⇒ -z + min <= 0
            (Vector3.Back, -aabb.End.Z)          // z <= max  ⇒ z - max <= 0
        };

        List<Vector3[]> result = new List<Vector3[]>();
        result.Add(polygon); // 初始一个多边形

        // 依次用每个平面切割
        foreach (var (normal, d) in planes)
        {
            List<Vector3[]> nextResult = new List<Vector3[]>();
            foreach (var poly in result)
            {
                var clipped = ClipPolygonByPlane(poly, normal, d);
                nextResult.AddRange(clipped);
            }
            result = nextResult;
        }

        return result;
    }

    /// <summary>
    /// 用平面裁剪单个多边形（Sutherland–Hodgman），返回裁剪后在外侧的多边形（可能0个、1个或多个，但SH算法返回0或1个多边形）。
    /// 保留区域：点满足 dot(normal, point) + d <= 0 ? 我们约定平面方程为 normal·P + d = 0，保留区域为 normal·P + d <= 0。
    /// 上面定义的平面：左面 normal=(-1,0,0), d=min，保留区域： -x + min <= 0 → x >= min，即外侧（大于最小值），正确。
    /// 所以我们要保留的点条件是：dot(normal, point) + d <= 0。
    /// </summary>
    private static List<Vector3[]> ClipPolygonByPlane(Vector3[] polygon, Vector3 normal, float d)
    {
        if (polygon.Length < 3) return new List<Vector3[]>(); // 无效多边形

        List<Vector3> output = new List<Vector3>();
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector3 current = polygon[i];
            Vector3 next = polygon[(i + 1) % polygon.Length];

            float currentDot = normal.Dot(current) + d;
            float nextDot = normal.Dot(next) + d;

            bool currentInside = currentDot <= 0; // 在保留侧
            bool nextInside = nextDot <= 0;

            if (currentInside)
            {
                // 当前点在保留侧，直接加入
                output.Add(current);
                if (!nextInside)
                {
                    // 离开保留区域，添加交点
                    Vector3 intersection = IntersectPlane(current, next, normal, d);
                    output.Add(intersection);
                }
            }
            else if (nextInside)
            {
                // 进入保留区域，添加交点然后下一个点会在后续添加
                Vector3 intersection = IntersectPlane(current, next, normal, d);
                output.Add(intersection);
            }
        }

        if (output.Count < 3)
            return new List<Vector3[]>(); // 裁剪后为线段或点，丢弃
        else
            return new List<Vector3[]> { output.ToArray() };
    }

    private static Vector3 IntersectPlane(Vector3 p1, Vector3 p2, Vector3 normal, float d)
    {
        // 计算线段与平面的交点，平面方程: normal·P + d = 0
        float t = -(normal.Dot(p1) + d) / normal.Dot(p2 - p1);
        return p1 + t * (p2 - p1);
    }
}