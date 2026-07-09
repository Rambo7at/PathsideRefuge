using Godot;
using System;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp
{
	[GlobalClass]
	public partial class CreatureAnimComp : AnimationPlayer
	{

		// ── 攻击相关 ──
		public Action OnEnableHitbox;
		public Action OnDisableHitbox;
		public Action OnCombo;
        public Action OnEndCombo;

        // ── 状态机相关 ──
        public Action OnEndAttack;
		public Action OnEndStagger;
		public Action OnEndDeath;

		// ── 预） ──
		public Action OnFootstep;       // 脚步声
		public Action OnLand;           // 落地

		private void EnableHitbox() => OnEnableHitbox?.Invoke();
		private void Combo() => OnCombo?.Invoke();

        private void EndCombo() => OnEndCombo?.Invoke();
        private void DisableHitbox() => OnDisableHitbox?.Invoke();
		private void EndAttack() => OnEndAttack?.Invoke();
		private void EndStagger() => OnEndStagger?.Invoke();
		private void EndDeath() => OnEndDeath?.Invoke();
		private void Footstep() => OnFootstep?.Invoke();
		private void Land() => OnLand?.Invoke();

	}
}
