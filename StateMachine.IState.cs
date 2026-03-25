namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public interface IState
		{
			void OnEntered(StateMachine<T> machine, IState previous, T subject, object parameter = null);
			/// <summary>
			/// <para> Only happens in StateMachine<T>.Update().</para>
			/// <para> It's not guaranteed that this will get called between `OnEntered()` and `OnLeaving()`, because these 2 methods can `ChangeState()` too.</para>
			/// </summary>
			void Update(StateMachine<T> machine, T subject);
			void OnLeaving(StateMachine<T> machine, IState next, T subject, object parameter = null);

			/// <summary>
			/// When used with ChangedState<S>, states are cached and reused. Reset() get called after the machine finished the whole ChangeState().
			/// </summary>
			void Reset ();

#if UNITY_2017_1_OR_NEWER
			void FixedUpdate(StateMachine<T> machine, T subject) {}
			void LateUpdate(StateMachine<T> machine, T subject) {}
#endif
        }
    }
}
