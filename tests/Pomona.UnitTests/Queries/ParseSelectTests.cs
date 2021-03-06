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
using System.Linq.Expressions;
using NUnit.Framework;
using Pomona.CodeGen;
using Pomona.TestHelpers;

namespace Pomona.UnitTests.Queries
{
    [TestFixture]
    public class ParseSelectTests : QueryExpressionParserTestsBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            // This is needed to get parser to use same anonymous types as those in expected expressions.
            AnonymousTypeBuilder.ScanAssemblyForExistingAnonymousTypes(GetType().Assembly);
        }


        protected void ParseAndAssertSelect<TRet>(string selectExpr, Expression<Func<Dummy, TRet>> expected)
        {
            var actual = parser.ParseSelectList(typeof (Dummy), selectExpr);
            actual.AssertEquals(expected);
        }

        [Test]
        public void ParseSelectList_WithExplicitNames_ReturnsCorrectExpression()
        {
            ParseAndAssertSelect("Friend as Venn,Guid as LongRandomNumber",
                                 _this => new {Venn = _this.Friend, LongRandomNumber = _this.Guid});
        }

        [Test]
        public void ParseSelectList_WithImplicitName_ReturnsCorrectExpression()
        {
            ParseAndAssertSelect("Friend.Parent.Number,Parent", _this => new {_this.Friend.Parent.Number, _this.Parent});
        }
    }
}