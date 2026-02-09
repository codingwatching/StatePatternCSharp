#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public abstract class TimedPopupState : IPopupState
        {
            public float endTime;
            public abstract void OnEnding(StateMachine<T> machine, T subject, object parameter = null);

            public void OnStarting(StateMachine<T> machine, T subject, object parameter = null)
            {
                endTime = (float) parameter;
            }

            public void Update(StateMachine<T> machine, T subject)
            {
                if (Time.time > endTime)
                    machine.EndPopupState(this);
            }
        }
    }
}
#endif