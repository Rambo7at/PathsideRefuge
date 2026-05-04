using Godot;
using System.Collections.Generic;
using 维修公司.Utils;

namespace 维修公司.Dll
{
    /// <summary>建筑系统</summary>
    public partial class BuildComp : Node3D
    {
        private MeshInstance3D m_Piece;

        private Marker3D m_Marker3D;

        private Camera3D m_Camera3D;

        private float m_placeDistance = 5f;      // 放置距离：原来10 → 现在5（拉近一倍）
        private float m_verticalOffset = 0.5f;    // 向上偏移，解决方块陷地里
        private int m_obstacleMask = 1 | 2 | 4;  // 障碍物层：地面+墙体+建筑（你按自己层改数字）

        // 移除[Export]，字段名改为m_开头（匹配你的命名习惯）
        private int m_groundCollisionMask = 1; // 地面碰撞掩码（需和地面的CollisionLayer匹配）
        private float m_gridStep = 0.25f; // 网格吸附步长（控制对齐精度）
        private float m_downRayLength = 20f; // 从Marker3D垂直向下的射线长度



        public override void _Ready()
        {

        }

        public override void _Process(double delta)
        {
            UpdatePreviewPiecePosition();
        }

        private void UpdatePreviewPiecePosition()
        {
            // 1. 基础校验
            if (m_Piece == null || m_Marker3D == null || m_Camera3D == null || !IsInsideTree())
            {
                return;
            }

            // 2. 取相机水平前方向（跟你人物移动同一套逻辑）
            Vector3 cameraForward = -m_Camera3D.GlobalTransform.Basis.Z;
            cameraForward.Y = 0;
            cameraForward = cameraForward.Normalized();

            // 3. 目标点：玩家前方 m_placeDistance 米（现在=5米，拉近一倍）
            Vector3 basePos = m_Marker3D.GlobalPosition;
            Vector3 targetHorizontal = basePos + cameraForward * m_placeDistance;

            // 4. 从目标点垂直向下找地面
            Viewport viewport = GetViewport();
            PhysicsDirectSpaceState3D spaceState = viewport.GetWorld3D().DirectSpaceState;

            PhysicsRayQueryParameters3D rayParams = PhysicsRayQueryParameters3D.Create(
                targetHorizontal,
                targetHorizontal - Vector3.Up * m_downRayLength
            );
            rayParams.CollisionMask = (uint)m_groundCollisionMask;

            // 排除自己
            var exclude = new Godot.Collections.Array<Rid>();
            StaticBody3D body = m_Piece.GetNodeOrNull<StaticBody3D>("StaticBody3D");
            if (body != null) exclude.Add(body.GetRid());
            rayParams.Exclude = exclude;

            var rayResult = spaceState.IntersectRay(rayParams);
            if (rayResult.Count == 0)
            {
                m_Piece.Visible = false;
                return;
            }

            // 5. 地面点 + 网格吸附
            Vector3 groundPos = (Vector3)rayResult["position"];
            Vector3 snapped = new Vector3(
                Mathf.Round(groundPos.X / m_gridStep) * m_gridStep,
                Mathf.Round(groundPos.Y / m_gridStep) * m_gridStep,
                Mathf.Round(groundPos.Z / m_gridStep) * m_gridStep
            );

            // 6. 向上偏移，解决「一半在地里」
            snapped.Y += m_verticalOffset;

            // ====================== 防穿墙核心 ======================
            CollisionShape3D colShape = m_Piece.GetNodeOrNull<CollisionShape3D>("StaticBody3D/CollisionShape3D");
            bool canPlace = true;

            if (colShape != null && colShape.Shape != null)
            {
                var shapeQuery = new PhysicsShapeQueryParameters3D();
                shapeQuery.Shape = colShape.Shape;
                shapeQuery.Transform = new Transform3D(Basis.Identity, snapped);
                shapeQuery.CollisionMask = (uint)m_obstacleMask;
                shapeQuery.Exclude = exclude;

                // 检测是否跟墙体/其他物体重叠
                var hitResult = spaceState.IntersectShape(shapeQuery, 1);
                canPlace = hitResult.Count == 0;
            }

            // 穿墙就隐藏 / 不显示
            m_Piece.Visible = canPlace;
            // ======================================================

            m_Piece.GlobalPosition = snapped;
        }







        /// <summary>
        /// 注：初始化方法，对外部开放
        /// </summary>
        /// <param name="camera3D"></param>
        /// <param name="piceName"></param>
        /// <param name="player"></param>
        public void InitBuildPiece(Marker3D marker3D, Camera3D camera3D, string piceName) // 新增Camera3D参数
        {
            if (!IsInsideTree())
            {
                GD.PrintErr("[BuildSystem] InitBuildPiece：自身不在场景树，无法设置预览件位置！");
                return;
            }
            if (m_Piece != null)
            {
                m_Piece.QueueFree();
                m_Piece = null;
            }

            //m_Piece = PieceManager.Instance.GetPiece(piceName);
            if (m_Piece == null) return;
            m_Marker3D = marker3D;
            m_Camera3D = camera3D; // 新增：保存摄像头引用
            //ToolUtils.GetNodeScript<PieceData>(m_Piece).CollisionShape3D.Disabled = true;

            AddChild(m_Piece);
            GD.Print($"[BuildSystem] 预览件初始位置：{m_Piece.GlobalPosition}");
        }






        /// <summary>工具方法：将piece设置成透明 </summary>
        /// <param name="materials"></param>
        /// <param name="alpha"></param>
        private void SetPieceAlpha(List<Material> materials, float alpha = 0.2f)
        {
            if (materials == null || materials.Count == 0) return;

            foreach (var mat in materials)
            {
                if (mat is not StandardMaterial3D standardMat) continue;

                standardMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                Color originalColor = standardMat.AlbedoColor;
                standardMat.AlbedoColor = new Color(originalColor.R, originalColor.G, originalColor.B, alpha);

                standardMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            }
        }

        /// <summary>工具方法：获取piece身上的全部mat </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        private List<Material> GetMeshMat(MeshInstance3D piece)
        {
            int matCount = piece.GetSurfaceOverrideMaterialCount();
            if (matCount == 0) return null;

            List<Material> materials = new List<Material>();

            for (int i = 0; i < matCount; i++)
            {
                Material material = piece.GetSurfaceOverrideMaterial(i);
                if (material == null) continue;
                materials.Add(material);
            }

            return materials;
        }
    }
}