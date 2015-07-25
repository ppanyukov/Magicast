namespace Magicast
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A dummy type to test cross-framework things.
    /// </summary>
    internal sealed class DummyClass
    {
#if !NET40  // Strange, this isn't in .NET 4.0 but everywhere else!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public string Foo()
        {
            return "foo";
        }
    }
}
