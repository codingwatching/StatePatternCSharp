using System;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// Default resolver that creates state instances via Activator.CreateInstance.
    /// Equivalent to `new S()` but without requiring the new() constraint at compile time.
    /// Throws MissingMethodException if the type has no parameterless constructor.
    /// </summary>
    public class ActivatorStateResolver : IStateResolver
    {
        public static readonly ActivatorStateResolver Instance = new ActivatorStateResolver();

        public object Resolve(Type stateType)
        {
            return Activator.CreateInstance(stateType);
        }
    }
}
