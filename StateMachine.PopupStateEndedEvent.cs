namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct PopupStateEndedEvent
        {
            public IPopupState<T> popupState;

            public PopupStateEndedEvent(IPopupState<T> popupState)
            {
                this.popupState = popupState;
            }
        }
    }

}