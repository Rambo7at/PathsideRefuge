using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Creature.Npc
{
    [GlobalClass]
    public partial class NpcStateMachine : Node, ISyncStateMachine
    {
        public enum NpcState
        {
            Patrol = 0, // 巡逻
            Chase = 1,  // 追击
        }

        public enum NpcAnimState
        {
            Idle = 0,
            Walk = 1,
            Run = 2,
            Jump = 3,
            Fall = 4,
        }

        public NpcAnimState m_npcAnimState { get; set; } = NpcAnimState.Idle;
        public NpcState m_npcState { get; set; } = NpcState.Patrol;

        public bool Walk => m_npcAnimState == NpcAnimState.Walk;
        public bool Idle => m_npcAnimState == NpcAnimState.Idle;

        private Npc _npc;


        public override void _Ready()
        {
            if (NetCore.Instance.IsClient) SetPhysicsProcess(false);

            var node = GetParent();

            if (node is not Npc npcComp)
            {
                CatLog.Err("[NpcStateMachine._Ready]：挂载的不是 npc 对象 已卸载");
                CatUtils.StopAndExit(this);
                return;
            }
            _npc = npcComp;
        }


        public override void _PhysicsProcess(double delta) => UpdateState();


        /// <summary> 注：移动状态 </summary>
        private void UpdateState()
        {
            if (_npc.IsOnFloor())
            {
                Vector3 horizontalVel = new(_npc.Velocity.X, 0, _npc.Velocity.Z);
                float speed = horizontalVel.Length();
                if (speed > 0.1f)
                    SwitchMoveState(NpcAnimState.Walk);
                else
                    SwitchMoveState(NpcAnimState.Idle);
            }
            else
            {
                // 如果意外离地（掉落），自动切 Fall（不主动跳跃）
                SwitchMoveState(_npc.Velocity.Y > 0 ? NpcAnimState.Jump : NpcAnimState.Fall);
            }
        }

        /// <summary> 注：切换移动状态 </summary>
        private void SwitchMoveState(NpcAnimState newState)
        {
            if (m_npcAnimState == newState) return;
            m_npcAnimState = newState;
            // CatLog.Ok($"[Npc] MoveState → {newState}");
        }

        public int GetState() => (int)m_npcState;

        public int GetAnimState() => (int)m_npcAnimState;
        public void SetState(int State) => m_npcState = (NpcState)State;

        public void SetAnimState(int State) => m_npcAnimState = (NpcAnimState)State;
    }
}
