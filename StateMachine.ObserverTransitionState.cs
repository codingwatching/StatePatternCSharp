using System;

namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public abstract class ObserverTransitionState<H, FROM, TO> : IState<T>, IObserver<H> where FROM : IState<T> where TO : class, IState<T>
        {
            IDisposable? handle;
            private readonly IObservable<H>? observable;
            TO instancedNext;
            object parameter;

			public ObserverTransitionState(IObservable<H> observable, TO instance = null, object parameter = null)
            {
                this.observable = observable;
                instancedNext = instance;
                this.parameter = parameter;
            }

            public void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null)
            {
                if (typeof(FROM) != previous.GetType()) throw new InvalidOperationException("Transition source state mismatch");

                // Reset
                isCompleted = false;
                exception = null;
                progress = default(H);
                handle = observable?.Subscribe(this);
            }

            public void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null)
            {
                handle!.Dispose();
            }

            public void Update(IStateMachine<T> machine, T subject)
            {
                if (isCompleted)
                {
                    if (instancedNext != null) machine.ChangeState(instancedNext, parameter);
                    else machine.ChangeState<TO>(parameter);
                }
                else if (exception != null) {
                    throw exception;
                }
            }

            bool isCompleted;
            Exception exception;
            H progress;

            public abstract void Reset();

            public virtual void OnCompleted()
            {
                isCompleted = true; // Because we don't know which thread it is
            }

            public virtual void OnNext(H value)
            {
                progress = value;
            }

            public virtual void OnError(Exception error)
            {
                exception = error;
            }
        }
    }
}