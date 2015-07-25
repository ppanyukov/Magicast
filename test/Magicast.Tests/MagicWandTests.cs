// Copyright Philip Panyukov, 2015

namespace Magicast.Tests
{
    using System;
    using System.Runtime.InteropServices;

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
        public void CannnotCastStructToClassInANaiveWay()
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
        public void CannotCastClassToStructInNaiveWay()
        {
            // TODO: check that source and target are of same kind (value type / class) and throw exception there?
            //
            // Structs and classes have different layout in memory.
            // So a naive cast will likely blow up. We don't allow this and throw.
            var foo = new FooClass
            {
                fieldA = "FooClass.FieldA",
                fieldB = "FooClass.FieldB"
            };

            Assert.Throws<InvalidOperationException>(() => MagicWand<FooClass, BarStruct>.Cast(foo));
        }

    }


    // Types under test
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
}