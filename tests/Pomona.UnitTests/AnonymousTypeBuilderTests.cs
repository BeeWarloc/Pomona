﻿// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

using System;
using System.IO;
using NUnit.Framework;
using Pomona.CodeGen;

namespace Pomona.UnitTests
{
    [TestFixture]
    public class AnonymousTypeBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            propNames = new[] {"Foo", "Bar"};
            builder = new AnonymousTypeBuilder(propNames);
        }

        private struct FixedHash
        {
            private readonly int value;

            public FixedHash(int value)
            {
                this.value = value;
            }

            public int Value
            {
                get { return value; }
            }

            private bool Equals(FixedHash other)
            {
                return value == other.value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is FixedHash && Equals((FixedHash) obj);
            }

            public override int GetHashCode()
            {
                return value;
            }
        }

        private string[] propNames;
        private AnonymousTypeBuilder builder;


        private object CreateAnonObject<TFoo, TBar>(TFoo foo, TBar bar)
        {
            var type = BuildTypeAndLoad();
            var typeInstance = type.MakeGenericType(typeof (TFoo), typeof (TBar));
            var obj = Activator.CreateInstance(typeInstance, foo, bar);
            return obj;
        }


        private void AssertObjectHasPropWithValue<T>(object obj, string name, T expected)
        {
            var type = obj.GetType();
            var prop = type.GetProperty(name);
            Assert.That(prop, Is.Not.Null);
            Assert.That(prop.GetValue(obj, null), Is.EqualTo(expected));
        }


        private Type BuildTypeAndLoad()
        {
            var typeDef = builder.BuildAnonymousType();
            var memStream = new MemoryStream();
            typeDef.Module.Assembly.Write(memStream);
            var loadedAsm = AppDomain.CurrentDomain.Load(memStream.ToArray());
            return loadedAsm.GetType(typeDef.Name);
        }


        [Test]
        public void CanConstructObject()
        {
            Assert.DoesNotThrow(() => CreateAnonObject(1337, "hello"));
        }


        [Test]
        public void CanSetPropertiesAndReadBack()
        {
            var obj = CreateAnonObject(1337, "hello");

            AssertObjectHasPropWithValue(obj, "Foo", 1337);
            AssertObjectHasPropWithValue(obj, "Bar", "hello");
        }


        [Test]
        public void EqualsReturnsFalseForDifferentObjectType()
        {
            var obj = CreateAnonObject(1337, "hello");
            Assert.That(obj.Equals("whatever"), Is.False);
        }


        [Test]
        public void EqualsReturnsFalseForSameTypeWithDifferentValues()
        {
            var obj = CreateAnonObject(1337, "hello");
            var objOther = Activator.CreateInstance(obj.GetType(), 42, "hello");
            Assert.That(obj.Equals(objOther), Is.False);
        }


        [Test]
        public void EqualsReturnsTrueForSameTypeWithSameValues()
        {
            var obj = CreateAnonObject(1337, "hello");
            var objOther = Activator.CreateInstance(obj.GetType(), 1337, "hello");
            Assert.That(obj.Equals(objOther), Is.True);
        }


        [Test]
        public void GetHashCodeReturnsExpectedValue()
        {
            var runtimeGenerated = CreateAnonObject(new FixedHash(1337), new FixedHash(0xdead));
            Assert.That(runtimeGenerated.GetHashCode(), Is.EqualTo(-2040804512));
        }


        [Test]
        public void ToStringReturnsHasCorrectFormatting()
        {
            var obj = CreateAnonObject(1337, "hello");
            var str = obj.ToString();
            Assert.That(str, Is.EqualTo("{ Foo = 1337, Bar = hello }"));
        }


        [Test]
        [Ignore]
        public void WriteAssemblyToFileForDebugging()
        {
            var tb = new AnonymousTypeBuilder(new[] {"Foo", "Bar"});
            var def = tb.BuildAnonymousType();
            def.Module.Assembly.Write("tempasm.dll");

            Assert.Fail("TODO: Remove me");
        }
    }
}