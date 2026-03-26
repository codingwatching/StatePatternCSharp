using System;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// Resolves state instances by type when a cache miss occurs in ChangeState&lt;S&gt;().
    /// The resolved instance is cached by the machine for future transitions.
    /// </summary>
    public interface IStateResolver
    {
        /// <summary>
        /// Create or retrieve a state instance for the given type.
        /// The returned instance must implement IState for the machine's T.
        /// </summary>
        object Resolve(Type stateType);
    }
}
