﻿using FrEee.Game.Objects.Abilities;
using FrEee.Game.Objects.Orders;
using FrEee.Game.Objects.Vehicles;
using FrEee.Modding;
using FrEee.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace FrEee.Tests.Utility
{
	[TestClass]
	public class SerializerTest
	{
		[ClassInitialize]
		public static void ClassInit(TestContext ctx)
		{
			// silly unit tests can't find their own assembly
			SafeType.ForceLoadType(typeof(SerializerTest));
			SafeType.ForceLoadType(typeof(Car));
			SafeType.ForceLoadType(typeof(Company));
		}

		[TestMethod]
		public void CircularReferences()
		{
			var george = new Person("George");
			var brad = new Person("Brad");
			george.Partner = brad;
			brad.Partner = george;
			var s = Serializer.SerializeToString(george);
			var george2 = Serializer.DeserializeFromString<Person>(s);
			Assert.AreEqual(george.ToString(), george2.ToString());
		}

		[TestMethod]
		public void MagicTypes()
		{
			var p = new System.Drawing.Point(6, 9);
			var s = Serializer.SerializeToString(p);
			var p2 = Serializer.DeserializeFromString(s);
			Assert.AreEqual(p, p2);
		}

		[TestMethod]
		public void Roundtrip()
		{
			var chevy = new Company("Chevrolet");
			var car = new Car(chevy, "HHR", 2007);
			var s = Serializer.SerializeToString(car);
			var car2 = Serializer.DeserializeFromString<Car>(s);
			Assert.AreEqual(car.ToString(), car2.ToString());
		}

		[TestMethod]
		public void Scalar()
		{
			var answer = 42;
			var s = Serializer.SerializeToString(answer);
			var answer2 = Serializer.DeserializeFromString(s);
			Assert.AreEqual(answer, answer2);
		}

		[TestMethod]
		public void NestedDictionary()
		{
			var dict = new SafeDictionary<string, SafeDictionary<string, string>>(true);
			dict["test"]["fred"] = "flintstone";
			var s = Serializer.SerializeToString(dict);
			var dict2 = Serializer.DeserializeFromString<SafeDictionary<string, SafeDictionary<string, string>>>(s);
			Assert.AreEqual(dict["test"]["fred"], dict2["test"]["fred"]);
		}

		[TestMethod]
		public void NestedCrossAssembly()
		{
			var list = new List<Formula<string>>();
			list.Add(new LiteralFormula<string>("hello"));
			var s = Serializer.SerializeToString(list);
			var list2 = Serializer.DeserializeFromString<List<Formula<string>>>(s);
			Assert.AreEqual(list.First(), list2.First());
		}

		[TestMethod]
		public void NestedCrossAssemblySafeTypeName()
		{
			var s = SafeType.GetShortTypeName(typeof(List<Formula<string>>));
			Assert.AreEqual("System.Collections.Generic.List`1[[FrEee.Modding.Formula`1[[System.String, mscorlib]], FrEee.Core]], mscorlib", s);
		}

		[TestMethod]
		public void LookUpComplexType()
		{
			{
				var tname = "System.Collections.Generic.List`1[[FrEee.Modding.Formula`1[[System.String, mscorlib]], FrEee.Core]], mscorlib";
				var st = new SafeType(tname);
				Assert.AreEqual(typeof(List<Formula<string>>), st.Type);
			}
			{
				var tname = "FrEee.Game.Objects.Orders.ConstructionOrder`2[[FrEee.Game.Objects.Vehicles.Ship, FrEee.Core],[FrEee.Game.Objects.Vehicles.Design`1[[FrEee.Game.Objects.Vehicles.Ship, FrEee.Core]], FrEee.Core";
				var st = new SafeType(tname);
				Assert.AreEqual(typeof(ConstructionOrder<Ship, Design<Ship>>), st.Type);
			}
		}

		[TestMethod]
		public void LegacyLookUpComplexType()
		{
			{
				var tname = "System.Collections.Generic.List`1[[FrEee.Game.Objects.Abilities.AbilityRule, FrEee.Core, Version=0.0.9.0, Culture=neutral, PublicKeyToken=null]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
				var st = new SafeType(tname);
				Assert.AreEqual(typeof(List<AbilityRule>), st.Type);
			}
			{
				var tname = "System.Collections.Generic.List`1[[FrEee.Modding.Formula`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], FrEee.Core, Version=0.0.9.0, Culture=neutral, PublicKeyToken=null]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
				var st = new SafeType(tname);
				Assert.AreEqual(typeof(List<Formula<string>>), st.Type);
			}
		}

		[TestMethod]
		public void LegacyLookUpArrays()
		{
			var tname = "FrEee.Modding.Script[], FrEee.Core, Version=0.0.9.0, Culture=neutral, PublicKeyToken=null";
			var st = new SafeType(tname);
			Assert.AreEqual(typeof(Script[]), st.Type);
		}

		private class Car
		{
			public Car(Company manufacturer, string model, int year)
			{
				Manufacturer = manufacturer;
				Model = model;
				Year = year;
			}

			public Company Manufacturer { get; private set; }

			public string Model { get; private set; }

			public int Year { get; private set; }

			public override string ToString() => $"{Year} {Manufacturer} {Model}";
		}

		private class Company
		{
			public Company(string name)
			{
				Name = name;
			}

			public string Name { get; set; }

			public override string ToString()
			{
				return Name;
			}
		}

		private class Person
		{
			public Person(string name)
			{
				Name = name;
			}

			public string Name { get; set; }

			public Person Partner { get; set; }

			public override string ToString() => $"{Name ?? "Nobody"}, whose partner is {Partner?.Name ?? "nobody"}";
		}
	}
}