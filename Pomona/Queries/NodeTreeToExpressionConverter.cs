#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright � 2012 Karsten Nikolai Strand
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
using System.Linq;
using System.Linq.Expressions;

namespace Pomona.Queries
{
    public class NodeTreeToExpressionConverter<T>
    {
        private readonly IQueryPropertyResolver propertyResolver;

        private ParameterExpression thisParam;


        public NodeTreeToExpressionConverter(IQueryPropertyResolver propertyResolver)
        {
            if (propertyResolver == null)
                throw new ArgumentNullException("propertyResolver");
            this.propertyResolver = propertyResolver;
        }


        public Expression ParseExpression(NodeBase node)
        {
            if (node.NodeType == NodeType.Symbol)
                return ResolveSymbolNode((SymbolNode)node);

            var binaryOperatorNode = node as BinaryOperator;

            if (binaryOperatorNode != null)
            {
                var leftChild = ParseExpression(binaryOperatorNode.Left);
                var rightChild = ParseExpression(binaryOperatorNode.Right);

                switch (binaryOperatorNode.NodeType)
                {
                    case NodeType.AndAlso:
                        return Expression.AndAlso(leftChild, rightChild);
                    case NodeType.OrElse:
                        return Expression.OrElse(leftChild, rightChild);
                    case NodeType.Add:
                        return Expression.Add(leftChild, rightChild);
                    case NodeType.Subtract:
                        return Expression.Subtract(leftChild, rightChild);
                    case NodeType.Multiply:
                        return Expression.Multiply(leftChild, rightChild);
                    case NodeType.Div:
                        return Expression.Divide(leftChild, rightChild);
                    case NodeType.Equal:
                        return Expression.Equal(leftChild, rightChild);
                    case NodeType.LessThan:
                        return Expression.LessThan(leftChild, rightChild);
                    case NodeType.GreaterThan:
                        return Expression.GreaterThan(leftChild, rightChild);
                    case NodeType.GreaterThanOrEqual:
                        return Expression.GreaterThanOrEqual(leftChild, rightChild);
                    case NodeType.LessThanOrEqual:
                        return Expression.LessThanOrEqual(leftChild, rightChild);
                }
            }

            if (node.NodeType == NodeType.GuidLiteral)
            {
                var guidNode = (GuidNode)node;
                return Expression.Constant(guidNode.Value);
            }

            if (node.NodeType == NodeType.DateTimeLiteral)
            {
                var dateTimeNode = (DateTimeNode)node;
                return Expression.Constant(dateTimeNode.Value);
            }

            if (node.NodeType == NodeType.StringLiteral)
            {
                var stringNode = (StringNode)node;
                return Expression.Constant(stringNode.Value);
            }

            if (node.NodeType == NodeType.IntLiteral)
            {
                var intNode = (IntNode)node;
                return Expression.Constant(intNode.Value);
            }

            throw new NotImplementedException();
        }


        public Expression<Func<T, bool>> ToLambdaExpression(NodeBase node)
        {
            try
            {
                this.thisParam = Expression.Parameter(typeof(T), "x");
                return Expression.Lambda<Func<T, bool>>(ParseExpression(node), this.thisParam);
            }
            finally
            {
                this.thisParam = null;
            }
        }


        private Expression ResolveSymbolNode(SymbolNode node)
        {
            if (node.HasArguments)
            {
                Expression expression;
                if (TryResolveOdataExpression(node, out expression))
                    return expression;
                throw new InvalidOperationException("Unable to resolve property");
            }
            else
                return this.propertyResolver.Resolve<T>(this.thisParam, node.Name);
        }


        private bool TryResolveOdataExpression(SymbolNode node, out Expression expression)
        {
            expression = null;

            List<Expression> argsExpressions;
            switch (node.Name)
            {
                case "substringof":
                    var stringContainsMethod = typeof(string).GetMethod("Contains");
                    argsExpressions = node.Children.Select(ParseExpression).ToList();
                    expression = Expression.Call(argsExpressions[1], stringContainsMethod, argsExpressions[0]);
                    return true;

                case "startswith":
                    var stringStartsWithMethod = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
                    argsExpressions = node.Children.Select(ParseExpression).ToList();
                    expression = Expression.Call(argsExpressions[0], stringStartsWithMethod, argsExpressions[1]);
                    return true;
            }

            return false;
        }
    }
}