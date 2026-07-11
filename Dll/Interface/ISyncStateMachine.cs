using System;
using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Interface
{
    public interface ISyncStateMachine
    {
        event Action OnAnimStateChanged;

        event Action<int> OnAttackAnimIndexChanged;

        event Action OnComboRequested;

        event Action OnOneShotChanged;
        int GetState();

        int GetAnimState();

        void SetState(int State);

        void SetAnimState(int State);

        void TriggerOneShot();

        void TriggerCombo();

        void TriggerAttackAnimIndex(int index);
    }
}
