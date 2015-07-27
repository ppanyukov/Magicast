# Magicast

.NET Type System rules are for weaklings. Automagically cast anything to anything in .NET. Almost anything.

Nuget package here: https://www.nuget.org/packages/Magicast/

*WARNING AND DISCLAIMER: 
This is a very dangerous (and some say even sleazy :)) 
piece of code here. Use at your own risk and don't blame me if things go BOOM.
But there are some very useful applications. See further.*

---------
## Problem

.NET is strict in terms of typing. You can't just cast anything to anything even if the source and target type sturcture is the same.

For example, you can't do this in C#:

	class Foo
	{
	  public string field1;
	  public string field2;
	}

	class Bar
	{
	  public string field1;
	  public string field2;
	}

	var foo = new Foo();
	var bar = (Bar)foo;      // does not compile, obviously

But why? The structure is identical right?

Equally, you can't do this either:

	public enum Foo : int
	{
	  one,
	  two,
	  three,
	}

	var enumArray = new Foo[]{Foo.one, Foo.two, Foo.three};
	var intArray = (int[])enumArray;   // does not compile either

But why is that the case? Surely, the enumArray *is* an int array under the hood, right?


And you can't do this either:

	pubic class Foo
	{
	  private string fieldA;
	}

	var foo = new Foo();
	var field = foo.fieldA;  // private field, no can do

That is plain nasty! Got to do all sort of reflection and stuff.

So this library allows to to bend these rules and beat the .NET type system into submissions
and show them who's the real boss here. Of course it uses a **very dangerous** hackery under the hood
therefore use with care and at your own risk.

NOTE: There can be some very legitimate and also interesting applications of this.

---------

## Quick start

All you need to do is this:

	// C#
	using Magicast;

	// Cast anything to anything
	var result = VeryUnsafeCast<FromType, ToType>.Cast(object_to_cast);

**Main Rule -- it's all about fields and memory layout**

Magicast works by giving you a view of the memory allocated by source type and allows you to
see it via another type. It's like in C:

	// Some dodgy C code
	void* memory = malloc(1024);  // allocate 1K RAM
	Foo* foo = (Foo*)memory;      // treat it like Foo
	Bar* bar = (Bar*)foo;         // treat it like Bar now

The main rule to remember is you are doing essentially something like above when you use Magicast
but in the .NET world.

And in .NET world when we talk about memory of an object, we are talking about the *fields*
in the object. Certainly not methods.

*Remember: make sure the source and target types are compatible in field layout!*



**Quick Rules -- what you can cast**

There is support for the following:

- Value type to Value type

	- struct to struct
	- primitive to primitive
	- enums

- Class to Class

	- any concrete class to another concrete class (reference types)

- Collections of the above
	- arrays
	- lists
	- sets
	- (but not IEnumerable!)

- Anything else

	- any source type which .NET thinks is directly assignable to the target type



**Quick Rules -- what you *cannot* cast**

Unless the source type is directly assignable to the target type, we do not allow the following casts:

These will always fail:

- struct to class (really bad things happen if you do)
- class to struct (equally bad things happen)


These will sometimes work:

- from interfaces to anything (this is truly scary)
- to interfaces (this doesn't lead to any working results)

The interfaces will work *only* if the source type is assignable to the target in .NET world.
Basically if you can cast using regular means of C#, our dodgy cast will also work.



----

## Use cases and examples

A bunch of use cases, to give idea where and how this may be useful.


### Arrays of Enums

What a pain the `enums` are in C# when you need to convert to/from int and are dealing with arrays of them.

*(aside: this is where Magicast originally came from: enums and arrays of such)*

Use Magicast to convert arrays of enums to arrays of ints at zero cost.

This is probably quite a legitimate case for it.


	public enum FooEnum {one, two};

	var enumArray = new Foo[]{FooEnum.one, FooEnum.two};
	var intArray = VeryUnsafeCast<FooEnum[], int[]>.Cast(enumArray);


### Accessing and changing private fields without reflection

Might be very handy for testing, when you want to check the value of a private field
(or even assign to it) but reflection is too hard.

Probably another legitimate case for it.

	public class Foo
	{
		private string fieldA;
	}

	// Declare a type with same fields as Foo and make fields public
	public class Bar
	{
		public string fieldA;
	}

	// Magicast
	var foo = new Foo();
	var bar = VeryUnsafeCast<Foo, Bar>(foo);

	// Get to those fields!
	bar.fieldA = "hello!";



### Automap arrays to objects

This is the case when we have an array of stuff and we want to access elements
of array as fields/properties of some object. Normally lots of manual mapping 
is involved, copying of memory and other laborious things.

You can skip all that 

	var array = new string[]{"one", "two", "three"};

	// Declare a type with fields like this:
	class ArrayView
	{
		private object padding;  // required

		// Yep, it even works with auto properties
		// Yep, even with read-only properties
		public string ElementZero {get;}
		public string ElementOne {get;}
	}

	// Magicast
	var view = VeryUnsafeCast<string[], ArrayView>()

	// Access them array elements!
	var s0 = view.ElementZero;		// equivalent of array[0]
	var s1 = view.ElementZero;		// equivalent of array[1]



### Turn objects with get/set auto-properties to immutable objects like a ninja

Someone somewhere said that *immutability is good*. And I agree.

However, it is the right pain to write library code which returns immutable objects.
Because C# does not have very good support for this -- it's really too much work. 
And so we end up with everything being mutable. Nice.

Now you can do it very easily with Magicast.

This is probably one of the ligitimate uses.

How to do it:

	// Declare two types with same propeties: 
	//	- one for internal use with get/set properties
	//  - one for public use, just with get properties

	internal class FooInternal
	{
		public string AutoPropA {get; set;}
		public string AutoPropB {get; set;}
	}

	public class FooPublic
	{
		public string AutoPropA {get;}
		public string AutoPropB {get;}
	}


	// Implement your method like this.
	public FooPublic SomeMethod()
	{
		// Use FooInternal to build the object.
		// Use Magicast to turn it into immutable version when you are done.

		var foo = new FooInternal
		{
			AutoPropA = "some value",
			AutoPropB = "some other value",
		};

		var immutable = VeryUnsafeCast<FooInternal, FooPublic>.Cast(foo);

		return immutable;
	}


### Change the meaning of 'polymorphism'

Turn cats into dogs and persons into cats!

OK, this one is a bit silly.

	public class Cat
	{
		public string name; 

		public string SaySomethingLikeACat()
		{
			return "Meaeoew" + this.name;
		}
	}

	public class Dog
	{
		public string name;

		public string SaySomethingLikeADog()
		{
			return "Woof Woof " + this.name;
		}
	}

	var cat = new Cat
	{
		name = "fluffy cat"
	};

	var dog = VeryUnsafeCast<Cat, Dog>.Cast(cat);

	cat.SaySomethingLikeACat(); // --> Meaeoew fuluffy cat
	dog.SaySomethingLikeADog(); // --> Woof Woof fluffy cat



### All of the above but with arrays, lists etc

All these casts sould work just fine with collection of objects: arrays, lists etc.

So for example:

	// Arrays -- should just work
	var arrayOfFoo = new Foo[]{....};
	var arrayOfBar = VeryUnsafeCast<Foo[], Bar[]>.Cast(arrayOfFoo);


	// List -- should also work
	var listOfFoo = new List<Foo>{....};
	var listOfBar = VeryUnsafeCast<List<Foo>, List<Bar>>.Cast(listOfFoo);


	// Dictionary -- change of value types
	var mapOfStringToFoo = new Dictionary<string, Foo>{....};
	vaf mapOfStringToBar = VeryUnsafeCast<Dictionary<string, Foo>, Dictionary<string, Bar>>.Cast(mapOfStringToFoo);





## Suported .NET frameworks


Aiming to support frameworks:

New things:

- net46
- dnx451
- dnxcore50
- dotnet (.NET 5.0? Not entirely sure what this is).


Old things:

- net45
- net40


REALLY old things:

- net35
- net20


At the moment I've done limited testing on `dnxcore50` and `dnx451`. But definitely going to run
more thorough tests soon.



