using Godot;
using System;

public partial class NpcBase : CharacterBody3D
{
    /// <summary>NPC移动速度</summary>
    [Export] public float Speed = 5.0f;
    /// <summary>巡逻半径（围绕初始位置的最大巡逻距离）</summary>
    [Export] public float PatrolRadius = 10.0f;
    /// <summary>到达目标点后的停留时间（秒）</summary>
    [Export] public float StopTime = 2.0f;
    /// <summary>判定到达目标点的距离阈值</summary>
    [Export] public float TargetDesiredDistance = 1.0f;
    /// <summary>导航代理组件（用于寻路和避障）</summary>
    [Export] public NavigationAgent3D navigationAgent3D;

    /// <summary>NPC初始位置（落地后记录，作为巡逻圆心）</summary>
    private Vector3 _originPosition;
    /// <summary>当前巡逻目标点</summary>
    private Vector3 _targetPosition;
    /// <summary>停留计时（累计到达目标点后的停留时间）</summary>
    private float _stopTimer = 0.0f;
    /// <summary>是否处于停留状态</summary>
    private bool _isStopping = false;
    /// <summary>是否为第一次移动/停留结束后首次移动</summary>
    private bool _isFirstMove = true;
    /// <summary>导航组件是否就绪（防崩溃延迟初始化标记）</summary>
    private bool _isNavigationReady = false;

    public override void _Ready()
    {
        // 导航代理未绑定则直接返回
        if (navigationAgent3D == null) return;

        // 导航代理基础配置
        navigationAgent3D.TargetDesiredDistance = TargetDesiredDistance;
        navigationAgent3D.Radius = 0.5f;
        navigationAgent3D.Height = 1.8f;

        // 关键：延迟1帧标记导航就绪（避开Ready阶段的路径计算，防止崩溃）
        CallDeferred(nameof(MarkNavigationReady));
    }

    /// <summary>延迟标记导航就绪（核心防崩溃逻辑）</summary>
    private void MarkNavigationReady()
    {
        _isNavigationReady = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleNpcMovement(delta);
    }

    /// <summary>注：NPC核心移动逻辑总入口</summary>
    /// <param name="delta">帧时间</param>
    private void HandleNpcMovement(double delta)
    {
        // 1. 下落逻辑（优先处理重力）
        if (!IsOnFloor())
        {
            Vector3 newVelocity = Velocity;
            newVelocity.Y -= 9.8f * (float)delta; // 固定重力值，避免GetGravity()潜在问题
            Velocity = newVelocity;
            MoveAndSlide();
            return;
        }

        // 2. 落地后初始化：记录初始位置作为巡逻圆心
        if (_originPosition == default)
        {
            _originPosition = GlobalPosition;
            return; // 本帧只记录位置，不执行后续逻辑（防帧内逻辑过载）
        }

        // 3. 停留逻辑：到达目标点后停留指定时间
        if (_isStopping)
        {
            _stopTimer += (float)delta;
            Velocity = Vector3.Zero; // 停止移动
            MoveAndSlide();

            // 停留时间达到阈值后重置状态，准备生成新目标
            if (_stopTimer >= StopTime)
            {
                _isStopping = false;
                _stopTimer = 0.0f;
                _isFirstMove = true; // 标记为首次移动，触发新目标生成
            }
            return;
        }

        // 4. 防崩溃：导航未就绪/组件未绑定则跳过本帧
        if (!_isNavigationReady || navigationAgent3D == null) return;

        // 5. 生成目标点：首次移动/停留结束后生成新的巡逻目标
        if (_isFirstMove)
        {
            GenerateNewTarget();
            // 目标点无效则直接返回
            if (_targetPosition == default) return;
            _isFirstMove = false;
            return; // 生成目标后本帧不移动，给导航组件1帧时间计算路径
        }

        // 6. 核心移动逻辑（异常捕获防止闪退）
        try
        {
            // 计算当前位置到目标点的距离
            float distanceToTarget = GlobalPosition.DistanceTo(_targetPosition);

            // 到达目标点判定：距离小于阈值则进入停留状态
            if (distanceToTarget <= TargetDesiredDistance)
            {
                _isStopping = true;
                Velocity = Vector3.Zero;
                MoveAndSlide();
                return;
            }

            // 计算朝向目标点的移动方向（仅X/Z平面移动，忽略Y轴）
            Vector3 direction = (_targetPosition - GlobalPosition).Normalized();
            direction.Y = 0;

            // 设置移动速度并执行移动
            Vector3 moveVelocity = direction * Speed;
            moveVelocity.Y = -0.1f; // 轻微向下的速度，保证贴地
            Velocity = moveVelocity;
            MoveAndSlide();
        }
        catch (Exception)
        {
            // 异常后重置状态，避免NPC卡死
            _isFirstMove = true;
        }
    }

    /// <summary>注：生成随机巡逻目标点（围绕初始位置）</summary>
    private void GenerateNewTarget()
    {
        // 随机种子初始化，保证随机值不重复
        GD.Randomize();

        // 随机生成巡逻角度（0~2π）和距离（1~PatrolRadius）
        float randomAngle = (float)GD.RandRange(0, Math.PI * 2);
        float randomDistance = (float)GD.RandRange(1, PatrolRadius);

        // 计算目标点坐标（基于初始位置的极坐标转换）
        float targetX = _originPosition.X + Mathf.Cos(randomAngle) * randomDistance;
        float targetZ = _originPosition.Z + Mathf.Sin(randomAngle) * randomDistance;
        _targetPosition = new Vector3(targetX, _originPosition.Y, targetZ);
    }
}