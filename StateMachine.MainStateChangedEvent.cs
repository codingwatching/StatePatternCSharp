namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct MainStateChangedEvent
        {
            public IState from, to;

            public MainStateChangedEvent(IState from, IState to)
            {
                this.from = from;
                this.to = to;
            }
        }
    }

}
