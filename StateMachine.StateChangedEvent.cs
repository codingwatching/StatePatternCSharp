namespace BAStudio.StatePattern
{
    public struct StateChangedEvent<T>
    {
        public IState<T> from, to;

        public StateChangedEvent(IState<T> from, IState<T> to)
        {
            this.from = from;
            this.to = to;
        }
    }

}
