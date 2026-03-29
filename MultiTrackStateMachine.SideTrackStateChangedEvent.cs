namespace BAStudio.StatePattern
{
    public partial class MultiTrackStateMachine<T, TRACK> where TRACK : unmanaged, System.Enum
    {
        public struct SideTrackStateChangedEvent
        {
			public TRACK track;
            public IState<T> from, to;
            public SideTrackStateChangedEvent(TRACK track, IState<T> from, IState<T> to)
            {
                this.track = track;
                this.from = from;
                this.to = to;
            }
        }
    }
}
