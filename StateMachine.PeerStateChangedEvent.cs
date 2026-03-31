namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct PeerStateChangedEvent
        {
            public StateMachine<T> source;
            public IState<T> from, to;

            public PeerStateChangedEvent(StateMachine<T> source, IState<T> from, IState<T> to)
            {
                this.source = source;
                this.from = from;
                this.to = to;
            }
        }
    }
}
