﻿// MIT License
// 
// Copyright (c) 2016 Wojciech Nagórski
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System.Collections.Generic;
using System.IO;
using ExtendedXmlSerialization.Test.TestObject;
using ExtendedXmlSerialization.Test.TestObjectConfigs;
using Xunit;

namespace ExtendedXmlSerialization.Test
{
    public class SerializationSpecificationTest : BaseTest
    {
        public SerializationSpecificationTest()
        {
            Serializer.SerializationToolsFactory = new SimpleSerializationToolsFactory()
            {
                Configurations = new List<IExtendedXmlSerializerConfig>
                {
                    new TestClassInheritanceWithMigrationsBaseConfig(),
                    new TestClassInheritanceWithMigrationsAConfig(),
                    new TestClassInheritanceWithMigrationsBConfig()
                }
            };
        }
        private static readonly IExtendedXmlSerializerConfig
            Stream = new ExtendedXmlSerializerConfig<object>(type => type == typeof(MemoryStream)),
            Null = new ExtendedXmlSerializerConfig<object>(type => type == null),
            Eight = new ExtendedXmlSerializerConfig<object>(type => type.Name.Length == 8);

        [Fact]
        public void VerifySpecification()
        {
            var sut = new SimpleSerializationToolsFactory
            {
                Configurations = new List<IExtendedXmlSerializerConfig> {Null, Stream, Eight}
            };

            Assert.Equal(Stream, sut.GetConfiguration(typeof(MemoryStream)));
            Assert.Equal(Null, sut.GetConfiguration(null));
            Assert.Equal(Eight, sut.GetConfiguration(typeof(BaseTest)));
        }

        [Fact]
        public void SpecificationForInheritance()
        {
            var objA =
                Serializer.Deserialize<TestClassInheritanceWithMigrationsA>(@"<?xml version=""1.0"" encoding=""utf-8""?>
<TestClassInheritanceWithMigrationsA type=""ExtendedXmlSerialization.Test.TestObject.TestClassInheritanceWithMigrationsA"">
<Property>1</Property>
<OtherProperty>2</OtherProperty>
</TestClassInheritanceWithMigrationsA>");

            Assert.Equal(1, objA.ChangedProperty);
            Assert.Equal(2, objA.OtherChangedProperty);

            var objBase =
                Serializer.Deserialize<TestClassInheritanceWithMigrationsBase>(
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestClassInheritanceWithMigrationsA type=""ExtendedXmlSerialization.Test.TestObject.TestClassInheritanceWithMigrationsBase"">
<Property>1</Property>
</TestClassInheritanceWithMigrationsA>");

            Assert.Equal(1, objBase.ChangedProperty);

            var objB =
                Serializer.Deserialize<TestClassInheritanceWithMigrationsB>(@"<?xml version=""1.0"" encoding=""utf-8""?>
<TestClassInheritanceWithMigrationsA type=""ExtendedXmlSerialization.Test.TestObject.TestClassInheritanceWithMigrationsB"">
<Property>1</Property>
<ProprtyWithoutChanges>3</ProprtyWithoutChanges>
</TestClassInheritanceWithMigrationsA>");

            Assert.Equal(1, objB.ChangedProperty);
            Assert.Equal(3, objB.ProprtyWithoutChanges);
        }
    }
}