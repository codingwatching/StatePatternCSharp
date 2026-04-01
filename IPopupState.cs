namespace BAStudio.StatePattern
{
    /// <summary>
    /// Lifecycle contract for a popup state within a state machine.
    /// Popup states are self-contained, self-terminating, and not cached.
    /// Extracted from StateMachine&lt;T&gt;.IPopupState to enable clean references with IStateMachine&lt;T&gt;.
    /// </summary>
    public interface IPopupState<T>
    {
        void OnStarting(IStateMachine<T> machine, object parameter = null);
        void OnEnding(IStateMachine<T> machine, object parameter = null);
        void Update(IStateMachine<T> machine);

#if UNITY_2017_1_OR_NEWER
        void FixedUpdate(IStateMachine<T> machine) {}
        void LateUpdate(IStateMachine<T> machine) {}
#endif
    }
}
