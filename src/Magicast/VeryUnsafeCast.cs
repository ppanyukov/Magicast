// Copyright Philip Panyukov, 2015

namespace Magicast
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The magic cast code here. Emit clever bit of IL to cast stuff from any type to any.
    /// </summary>
    /// <typeparam name="TSource">The type to cast from.</typeparam>
    /// <typeparam name="TTarget">The type to cast to.</typeparam>
    /// <remarks>
    /// Note on static members. Because this is a generic, the static members will be generated
    /// for each combination of type parameters. This is the reason why we can generate the
    /// dynamic cast once in the static class constructor.
    /// </remarks>
    internal static class VeryUnsafeCast<TSource, TTarget>
    {
        /// <summary>
        /// Dynamically generated delegate to cast from 
        /// <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
        /// </summary>
        private static readonly Func<TSource, TTarget> castDelegate;

        /// <summary>
        /// Here we generate the dynamic delegate.
        /// </summary>
        static VeryUnsafeCast()
        {
            castDelegate = CreateCastDelegate();
        }

        /// <summary>
        /// Magically casts any source type <typeparamref name="TSource"/> 
        /// to target type <typeparamref name="TTarget"/>.
        /// It's magic! :)
        /// </summary>
        /// <param name="obj">
        /// The object of type <typeparamref name="TSource"/> to cast to 
        /// target type <typeparamref name="TTarget"/>.
        /// </param>
        /// <returns>
        /// An object of type <typeparamref name="TTarget"/>.
        /// </returns>
#if !NET40
        // Stragely this attribute is not available in .NET 4.0 but available everywhere else!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static TTarget Cast(TSource obj)
        {
            return castDelegate(obj);
        }

        /// <summary>
        /// Generates dynamic method using IL emit.
        /// </summary>
        /// <returns>
        /// Delegate which actually casts object of type <typeparamref name="TSource"/> 
        /// to type <typeparamref name="TTarget"/>
        /// </returns>
        private static Func<TSource, TTarget> CreateCastDelegate()
        {

            //
            // dnxcore50:
            //      Need System.Reflection for Type.GetTypeInfo.
            //      This looks to be the only way to get to the assembly which we need to generated dynamic method.
            // NET40:
            //      Type.GetTypeInfo() is not available.
#if NET40
            var isSourceClass = typeof(TSource).IsClass;
            var isTargetClass = typeof(TTarget).IsClass;
            var assembly = Assembly.GetExecutingAssembly();
#else
            var isSourceClass = typeof(TSource).GetTypeInfo().IsClass;
            var isTargetClass = typeof(TTarget).GetTypeInfo().IsClass;
            var assembly = typeof(VeryUnsafeCast<TSource, TTarget>).GetTypeInfo().Assembly;
#endif
            // Both source and target need to be either class or struct.
            // Otherwise things will crash because the two things are not compatible in memory layout.
            var isOk = isSourceClass == isTargetClass;
            if (!isOk)
            {
                return ThrowFunc;
            }

            var someMethod = new DynamicMethod(
                name: "VeryUnsafeCast - CastToAnything", 
                returnType: typeof(TTarget), 
                parameterTypes: new Type[] { typeof(TSource) }, 
                m: assembly.ManifestModule);

            var il = someMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // Load arg_0 onto the stack (of type TSourceType)
            il.Emit(OpCodes.Ret);

            return (Func<TSource, TTarget>)someMethod.CreateDelegate(typeof(Func<TSource, TTarget>));
        }

        private static TTarget ThrowFunc(TSource obj)
        {
            throw new InvalidOperationException("Even though it's magic, we can't cast structs to classes and classes to structs. Because things will really crash if you do.");
        }
    }
}