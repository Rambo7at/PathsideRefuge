using Godot;
using System;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp;

[GlobalClass]
public partial class CreatureAnimComp : AnimationPlayer
{
    // ── 攻击相关 ──
    public event Action OnMainHandHitEnable;    // 主手攻击判定开启
    public event Action OnMainHandHitDisable;   // 主手攻击判定关闭
    public event Action OnOffHandHitEnable;     // 副手攻击判定开启
    public event Action OnOffHandHitDisable;    // 副手攻击判定关闭
    public event Action OnCombo;
    public event Action OnEndCombo;

    // ── 状态机相关 ──
    public event Action OnEndAttack;
    public event Action OnEndStagger;
    public event Action OnEndDeath;

    // ── 准备 ──
    public event Action OnFootstep;

    // ── 动画事件回调 ──
    private void MainHandHitEnable() => OnMainHandHitEnable?.Invoke();
    private void MainHandHitDisable() => OnMainHandHitDisable?.Invoke();
    private void OffHandHitEnable() => OnOffHandHitEnable?.Invoke();
    private void OffHandHitDisable() => OnOffHandHitDisable?.Invoke();
    private void Combo() => OnCombo?.Invoke();
    private void EndCombo() => OnEndCombo?.Invoke();
    private void EndAttack() => OnEndAttack?.Invoke();
    private void EndStagger() => OnEndStagger?.Invoke();
    private void EndDeath() => OnEndDeath?.Invoke();
    private void Footstep() => OnFootstep?.Invoke();
}
