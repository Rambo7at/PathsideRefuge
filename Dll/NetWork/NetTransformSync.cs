using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.NetWork
{

    /// <summary>
    /// 注：网络 Transform 同步组件，依赖 NetSyncBase。
    /// Owner 端写入 NetObj 并递增版本号；非 Owner 端从 NetObj 读取并插值平滑跟随。
    /// </summary>
    public partial class NetTransformSync : Node
    {
        /// <summary> 注：同步目标节点，默认是父节点 </summary>
        [Export] public Node3D Target { get; set; }

        /// <summary> 注：是否同步旋转 </summary>
        [Export] public bool SyncRotation { get; set; } = true;

        /// <summary> 注：非 Owner 端的插值速度（值越大越快到目标） </summary>
        [Export] public float InterpolationSpeed { get; set; } = 10f;

        /// <summary> 注：Owner 端更新 NetObj 的最低时间间隔（秒），避免每帧写入 </summary>
        [Export] public float MinUpdateInterval { get; set; } = 0.05f; // 20Hz

        private NetSyncBase _syncBase;
        private Vector3 _lastSentPosition;
        private Quaternion _lastSentRotation;
        private float _updateTimer;

        public override void _Ready()
        {
            // 默认目标节点是父节点，也可以手动指定
            Target ??= GetParent<Node3D>();
            _syncBase = GetParent<NetSyncBase>();

            // 初始化记录值
            if (Target != null)
            {
                _lastSentPosition = Target.Position;
                _lastSentRotation = Target.Quaternion;
            }
        }

        public override void _Process(double delta)
        {
            if (_syncBase?.NetObj == null || Target == null)
                return;

            if (_syncBase.IsOwner)
            {
                // Owner 端：检查变化并写入 NetObj
                OwnerUpdate((float)delta);
            }
            else
            {
                // 非 Owner 端：从 NetObj 读取并插值
                RemoteUpdate((float)delta);
            }
        }

        /// <summary> 注：Owner 端逻辑，检测 Transform 变化并更新 NetObj </summary>
        private void OwnerUpdate(float delta)
        {
            _updateTimer += delta;
            if (_updateTimer < MinUpdateInterval)
                return;
            _updateTimer = 0f;

            bool changed = false;
            Vector3 currentPos = Target.Position;
            Quaternion currentRot = Target.Quaternion;

            if (currentPos != _lastSentPosition)
            {
                _syncBase.NetObj.Position = currentPos;
                _lastSentPosition = currentPos;
                changed = true;
            }

            if (SyncRotation && currentRot != _lastSentRotation)
            {
                _syncBase.NetObj.Rotation = currentRot;
                _lastSentRotation = currentRot;
                changed = true;
            }

            if (changed)
            {
                _syncBase.NetObj.IncreaseDataRevision();
            }
        }

        /// <summary> 注：非 Owner 端逻辑，从 NetObj 读取目标并插值平滑移动 </summary>
        private void RemoteUpdate(float delta)
        {
            Vector3 targetPos = _syncBase.NetObj.Position;
            Target.Position = Target.Position.Lerp(targetPos, delta * InterpolationSpeed);

            if (SyncRotation)
            {
                Quaternion targetRot = _syncBase.NetObj.Rotation;
                Target.Quaternion = Target.Quaternion.Slerp(targetRot, delta * InterpolationSpeed);
            }
        }
    }
}
