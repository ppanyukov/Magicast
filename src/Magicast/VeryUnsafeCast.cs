﻿// Copyright Philip Panyukov, 2015

namespace Magicast
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Casts from anything to anything in a VERY DANGEROUS (but fun) way. Use responsibly.
    /// </summary>
    /// <typeparam name="TSource">The type to cast from.</typeparam>
    /// <typeparam name="TTarget">The type to cast to.</typeparam>
    public static class VeryUnsafeCast<TSource, TTarget>
    {
        // Note on static members. Because this is a generic, the static members will be generated
        // for each combination of type parameters. This is the reason why we can generate the
        // dynamic cast delegate once in the static class constructor.
        
        private static readonly Func<TSource, TTarget> castDelegate;

        static VeryUnsafeCast()
        {
            castDelegate = CreateCastDelegate();
        }

        /// <summary>
        /// Magically but VERY DANGEROUSLY casts any source type <typeparamref name="TSource"/> 
        /// to target type <typeparamref name="TTarget"/>. Use responsibly.
        /// </summary>
        /// <param name="obj">
        /// The object of type <typeparamref name="TSource"/> to cast from.
        /// </param>
        /// <returns>
        /// An object of type <typeparamref name="TTarget"/>.
        /// </returns>
        public static TTarget Cast(TSource obj)
        {
            return castDelegate(obj);
        }

        /// <summary>
        /// Generates dynamic method to cast from source to target using IL emit.
        /// </summary>
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
            var isSourceStruct = typeof(TSource).IsValueType;

            var isTargetClass = typeof(TTarget).IsClass;
            var isTargetStruct = typeof(TTarget).IsValueType;

            var isTargetAssignable = typeof(TTarget).IsAssignableFrom(typeof(TSource));

            var assembly = Assembly.GetExecutingAssembly();
#else
            var isSourceClass = typeof(TSource).GetTypeInfo().IsClass;
            var isSourceStruct = typeof(TSource).GetTypeInfo().IsValueType;

            var isTargetClass = typeof(TTarget).GetTypeInfo().IsClass;
            var isTargetStruct = typeof(TTarget).GetTypeInfo().IsValueType;

            var isTargetAssignable = typeof(TTarget).GetTypeInfo().IsAssignableFrom(typeof(TSource).GetTypeInfo());

            var assembly = typeof(VeryUnsafeCast<TSource, TTarget>).GetTypeInfo().Assembly;
#endif
            // Both source and target need to be either class or struct.
            // Otherwise things will crash because the two things are not compatible in memory layout.
            //
            // Equally, we cannot allow the source type to be an interface because it
            // becomes even more insanely dangerous.
            // This cast operates on the premise that we know the source and the target
            // memory layout, which we can't do if the source is the interace.
            //
            // The cast to an interface doesn't make sense either because calls onto
            // interface methods will not work anyway.
            //
            // The only exception for mix-and-match if the target is directly assignable from the source.
            var isOk =
                isTargetAssignable
                ||
                isSourceClass && isTargetClass
                ||
                isSourceStruct && isTargetStruct;
            if (!isOk)
            {
                return ThrowFuncBothMustBeStructOrClass;
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

        private static TTarget ThrowFuncBothMustBeStructOrClass(TSource obj)
        {
            throw new InvalidCastException(
                "Even though it's magic, we can only cast structs to structs and classes to classes and " +
                "also in cases when target is directly assignable from the source.");
        }
    }
}