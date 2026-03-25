namespace BAStudio.StatePattern
{
    public partial class MultiTrackStateMachine<T, TRACK> where TRACK : unmanaged, System.Enum
    {
        public struct SideTrackStateChangedEvent
        {
			public TRACK track;
            public StateMachine<T>.IState from, to;
            public SideTrackStateChangedEvent(TRACK track, StateMachine<T>.IState from, StateMachine<T>.IState to)
            {
                this.track = track;
                this.from = from;
                this.to = to;
            }
        }
    }
}
