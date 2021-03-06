﻿#region License

// ----------------------------------------------------------------------------
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

#endregion

using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Pomona.Common.TypeSystem;

namespace Pomona
{
#if false
    public class EnumType : TypeSpec
    {
        private readonly Dictionary<string, int> enumValues;
        private readonly Type mappedType;

        private readonly TypeMapper typeMapper;


        public EnumType(Type mappedType, TypeMapper typeMapper, Dictionary<string, int> enumValues)
        {
            if (mappedType == null)
                throw new ArgumentNullException("targetType");
            if (typeMapper == null)
                throw new ArgumentNullException("typeMapper");
            if (enumValues == null)
                throw new ArgumentNullException("enumValues");

            if (!mappedType.IsEnum)
                throw new ArgumentException("Type is not enum.", "targetType");

            this.mappedType = mappedType;
            this.typeMapper = typeMapper;
            this.enumValues = enumValues;
        }

        #region TypeSpec Members

        public TypeSpec BaseType
        {
            get { return null; }
        }

        public Type CustomClientLibraryType { get; set; }

        public TypeSpec ElementType
        {
            get { throw new NotSupportedException(); }
        }

        public IList<TypeSpec> GenericArguments
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsAlwaysExpanded
        {
            get { return true; }
        }

        public bool IsBasicWireType
        {
            get { return true; }
        }

        public bool IsCollection
        {
            get { return false; }
        }

        public bool IsDictionary
        {
            get { return false; }
        }

        public bool IsGenericType
        {
            get { return false; }
        }

        public bool IsGenericTypeDefinition
        {
            get { return false; }
        }

        public bool IsNullable
        {
            get { return false; }
        }

        public bool IsValueType
        {
            get { return false; }
        }

        public JsonConverter JsonConverter
        {
            get { return new StringEnumConverter(); }
        }

        public bool MappedAsValueObject
        {
            get { return false; }
        }

        public Type MappedTypeInstance
        {
            get { return this.mappedType; }
        }

        public string Name
        {
            get { return this.mappedType.Name; }
        }

        public string PluralName
        {
            get { return null; }
        }

        public PropertySpec PrimaryId
        {
            get { return null; }
        }

        TypeSpec TypeSpec.DictionaryKeyType
        {
            get { throw new NotSupportedException(); }
        }

        TypeSpec TypeSpec.DictionaryType
        {
            get { throw new NotSupportedException(); }
        }

        TypeSpec TypeSpec.DictionaryValueType
        {
            get { throw new NotSupportedException(); }
        }

        bool TypeSpec.HasUri
        {
            get { throw new NotSupportedException(); }
        }

        IList<PropertySpec> TypeSpec.Properties
        {
            get { return new PropertySpec[] { }; }
        }

        TypeSerializationMode TypeSpec.SerializationMode
        {
            get { return TypeSerializationMode.Value; }
        }


        public object Create(IDictionary<PropertySpec, object> args)
        {
            throw new NotImplementedException();
        }

        #endregion

        public Dictionary<string, int> EnumValues
        {
            get { return this.enumValues; }
        }

        public Type MappedType
        {
            get { return this.mappedType; }
        }
    }
#endif
}