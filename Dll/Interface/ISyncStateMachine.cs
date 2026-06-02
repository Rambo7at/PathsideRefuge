using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Interface
{
    public interface ISyncStateMachine
    {
        int GetState();

        int GetAnimState();

        void SetState(int State);

        void SetAnimState(int State);

    }
}
