namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct NewPopupStateEvent
		{
			public IPopupState<T> popupState;

            public NewPopupStateEvent(IPopupState<T> popupState)
            {
                this.popupState = popupState;
            }
        }
    }

}