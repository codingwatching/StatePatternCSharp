namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public sealed class NoOpState : IState<T>
		{
			public void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null) {}
			public void Update(IStateMachine<T> machine, T subject) {}
			public void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null) {}
            public void Reset() {}
		}
    }
}
