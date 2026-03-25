namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public sealed class NoOpState : IState
		{
			public void OnEntered(StateMachine<T> machine, IState previous, T subject, object parameter = null) {}
			public void Update(StateMachine<T> machine, T subject) {}
			public void OnLeaving(StateMachine<T> machine, IState next, T subject, object parameter = null) {}
            public void Reset() {}
		}
    }
}
