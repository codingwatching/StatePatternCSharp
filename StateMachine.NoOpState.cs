namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public sealed class NoOpState : IState<T>
		{
			public void OnEntered(IStateMachine<T> machine, IState<T> previous, object parameter = null) {}
			public void Update(IStateMachine<T> machine) {}
			public void OnLeaving(IStateMachine<T> machine, IState<T> next, object parameter = null) {}
            public void Reset() {}
		}
    }
}
