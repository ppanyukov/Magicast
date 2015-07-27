// Copyright Philip Panyukov, 2015

namespace Magicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    public sealed class MagicWandTests
    {
        [Fact]
        public void CanCastStructToStruct()
        {
            var foo = new FooStruct
            {
                fieldA = "FooStruct.FieldA",
                fieldB = "FooStruct.FieldB"
            };

            var bar = MagicWand<FooStruct, BarStruct>.Cast(foo);
            Assert.Equal(bar.fieldC, foo.fieldA);
            Assert.Equal(bar.fieldD, foo.fieldB);
        }

        [Fact]
        public void CanCastClassToClass()
        {
            var foo = new FooClass
            {
                fieldA = "FooClass.fieldA",
                fieldB = "FooClass.fieldB"
            };

            var bar = MagicWand<FooClass, BarClass>.Cast(foo);
            Assert.Equal(bar.fieldC, foo.fieldA);
            Assert.Equal(bar.fieldD, foo.fieldB);
        }

        [Fact]
        public void CastStructToClass_Should_Throw_InvalidOperationException()
        {
            // TODO: check that source and target are of same kind (value type / class) and throw exception there?
            //
            // Structs and classes have different layout in memory.
            // So a naive cast will likely blow up. We don't allow this and throw.
            var foo = new FooStruct
            {
                fieldA = "FooStruct.FieldA",
                fieldB = "FooStruct.FieldB"
            };

            Assert.Throws<InvalidOperationException>(() => MagicWand<FooStruct, BarClass>.Cast(foo));
        }

        [Fact]
        public void CastClassToStruct_Should_Throw_InvalidOperationException()
        {
            // Structs and classes have different layout in memory.
            // So a naive cast will likely blow up. We don't allow this and throw.
            var foo = new FooClass
            {
                fieldA = "FooClass.FieldA",
                fieldB = "FooClass.FieldB"
            };

            Assert.Throws<InvalidOperationException>(() => MagicWand<FooClass, BarStruct>.Cast(foo));
        }

        [Fact]
        public void CanCastClassHierarchyToFlatClass()
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
            var fooDerived = new FooDerivedClass
            {
                fieldBaseA = "fieldBaseA",
                fieldBaseB = "fieldBaseB",
                fieldDerivedA = "fieldDerivedA",
                fieldDerivedB = "fieldDerivedB"
            };

            var bar = MagicWand<FooDerivedClass, BarFlatClass>.Cast(fooDerived);
            Assert.Equal(fooDerived.fieldBaseA, bar.fieldFlatA);
            Assert.Equal(fooDerived.fieldBaseB, bar.fieldFlatB);
            Assert.Equal(fooDerived.fieldDerivedA, bar.fieldFlatC);
            Assert.Equal(fooDerived.fieldDerivedB, bar.fieldFlatD);
        }

        [Fact]
        public void CanCastFlatClassToClassHierarchy()
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

            var barFlat = new BarFlatClass
            {
                fieldFlatA = "fieldBaseA",
                fieldFlatB = "fieldBaseB",
                fieldFlatC = "fieldBaseC",
                fieldFlatD = "fieldBaseD",
            };

            var fooHierarchy = MagicWand<BarFlatClass, FooDerivedClass>.Cast(barFlat);
            Assert.Equal(barFlat.fieldFlatA, fooHierarchy.fieldBaseA);
            Assert.Equal(barFlat.fieldFlatB, fooHierarchy.fieldBaseB);
            Assert.Equal(barFlat.fieldFlatC, fooHierarchy.fieldDerivedA);
            Assert.Equal(barFlat.fieldFlatD, fooHierarchy.fieldDerivedB);
        }

        [Fact]
        public void CastingOnlyInvolvesData_And_Thus_OpensAnInteresting_PolymorphicBehaviours()
        {
            // Casting only applies to *data*. The methods are not affected.
            // This can be a disappointing thing or it can be a good thing.
            //
            // So for example, here we can change Cat into Dog :)

            var cat = new Cat
            {
                name = "My fluffy Cat"
            };

            // Turn this cat into Dog as far as behaviour is concerned :)
            var dog = MagicWand<Cat, Dog>.Cast(cat);

            // The dog still has the cat's name
            Assert.Equal(cat.name, dog.name);

            // But it now behaves like a dog :)
            Assert.Equal(Dog.DogSound, dog.SaySometing());
        }

        [Fact]
        public void CanCastToTypeWithFewerFields_AndExtraFieldsJustDropOff()
        {
            // The source and target types don't have to have eactly same number of fields.
            // For example here the target will have fewer fieds than the source and that's fine.
            var foo = new FooClassWithThreeFields()
            {
                fieldA = "foo.FieldA",
                fieldB = "foo.FieldA",
                fieldC = "foo.FieldC",
            };

            var bar = MagicWand<FooClassWithThreeFields, BarClassWithOneField>.Cast(foo);

            // The extra fields just drop off.
            Assert.Equal(foo.fieldA, bar.fieldA);
        }

        [Fact(Skip = "This will lead to a crash so not included in the test run.")]
        public void BUT_CannotCastToTypeWithMoreFields_AsAccessingThoseExtraFields_WillLikelyLeadToACrash()
        {
            // As these casts are just a bunch of memory tricks, 
            // we can't expect to cast to a type with more fields and be able
            // to access those extra fields -- those memory locations may not be there at all.
            //
            // TODO: Can we do anything to sanity check this in the MagicCast like we do with structs and classes?
            var bar = new BarClassWithOneField
            {
                fieldA = "bar.fieldA"
            };

            var foo = MagicWand<BarClassWithOneField, FooClassWithThreeFields>.Cast(bar);

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
            var enumArray = new FooEnumBasedOnInt[]
            {
                FooEnumBasedOnInt.ValueC, 
                FooEnumBasedOnInt.ValueB,
                FooEnumBasedOnInt.ValueA,
            };

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
            int[] intArray = MagicWand<FooEnumBasedOnInt[], int[]>.Cast(enumArray);

            Assert.Equal((int)enumArray[0], intArray[0]);
            Assert.Equal((int)enumArray[1], intArray[1]);
            Assert.Equal((int)enumArray[2], intArray[2]);
        }

        [Fact]
        public void IEnumerable_CanCast_EnumEnumerable_ToIntEnumerable()
        {
            // This is a also a ligitimate use case where we have something like this:
            //   Source: IEnumerable<enum>
            //   Target: IEnumerable<int>
            var enumEnum = new FooEnumBasedOnInt[]
            {
                FooEnumBasedOnInt.ValueC,
                FooEnumBasedOnInt.ValueB,
                FooEnumBasedOnInt.ValueA,
            }.AsEnumerable();

            // This does not compile
            //var intArray = (IEnumerable<int>)enumEnum;

            // This will work, but:
            //  - will ultimately iterate over the source IEnumerable in order to cast
            //
            IEnumerable<int> intEnumerable = enumEnum.Cast<int>();

            // We can do better. In this case the enum IEnumerable *is* int IEnumerable.
            IEnumerable<int> intEnum = MagicWand<IEnumerable<FooEnumBasedOnInt>, IEnumerable<int>>.Cast(enumEnum);

            Assert.Equal((int)enumEnum.Skip(0).First(),intEnum.Skip(0).First());
            Assert.Equal((int)enumEnum.Skip(1).First(),intEnum.Skip(1).First());
            Assert.Equal((int)enumEnum.Skip(2).First(),intEnum.Skip(2).First());
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
    }
}