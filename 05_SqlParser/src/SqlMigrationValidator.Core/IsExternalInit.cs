// Polyfill required for C# record types targeting netstandard2.0.
// The compiler emits init-only setters that reference this type, which only
// exists in net5+. Declaring it here satisfies the compiler without any
// runtime cost — the type is never instantiated.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
