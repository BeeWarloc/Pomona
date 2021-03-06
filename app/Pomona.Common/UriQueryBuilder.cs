// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2013 Karsten Nikolai Strand
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
using System.Linq.Expressions;
using System.Text;

namespace Pomona.Common
{
    internal class UriQueryBuilder
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();

        public override string ToString()
        {
            return stringBuilder.ToString();
        }

        public void AppendParameter(string key, object value)
        {
            AppendQueryParameterStart(key);
            AppendEncodedQueryValue(value.ToString());
        }

        public void AppendExpressionParameter(string queryKey, LambdaExpression predicate,
                                              Func<string, string> transform = null)
        {
            var filterString = new QueryPredicateBuilder(predicate).ToString();

            if (transform != null)
                filterString = transform(filterString);

            AppendQueryParameterStart(queryKey);
            AppendEncodedQueryValue(filterString);
        }

        private void AppendQueryParameterStart(string queryKey)
        {
            if (stringBuilder.Length > 0)
                stringBuilder.Append('&');

            AppendEncodedQueryValue(queryKey);
            stringBuilder.Append('=');
        }


        private void AppendEncodedQueryValue(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var sb = stringBuilder;

            foreach (var b in bytes)
            {
                if (b == ' ')
                    sb.Append('+');
                else if (b < 128
                         &&
                         (char.IsLetterOrDigit((char) b) || b == '\'' || b == '.' || b == '~' || b == '-' || b == '_'
                          || b == ')' || b == '(' || b == ' ' || b == '$'))
                    sb.Append((char) b);
                else
                    sb.AppendFormat("%{0:X2}", b);
            }
        }
    }
}