namespace BAStudio.StatePattern
{
    /// <summary>
    /// Lifecycle contract for a state within a state machine.
    /// Extracted from StateMachine&lt;T&gt;.IState to enable clean mutual references with IStateMachine&lt;T&gt;.
    /// </summary>
    public interface IState<T>
    {
        void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null);

        /// <summary>
        /// <para>Only happens in StateMachine&lt;T&gt;.Update().</para>
        /// <para>Not guaranteed between OnEntered and OnLeaving — those can trigger ChangeState too.</para>
        /// </summary>
        void Update(IStateMachine<T> machine, T subject);

        void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null);

        /// <summary>
        /// Called after the machine completes the full ChangeState sequence.
        /// Used to reset cached/reused state instances.
        /// </summary>
        void Reset();

#if UNITY_2017_1_OR_NEWER
        void FixedUpdate(IStateMachine<T> machine, T subject) {}
        void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
    }
}
