// Copyright Philip Panyukov, 2015

namespace Magicast.Tests
{
    using System;

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
    }
}