// Copyright Philip Panyukov, 2015

namespace Magicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    public sealed class VeryUnsafeCastTests
    {
        [Fact]
        public void StructToStruct_CanCast()
        {
            var foo = new FooStruct { fieldA = "FooStruct.FieldA", fieldB = "FooStruct.FieldB" };

            var bar = VeryUnsafeCast<FooStruct, BarStruct>.Cast(foo);
            Assert.Equal(bar.fieldC, foo.fieldA);
            Assert.Equal(bar.fieldD, foo.fieldB);
        }

        [Fact]
        public void ClassToClass_CanCast()
        {
            var foo = new FooClass { fieldA = "FooClass.fieldA", fieldB = "FooClass.fieldB" };

            var bar = VeryUnsafeCast<FooClass, BarClass>.Cast(foo);
            Assert.Equal(bar.fieldC, foo.fieldA);
            Assert.Equal(bar.fieldD, foo.fieldB);
        }

        [Fact]
        public void StructToClass_CannotCast_Should_Throw_InvalidCastException()
        {
            // TODO: check that source and target are of same kind (value type / class) and throw exception there?
            //
            // Structs and classes have different layout in memory.
            // So a naive cast will likely blow up. We don't allow this and throw.
            var foo = new FooStruct { fieldA = "FooStruct.FieldA", fieldB = "FooStruct.FieldB" };

            Assert.Throws<InvalidCastException>(() => VeryUnsafeCast<FooStruct, BarClass>.Cast(foo));
        }

        [Fact]
        public void ClassToStruct_CannotCast_Should_Throw_InvalidCastException()
        {
            // Structs and classes have different layout in memory.
            // So a naive cast will likely blow up. We don't allow this and throw.
            var foo = new FooClass { fieldA = "FooClass.FieldA", fieldB = "FooClass.FieldB" };

            Assert.Throws<InvalidCastException>(() => VeryUnsafeCast<FooClass, BarStruct>.Cast(foo));
        }

        [Fact]
        public void Interfaces_CannotCast_FromInterface_ToIncompatibleInterface()
        {
            // Casting from an interface is even more dangerous because we have no
            // clue what the source memroy layout is. Hence not allowed.
            IList<int> foo = new List<int>();
            Assert.Throws<InvalidCastException>(() => VeryUnsafeCast<IList<int>, IList<string>>.Cast(foo));
        }

        [Fact]
        public void Interfaces_CanCast_FromClass_ToInterface_IfTargetIsAsignable_CanCast()
        {
            // Casting from an interface is even more dangerous because we have no
            // clue what the source memroy layout is. Hence not allowed.
            List<int> foo = new List<int>() { 0, 1, 2, 3 };
            var barIList = VeryUnsafeCast<List<int>, IList<int>>.Cast(foo);
            var barIEnumerable = VeryUnsafeCast<List<int>, IEnumerable<int>>.Cast(foo);

            Assert.Equal(foo.Count, barIList.Count);
            Assert.Equal(foo.Count, barIEnumerable.Count());
        }

        [Fact]
        public void Interfaces_CanCast_FromInterface_ToInterface_IfTargetIsAssignable()
        {
            // Casting from an interface is even more dangerous because we have no
            // clue what the source memroy layout is. Hence not allowed.
            IList<int> foo = new List<int> { 0, 1, 2, 3 };
            var barIList = VeryUnsafeCast<IList<int>, IList<int>>.Cast(foo);
            var barIEnumerable = VeryUnsafeCast<IEnumerable<int>, IEnumerable<int>>.Cast(foo);

            Assert.Equal(foo.Count, barIList.Count);
            Assert.Equal(foo.Count, barIEnumerable.Count());
        }

        [Fact]
        public void Interfaces_CanCast_FromIEnumerableDerived_ToIEnumerableBase()
        {
            IEnumerable<FooDerivedClass> fooDerivedEnum = new List<FooDerivedClass> { new FooDerivedClass { fieldBaseA = "a", fieldBaseB = "b", fieldDerivedA = "c", fieldDerivedB = "d" } };

            // This works
            IEnumerable<FooBaseClass> fooBaseEnum = fooDerivedEnum;

            // This should also work right?
            var ourCast = VeryUnsafeCast<IEnumerable<FooDerivedClass>, IEnumerable<FooBaseClass>>.Cast(fooDerivedEnum);
            Assert.Equal(fooDerivedEnum.First().fieldBaseA, ourCast.First().fieldBaseA);
            Assert.Equal(fooDerivedEnum.First().fieldBaseB, ourCast.First().fieldBaseB);
        }

        [Fact]
        public void CalssHierarchy_CanCast_ToFlatClass()
        {
            // Having the source class FooDerivedClass which is part of the hierarchy:
            //    object 
            //      --> FooBaseClass 
            //              fieldA
            //              fieldB
            //          --> FooDerivedClass
            //              fieldC
            //              fieldD
            //
            // We can cast it to another "flat" class and accumulate all the fields:
            //      object
            //      --> BarFlatClass
            //          fieldA
            //          fieldB
            //          fieldC
            //          fieldD
            var fooDerived = new FooDerivedClass { fieldBaseA = "fieldBaseA", fieldBaseB = "fieldBaseB", fieldDerivedA = "fieldDerivedA", fieldDerivedB = "fieldDerivedB" };

            var bar = VeryUnsafeCast<FooDerivedClass, BarFlatClass>.Cast(fooDerived);
            Assert.Equal(fooDerived.fieldBaseA, bar.fieldFlatA);
            Assert.Equal(fooDerived.fieldBaseB, bar.fieldFlatB);
            Assert.Equal(fooDerived.fieldDerivedA, bar.fieldFlatC);
            Assert.Equal(fooDerived.fieldDerivedB, bar.fieldFlatD);
        }

        [Fact]
        public void FlatClass_CanCast_To_ClassHierarchy()
        {
            // Just like we can cast source type with a class hierarchy to flat type,
            // we can do the same the other way around:
            // Cast a type with flat structure to hierarchy:
            //
            // Having the source flat class FooFlatClass:
            //      object
            //      --> FooFlatClass
            //          fieldA
            //          fieldB
            //          fieldC
            //          fieldD
            //
            // We can cast it to a type with which is part of hierarchy:
            //    object 
            //      --> FooBaseClass 
            //              fieldA
            //              fieldB
            //          --> FooDerivedClass
            //              fieldC
            //              fieldD
            //

            var barFlat = new BarFlatClass { fieldFlatA = "fieldBaseA", fieldFlatB = "fieldBaseB", fieldFlatC = "fieldBaseC", fieldFlatD = "fieldBaseD", };

            var fooHierarchy = VeryUnsafeCast<BarFlatClass, FooDerivedClass>.Cast(barFlat);
            Assert.Equal(barFlat.fieldFlatA, fooHierarchy.fieldBaseA);
            Assert.Equal(barFlat.fieldFlatB, fooHierarchy.fieldBaseB);
            Assert.Equal(barFlat.fieldFlatC, fooHierarchy.fieldDerivedA);
            Assert.Equal(barFlat.fieldFlatD, fooHierarchy.fieldDerivedB);
        }

        [Fact]
        public void Polymorphysm_CastingOnlyInvolvesData_And_Thus_OpensAnInteresting_PolymorphicBehaviours()
        {
            // Casting only applies to *data*. The methods are not affected.
            // This can be a disappointing thing or it can be a good thing.
            //
            // So for example, here we can change Cat into Dog :)

            var cat = new Cat { name = "My fluffy Cat" };

            // Turn this cat into Dog as far as behaviour is concerned :)
            var dog = VeryUnsafeCast<Cat, Dog>.Cast(cat);

            // The dog still has the cat's name
            Assert.Equal(cat.name, dog.name);

            // But it now behaves like a dog :)
            Assert.Equal(Dog.DogSound, dog.SaySometing());
        }

        [Fact]
        public void ExtraFields_CanCast_ToTypeWithFewerFields_AndExtraFieldsJustDropOff()
        {
            // The source and target types don't have to have eactly same number of fields.
            // For example here the target will have fewer fieds than the source and that's fine.
            var foo = new FooClassWithThreeFields() { fieldA = "foo.FieldA", fieldB = "foo.FieldA", fieldC = "foo.FieldC", };

            var bar = VeryUnsafeCast<FooClassWithThreeFields, BarClassWithOneField>.Cast(foo);

            // The extra fields just drop off.
            Assert.Equal(foo.fieldA, bar.fieldA);
        }

        [Fact(Skip = "This will lead to a crash so not included in the test run.")]
        public void ExtraFields_CannotCast_ToTypeWithMoreFields_AsAccessingThoseExtraFields_WillLikelyLeadToACrash()
        {
            // As these casts are just a bunch of memory tricks, 
            // we can't expect to cast to a type with more fields and be able
            // to access those extra fields -- those memory locations may not be there at all.
            //
            // TODO: Can we do anything to sanity check this in the MagicCast like we do with structs and classes?
            var bar = new BarClassWithOneField { fieldA = "bar.fieldA" };

            var foo = VeryUnsafeCast<BarClassWithOneField, FooClassWithThreeFields>.Cast(bar);

            // The field that was there is still here
            Assert.Equal(bar.fieldA, foo.fieldA);

            // Getting to any of these extra fields is likely to lead to a big boom.
            // Undefined behaviour let's say.
            Assert.Equal(default(string), foo.fieldB);
            Assert.Equal(default(string), foo.fieldC);
        }

        // Arrays and enumerables.
        // Probably the coolest feature is you can cast arrays and IEnumerable<T> to anything too.
        [Fact]
        public void Array_CanCast_EnumArray_ToIntArray()
        {
            // This is a ligitimate use case where we have something like this:
            //   Source: array of enum type
            //   Target: array of int type
            var enumArray = new FooEnumBasedOnInt[] { FooEnumBasedOnInt.ValueC, FooEnumBasedOnInt.ValueB, FooEnumBasedOnInt.ValueA, };

            // This does not compile
            //var intArray = (int[])enumArray;

            // This will work, but:
            //  - will convert the array to IEnumerable<T>, which is not the same as array
            //  - will ultimately iterate over the arrray in order to cast
            //  - in order to get back to array, need to invoke ToArray() which will copy the array.
            //
            //IEnumerable<int> intEnumerable = enumArray.Cast<int>();
            //int[] intArray = enumArray.Cast<int>().ToArray();

            // We can do better. In this case the enum array *is* int array.
            int[] intArray = VeryUnsafeCast<FooEnumBasedOnInt[], int[]>.Cast(enumArray);

            Assert.Equal((int)enumArray[0], intArray[0]);
            Assert.Equal((int)enumArray[1], intArray[1]);
            Assert.Equal((int)enumArray[2], intArray[2]);
        }

        [Fact]
        public void Array_CanCast_ClassArray_TAnotherClassArray()
        {
            // It does not have to be enum[] to int[] cast.
            // Can do any array of objects into arary of other type as long as the two types are compatible.
            var fooArray = new FooDerivedClass[]
            {
                new FooDerivedClass { fieldBaseA = "0.fieldBaseA", fieldBaseB = "0.fieldBaseB", fieldDerivedA = "0.fieldDerivedA", fieldDerivedB = "0.fieldDerivedB", },
                new FooDerivedClass { fieldBaseA = "1.fieldBaseA", fieldBaseB = "1.fieldBaseB", fieldDerivedA = "1.fieldDerivedA", fieldDerivedB = "1.fieldDerivedB", },
            };

            // OK, this does not compile
            //var barArray = (BarFlatClass[])fooArray;

            // But we still can
            var barArray = VeryUnsafeCast<FooDerivedClass[], BarFlatClass[]>.Cast(fooArray);

            // Everything should still work.
            Assert.Equal(fooArray.Length, barArray.Length);

            for (int i = 0; i < fooArray.Length; i++)
            {
                var foo = fooArray[i];
                var bar = barArray[i];

                Assert.Equal(foo.fieldBaseA, bar.fieldFlatA);
                Assert.Equal(foo.fieldBaseB, bar.fieldFlatB);
                Assert.Equal(foo.fieldDerivedA, bar.fieldFlatC);
                Assert.Equal(foo.fieldDerivedB, bar.fieldFlatD);
            }
        }

        [Fact]
        public void List_CanCast_ClassList_TAnotherClassList()
        {
            // Same goes for lists

            var fooArray = new List<FooDerivedClass>
            {
                new FooDerivedClass { fieldBaseA = "0.fieldBaseA", fieldBaseB = "0.fieldBaseB", fieldDerivedA = "0.fieldDerivedA", fieldDerivedB = "0.fieldDerivedB", },
                new FooDerivedClass { fieldBaseA = "1.fieldBaseA", fieldBaseB = "1.fieldBaseB", fieldDerivedA = "1.fieldDerivedA", fieldDerivedB = "1.fieldDerivedB", },
            };

            // OK, this does not compile
            //var barArray = (BarFlatClass[])fooArray;

            // But we still can
            var barArray = VeryUnsafeCast<List<FooDerivedClass>, List<BarFlatClass>>.Cast(fooArray);

            // Everything should still work.
            Assert.Equal(fooArray.Count, barArray.Count);

            for (int i = 0; i < fooArray.Count; i++)
            {
                var foo = fooArray[i];
                var bar = barArray[i];

                Assert.Equal(foo.fieldBaseA, bar.fieldFlatA);
                Assert.Equal(foo.fieldBaseB, bar.fieldFlatB);
                Assert.Equal(foo.fieldDerivedA, bar.fieldFlatC);
                Assert.Equal(foo.fieldDerivedB, bar.fieldFlatD);
            }
        }

        [Fact]
        public void Dictionary_CanCast_ClassMap_TAnotherClassMap()
        {
            // Same goes for dictionaries

            var fooMap = new Dictionary<string, FooDerivedClass>
            {
                { "key", new FooDerivedClass { fieldBaseA = "0.fieldBaseA", fieldBaseB = "0.fieldBaseB", fieldDerivedA = "0.fieldDerivedA", fieldDerivedB = "0.fieldDerivedB", } },
                { "key2", new FooDerivedClass { fieldBaseA = "1.fieldBaseA", fieldBaseB = "1.fieldBaseB", fieldDerivedA = "1.fieldDerivedA", fieldDerivedB = "1.fieldDerivedB", } }
            };

            // But we still can
            var barMap = VeryUnsafeCast<Dictionary<string, FooDerivedClass>, Dictionary<string, BarFlatClass>>.Cast(fooMap);

            // Everything should still work.
            Assert.Equal(fooMap.Count, barMap.Count);

            foreach (var kv in fooMap)
            {
                var foo = fooMap[kv.Key];
                var bar = barMap[kv.Key];

                Assert.Equal(foo.fieldBaseA, bar.fieldFlatA);
                Assert.Equal(foo.fieldBaseB, bar.fieldFlatB);
                Assert.Equal(foo.fieldDerivedA, bar.fieldFlatC);
                Assert.Equal(foo.fieldDerivedB, bar.fieldFlatD);
            }

            // Add stuff to the new map
            barMap.Add("key3", new BarFlatClass { fieldFlatA = "flatFieldA", fieldFlatB = "flatFieldB", fieldFlatC = "flatFieldC", fieldFlatD = "flatFieldD" });


            // Use it from the old :)
            Assert.Equal(fooMap["key3"].fieldBaseA, "flatFieldA");
            Assert.Equal(fooMap["key3"].fieldBaseB, "flatFieldB");
            Assert.Equal(fooMap["key3"].fieldDerivedA, "flatFieldC");
            Assert.Equal(fooMap["key3"].fieldDerivedB, "flatFieldD");
        }

        [Fact]
        public void ArrayToClass_CanCast_ToMakeArrayAccessor_StringArray()
        {
            // We can cast an array to a class to assign the array elements
            // to the fields and then access the array via those fieds
            // instead of via the array.
            //
            // The keys to success: 
            //   - target type needs to be a class
            //   - the first field in target needs to be of type object
            //   - here is the target type should have not more fields than there are elements in the array.
            var array = new[] { "value1", "value2", "value3", "value4" };

            var foo = VeryUnsafeCast<string[], FooWithArrayPad_StringsAutoProps>.Cast(array);
            Assert.Equal(array[0], foo.AutoPropA);
            Assert.Equal(array[1], foo.AutoPropB);
        }

        [Fact]
        public void ArrayToClass_CanCast_ToMakeArrayAccessor_IntArray()
        {
            // We can cast an array to a class to assign the array elements
            // to the fields and then access the array via those fieds
            // instead of via the array.
            //
            // The keys to success: 
            //   - target type needs to be a class
            //   - the first field in target needs to be of type object
            //   - here is the target type should have not more fields than there are elements in the array.
            var array = new[] { 1, 2 };

            var foo = VeryUnsafeCast<int[], FooWithArrayPad_Ints>.Cast(array);
            Assert.Equal(array[0], foo.fieldA);
            Assert.Equal(array[1], foo.fieldB);
        }

        [Fact]
        public void AutoProps_CanCast_LikeAnyOtherObject()
        {
            // We don't need to have the source and target with fields.
            // Auto props also work. Because they have backing fields.
            // This is a cool use case when you can quickly turn internal
            // mutable structures into public immutable structures.
            // (with caveat that your users don't use Magicast of course :))

            var foo = new FooWithReadWriteAutoPros
            {
                AutoPropA = "auto prop A",
                AutoPropB = "auto prob B"
            };

            var bar = VeryUnsafeCast<FooWithReadWriteAutoPros, BarWithReadonlyAutoProps>.Cast(foo);

            Assert.Equal(foo.AutoPropA, bar.AutoPropA);
            Assert.Equal(foo.AutoPropB, bar.AutoPropB);

        }

        //  GC-related assurance
        [Fact]
        public void GC_Casts_SurviceGC()
        {
            // When we cast from A to B using our magic code, everyting still works for
            // legitimate cases even after GC has kicked in and collected garbage.

            // We will cast to this.
            BarClass bar = GetBarClassFromFoo();

            var collectionStats = GetGcCollectionStats();

            // Allocate lots of stuff, hopefull induce GC to do whatever on its own.
            for (var i = 0; i < 5000; i++)
            {
                var bytes = new byte[1024];
                GC.AddMemoryPressure(64 * 1024);
            }

            for (var i = 0; i < 10; i++)
            {
                // Hope this works in all versions of .NET
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            var collectionStats2 = GetGcCollectionStats();

            // Expect everything to be still there and no crashes.
            Assert.Equal("fieldA", bar.fieldC);
            Assert.Equal("fieldB", bar.fieldD);

            // Also make sure that we did have collections everywhere.
            for (int i = 0; i < collectionStats2.Length; i++)
            {
                var collections = collectionStats2[i] - collectionStats[i];
                Console.WriteLine("Casts_SurviceGC: GC Collection in Gen {0}: {1}", i, collections);
                Assert.True(collections > 0);
            }
        }

        private static int[] GetGcCollectionStats()
        {
            var maxGeneration = GC.MaxGeneration;
            var collections = new int[maxGeneration];
            for (int i = 0; i < maxGeneration; i++)
            {
                collections[i] = GC.CollectionCount(i);
            }

            return collections;
        }


        private static BarClass GetBarClassFromFoo()
        {
            var foo = new FooClass { fieldA = "fieldA", fieldB = "fieldB" };

            return VeryUnsafeCast<FooClass, BarClass>.Cast(foo);
        }




        // Types under test

        // Simple structs with same structure
        public struct FooStruct
        {
            public string fieldA;

            public string fieldB;
        }

        public struct BarStruct
        {
            public string fieldC;

            public string fieldD;
        }

        // Simple classes with same structure
        public class FooClass
        {
            public string fieldA;

            public string fieldB;
        }

        public class BarClass
        {
            public string fieldC;

            public string fieldD;
        }


        // Class hierarchy

        // The base and the derived classes
        public class FooBaseClass
        {
            public string fieldBaseA;

            public string fieldBaseB;
        }

        public class FooDerivedClass : FooBaseClass
        {
            public string fieldDerivedA;

            public string fieldDerivedB;
        }

        // This one combines the types form hierarchies in layout.
        public class BarFlatClass
        {
            public string fieldFlatA;

            public string fieldFlatB;

            public string fieldFlatC;

            public string fieldFlatD;
        }


        // Types for changing behaviour -- the new kind of polymorphism

        // Here we have cat with data fields like name and behaviour.
        public class Cat
        {
            public const string CatSound = "Meauwuw";

            public string name;

            public string SaySometing()
            {
                return CatSound;
            }
        }

        // When we cast to Cat to Dog, we will acquire data fields (name)
        // but when we call SaySomething it will be Dog's behaviour now.
        // How cool is that.
        public class Dog
        {
            public const string DogSound = "Woof Woof";

            public string name;

            public string SaySometing()
            {
                return DogSound;
            }
        }


        // Types with different number of fields
        public class FooClassWithThreeFields
        {
            public string fieldA;

            public string fieldB;

            public string fieldC;
        }

        public class BarClassWithOneField
        {
            public string fieldA;
        }


        // Types for array tests
        public enum FooEnumBasedOnInt : int
        {
            ValueA = 0,

            ValueB = 1,

            ValueC = 2,
        }


        // Array view, need padding
        public class FooWithArrayPad_Ints
        {
            private object pad; // requires a padding of reference type.

            public int fieldA;

            public int fieldB;
        }

        // Array view, need padding
        public class FooWithArrayPad_StringsAutoProps
        {
            private object pad; // requires a padding of reference type.

            public string AutoPropA { get; }

            public string AutoPropB { get; }
        }


        // Types to test auto properties
        public class FooWithReadWriteAutoPros
        {
            public string AutoPropA { get; set; }
            public string AutoPropB { get; set; }
        }

        public class BarWithReadonlyAutoProps
        {
            // Readonly
            public string AutoPropA { get; }
            public string AutoPropB { get; }
        }

    }
}
