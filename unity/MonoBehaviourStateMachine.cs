#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// MonoBehaviour wrapper around StateMachine&lt;T&gt;.
    /// Auto-registers co-located MonoBehaviour states and cloned ScriptableObject states.
    /// </summary>
    public abstract class MonoBehaviourStateMachine<T> : MonoBehaviour
    {
        [Header("State Configuration")]
        [Tooltip("ScriptableObject assets implementing StateMachine<T>.IState. Assign in the Inspector.")]
        [SerializeField] ScriptableObject[] scriptableObjectStates;

        List<UnityEngine.Object> clonedInstances;

        public StateMachine<T> Machine { get; private set; }

        protected abstract T ResolveSubject();

        /// <summary>
        /// Called after the machine is created and states are registered.
        /// Override to set the initial state or perform additional setup.
        /// </summary>
        protected virtual void Initialize() {}

        protected virtual void Awake()
        {
            T subject = ResolveSubject();
            if (IsNullSubject(subject))
            {
                throw new InvalidOperationException(
                    $"[{GetType().Name}] ResolveSubject() returned null. " +
                    "Override ResolveSubject() to return a valid subject instance.");
            }

            Machine = new StateMachine<T>(subject);
            RegisterComponentStates();
            RegisterScriptableObjectStates();
            Initialize();
        }

        void RegisterComponentStates()
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (ReferenceEquals(component, this))
                    continue;

                if (component is StateMachine<T>.IState state)
                    Machine.CacheByType(state);
            }
        }

        void RegisterScriptableObjectStates()
        {
            if (scriptableObjectStates == null)
                return;

            for (int i = 0; i < scriptableObjectStates.Length; i++)
            {
                ScriptableObject stateAsset = scriptableObjectStates[i];
                if (stateAsset == null)
                    continue;

                if (stateAsset is not StateMachine<T>.IState)
                {
                    Debug.LogWarning(
                        $"[{GetType().Name}] ScriptableObject '{stateAsset.name}' ({stateAsset.GetType().Name}) " +
                        "does not implement StateMachine<T>.IState. Skipping.",
                        this);
                    continue;
                }

                ScriptableObject clone = Instantiate(stateAsset);
                clone.name = stateAsset.name;

                if (clone is StateMachine<T>.IState clonedState)
                {
                    clonedInstances ??= new List<UnityEngine.Object>();
                    clonedInstances.Add(clone);
                    Machine.CacheByType(clonedState);
                }
            }
        }

        protected virtual void Update()
        {
            EnsureMachineInitialized();
            Machine.Update();
        }

        protected virtual void FixedUpdate()
        {
            EnsureMachineInitialized();
            Machine.FixedUpdate(Machine, Machine.Subject);
        }

        protected virtual void LateUpdate()
        {
            EnsureMachineInitialized();
            Machine.LateUpdate(Machine, Machine.Subject);
        }

        protected virtual void OnDestroy()
        {
            if (clonedInstances == null)
                return;

            for (int i = 0; i < clonedInstances.Count; i++)
            {
                UnityEngine.Object clone = clonedInstances[i];
                if (clone == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(clone);
                else
                    DestroyImmediate(clone);
            }

            clonedInstances = null;
        }

        void EnsureMachineInitialized()
        {
            if (Machine != null)
                return;

            throw new InvalidOperationException(
                $"[{GetType().Name}] Machine is null. Did you override Awake() without calling base.Awake()?");
        }

        static bool IsNullSubject(T subject)
        {
            if (subject is UnityEngine.Object unityObject)
                return unityObject == null;

            return ReferenceEquals(subject, null);
        }
    }
}
#endif
