namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct MainStateChangedEvent
        {
            public IState<T> from, to;

            public MainStateChangedEvent(IState<T> from, IState<T> to)
            {
                this.from = from;
                this.to = to;
            }
        }
    }

}
