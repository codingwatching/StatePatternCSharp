namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        /// <summary>
        /// Envelope wrapping any event that crosses a peer machine boundary.
        /// States opt in to source discrimination per event type via
        /// <c>IEventReceiverState&lt;T, ForwardedEvent&lt;E&gt;&gt;</c>.
        /// </summary>
        public struct ForwardedEvent<E>
        {
            public StateMachine<T> source;
            public E inner;
            public static implicit operator E(ForwardedEvent<E> ev) => ev.inner;

            public ForwardedEvent(StateMachine<T> source, E inner)
            {
                this.source = source;
                this.inner = inner;
            }
        }
    }
}
