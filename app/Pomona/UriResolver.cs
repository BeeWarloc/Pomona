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
using System.Text;

using Pomona.Common.TypeSystem;

namespace Pomona
{
    public class UriResolver : IUriResolver
    {
        private readonly IBaseUriProvider baseUriProvider;
        private readonly ITypeMapper typeMapper;


        public UriResolver(ITypeMapper typeMapper, IBaseUriProvider baseUriProvider)
        {
            if (typeMapper == null)
                throw new ArgumentNullException("typeMapper");
            if (baseUriProvider == null)
                throw new ArgumentNullException("baseUriProvider");
            this.typeMapper = typeMapper;
            this.baseUriProvider = baseUriProvider;
        }


        public ITypeMapper TypeMapper
        {
            get { return this.typeMapper; }
        }


        public virtual string RelativeToAbsoluteUri(string path)
        {
            return String.Format("{0}{1}", this.baseUriProvider.BaseUri, path);
        }


        public string GetUriFor(PropertySpec property, object entity)
        {
            return RelativeToAbsoluteUri(BuildRelativeUri(entity, property));
        }


        public string GetUriFor(object entity)
        {
            return RelativeToAbsoluteUri(BuildRelativeUri(entity, null));
        }


        private string BuildRelativeUri(object entity, PropertySpec property)
        {
            var sb = new StringBuilder();
            BuildRelativeUri(entity, property, sb);
            return sb.ToString();
        }


        private void BuildRelativeUri(object entity, PropertySpec property, StringBuilder sb)
        {
            var type = this.typeMapper.GetClassMapping(entity.GetType()) as ResourceType;
            if (type == null)
                throw new InvalidOperationException("Can only get Uri for a ResourceType.");

            type.AppendUri(entity, sb);

            if (property != null)
            {
                sb.Append('/');
                sb.Append(((PropertyMapping)property).UriName);
            }
        }
    }
}