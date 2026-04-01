#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public abstract class TimedPopupState : IPopupState<T>
        {
            public float endTime;
            public abstract void OnEnding(IStateMachine<T> machine, object parameter = null);

            public void OnStarting(IStateMachine<T> machine, object parameter = null)
            {
                endTime = (float) parameter;
            }

            public void Update(IStateMachine<T> machine)
            {
                if (Time.time > endTime)
                    machine.EndPopupState(this);
            }
        }
    }
}
#endif
