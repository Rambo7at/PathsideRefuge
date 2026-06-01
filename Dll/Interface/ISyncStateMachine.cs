using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Interface
{
    public interface ISyncStateMachine
    {
        int GetState();

        int GetMoveState();

        void SetState(int State);

        void SetMoveState(int State);

    }
}
