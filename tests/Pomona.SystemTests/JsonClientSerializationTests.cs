﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
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

#endregion

using System;

using Critters.Client;

using Newtonsoft.Json.Linq;

using NSubstitute;

using NUnit.Framework;

using Pomona.Common;
using Pomona.Common.Internals;
using Pomona.Common.Serialization;
using Pomona.Common.Serialization.Json;
using Pomona.TestHelpers;

namespace Pomona.SystemTests
{
    [TestFixture]
    public class JsonClientSerializationTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            this.typeMapper = new ClientTypeMapper(typeof(Client).Assembly);
        }

        #endregion

        private ClientTypeMapper typeMapper;


        private JObject SerializeAndGetJsonObject<T>(T value)
        {
            var serializerFactory =
                new PomonaJsonSerializerFactory(new ClientSerializationContextProvider(this.typeMapper,
                    Substitute.For<IPomonaClient>()));
            var serializer = serializerFactory.GetSerializer();
            Console.WriteLine("Serialized object to json:");
            var jsonString = serializer.SerializeToString(value);
            Console.WriteLine(jsonString);

            return (JObject)JToken.Parse(jsonString);
        }


        [Test]
        public void SerializeClassWithObjectProperty_PropertyGotIntValue_ReturnsCorrectJson()
        {
            var obj = new HasObjectPropertyForm();
            obj.FooBar = 1337;
            var jobject = SerializeAndGetJsonObject(obj);

            var fooBarBox = jobject.AssertHasPropertyWithObject("fooBar");
            Assert.That(fooBarBox.AssertHasPropertyWithString("_type"), Is.EqualTo("Int32"));
            Assert.That(fooBarBox.AssertHasPropertyWithInteger("value"), Is.EqualTo(1337));
        }


        [Test]
        public void SerializeCritterForm_WithReferences()
        {
            var critterForm = new CritterForm
            {
                Name = "Sheep",
                CrazyValue = new CrazyValueObjectForm { Info = "blblbobobo", Sickness = "whawhahaha" }
            };

            critterForm.Weapons.Add(new GunForm());

            var jobject = SerializeAndGetJsonObject(critterForm);
            Assert.That(jobject.AssertHasPropertyWithString("name"), Is.EqualTo("Sheep"));
            jobject.AssertHasPropertyWithObject("crazyValue");
        }


        [Test]
        public void SerializeStringToObjectDictionary_ReturnsCorrectJson()
        {
            var dictContainer = new StringToObjectDictionaryContainerForm();
            dictContainer.Map["foo"] = 1234;
            dictContainer.Map["bar"] = "hoho";

            var jobj = SerializeAndGetJsonObject(dictContainer);

            var mapJobj = jobj.AssertHasPropertyWithObject("map");
            var fooBox = mapJobj.AssertHasPropertyWithObject("foo");
            Assert.That(mapJobj.AssertHasPropertyWithString("bar"), Is.EqualTo("hoho"));

            fooBox.AssertHasPropertyWithValue("_type", "Int32");
            fooBox.AssertHasPropertyWithValue("value", 1234);
        }
    }
}