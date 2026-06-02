using Godot;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

[GlobalClass]
public partial class PlayerStateMachine : Node, ISyncStateMachine
{
    public enum PlayerState
    {
        Idle = 0,
        Interact = 1,   
        Build = 2,      
    }

    public enum PlayerAnimState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Jump = 3,
        Fall = 4,
        Attack = 5,
        AttackFinished = 6
    }

    private Player m_player;

    public PlayerAnimState m_playerAnimState { get; set; } = PlayerAnimState.Idle;
    public PlayerState m_playerState { get; set; } = PlayerState.Idle;
    public bool Walk => m_playerAnimState == PlayerAnimState.Walk;
    public bool Jump => m_playerAnimState == PlayerAnimState.Jump;
    public bool Idle => m_playerAnimState == PlayerAnimState.Idle;
    public bool Attack => m_playerAnimState == PlayerAnimState.Attack;
    public bool AttackFinished => m_playerAnimState == PlayerAnimState.AttackFinished;


    public override void _Ready()
    {
        var node = GetParent();

        if (node == null)
        {
            CatLog.Err($"[PlayerStateMachine._Ready]：检测挂载对象是空，已返回");
            CatUtils.StopAndExit(this);
            return;
        }

        if (node is not Player pl)
        {
            CatLog.Err($"[PlayerController._Ready]：检测挂载对象并非 player ，已返回");
            CatUtils.StopAndExit(this);
            return;
        }

        m_player = pl;

        if (m_player.m_PlayerData == null)
        {
            SetProcess(false);
            SetPhysicsProcess(false);
            return;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (m_player.m_PlayerData == null) return;

        UpdatePhysicsBasedState();
    }

    /// <summary> 切换移动状态，状态不变则不执行 </summary>
    public void SwitchState(PlayerAnimState newState)
    {
        if (m_playerAnimState == newState) return;

        m_playerAnimState = newState;
        //CatLog.Ok($"[State] Changed to: {newState}");
    }

    /// <summary> 根据玩家速度和是否在地面自动切换物理状态 </summary>
    private void UpdatePhysicsBasedState()
    {
        // 攻击状态下不允许物理状态切换（保持攻击动画）
        if (m_playerAnimState == PlayerAnimState.Attack)
            return;

        if (Input.IsActionJustPressed("cat_Attack"))
        {
            SwitchState(PlayerAnimState.Attack);
            return;
        }

        if (m_player.IsOnFloor())
        {
            Vector3 horizontalVel = new(m_player.Velocity.X, 0, m_player.Velocity.Z);
            float speed = horizontalVel.Length();

            if (speed > 0.1f)
                SwitchState(PlayerAnimState.Walk);
            else
                SwitchState(PlayerAnimState.Idle);
        }
        else
        {
            SwitchState(m_player.Velocity.Y > 0 ? PlayerAnimState.Jump : PlayerAnimState.Fall);
        }
    }

    /// <summary> 动画调用，结束攻击 </summary>
    public void EndAttack()
    {
        if (m_playerAnimState != PlayerAnimState.Attack) return;

        Vector3 horizontalVel = new(m_player.Velocity.X, 0, m_player.Velocity.Z);
        PlayerAnimState nextState = horizontalVel.Length() > 0.1f ? PlayerAnimState.Walk : PlayerAnimState.Idle;

        SwitchState(nextState);
    }

    public int GetState() => (int)m_playerState;

    public int GetAnimState() => (int)m_playerAnimState;

    public void SetState(int State) => m_playerState = (PlayerState)State;

    public void SetAnimState(int State) => m_playerAnimState = (PlayerAnimState)State;
}