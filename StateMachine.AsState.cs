namespace BAStudio.StatePattern
{
    public partial class StateMachine<T> : IState<T>
    {
        // --- IState<T> explicit implementation ---

        void IState<T>.OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter)
        {
            Subject = subject;
            OnNestedEntry(previous, parameter);
        }

        void IState<T>.Update(IStateMachine<T> machine, T subject)
        {
            Update();
        }

        void IState<T>.OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter)
        {
            if (CurrentState is not NoOpState && CurrentState != null)
                CurrentState.OnLeaving(this, null, subject, parameter);
            CurrentState = new NoOpState();
        }

        void IState<T>.Reset()
        {
            ResetInternalState();
        }

#if UNITY_2017_1_OR_NEWER
        void IState<T>.FixedUpdate(IStateMachine<T> machine, T subject)
        {
            FixedUpdate();
        }

        void IState<T>.LateUpdate(IStateMachine<T> machine, T subject)
        {
            LateUpdate();
        }
#endif

        // --- Virtual hooks ---

        /// <summary>
        /// Called when this machine is entered as a state inside a parent machine.
        /// Override to set the initial state (e.g., <c>ChangeState&lt;MyInitialState&gt;()</c>).
        /// Subject is already set when this is called.
        /// </summary>
        protected virtual void OnNestedEntry(IState<T> previous, object parameter) { }

        /// <summary>
        /// Called when IState&lt;T&gt;.Reset() is invoked (after parent transitions away).
        /// Default: no-op (AutoStateCache preserved for re-entry efficiency).
        /// Override to clear: <c>AutoStateCache?.Clear();</c>
        /// </summary>
        protected virtual void ResetInternalState()
        {
            peerEventForwardingDepth = 0;
            peerDispatchingDepth = 0;
        }

        // --- Debug path ---

        /// <summary>
        /// Full nested state path from this machine downward.
        /// Example: "Alive > Exploration > Walking"
        /// </summary>
        public string GetStatePath()
        {
            if (CurrentState == null || CurrentState is NoOpState)
                return "(none)";

            string name = CurrentState.GetType().Name;

            if (CurrentState is StateMachine<T> sub)
                return name + " > " + sub.GetStatePath();

            return name;
        }
    }
}
