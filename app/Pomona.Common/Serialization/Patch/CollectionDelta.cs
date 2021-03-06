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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Pomona.Common.Internals;
using Pomona.Common.TypeSystem;

namespace Pomona.Common.Serialization.Patch
{
    public class CollectionDelta : Delta, ICollectionDelta
    {
        // Has set semantics for now, does not keep array order

        private static readonly MethodInfo addOriginalItem =
            ReflectionHelper.GetMethodDefinition<CollectionDelta<object, IEnumerable>>(
                x => x.AddOriginalItem<object>(null));

        private static readonly MethodInfo removeOriginalItem =
            ReflectionHelper.GetMethodDefinition<CollectionDelta<object, IEnumerable>>(
                x => x.RemoveOriginalItem<object>(null));

        private bool originalItemsLoaded;
        private IList<object> trackedItems = new List<object>();


        public CollectionDelta(object original, TypeSpec type, ITypeMapper typeMapper, Delta parent = null)
            : base(original, type, typeMapper, parent)
        {
        }


        protected CollectionDelta()
        {
        }


        public IEnumerable<object> AddedItems
        {
            get { return TrackedItems.Where(x => !(x is Delta)).Except(OriginalItems); }
        }

        public IEnumerable<Delta> ModifiedItems
        {
            get { return TrackedItems.OfType<Delta>().Where(x => x.IsDirty); }
        }

        public IEnumerable<object> RemovedItems
        {
            get { return OriginalItems.Except(TrackedOriginalItems); }
        }

        protected IEnumerable<object> OriginalItems
        {
            get { return ((IEnumerable)Original).Cast<object>(); }
        }

        protected IList<object> TrackedItems
        {
            get
            {
                if (!this.originalItemsLoaded)
                {
                    this.originalItemsLoaded = true;
                    foreach (var item in CreateItemsWrapper())
                        this.trackedItems.Add(item);
                }
                return this.trackedItems;
            }
        }

        protected IEnumerable<object> TrackedOriginalItems
        {
            get
            {
                return TrackedItems.Select(x =>
                {
                    var delta = x as Delta;
                    if (delta != null)
                        return delta.Original;
                    return x;
                });
            }
        }


        public override void Apply()
        {
            var nonGenericList = Original as IList;

            // Important to cache these, if not a "collection modified during enumeration" exception will be thrown.
            var removedItems = RemovedItems.ToList();
            var addedItems = AddedItems.ToList();
            var modifiedItems = ModifiedItems.ToList();
            foreach (var item in removedItems)
            {
                if (nonGenericList != null)
                    nonGenericList.Remove(item);
                else
                {
                    removeOriginalItem.MakeGenericMethod(Type.ElementType.Type)
                        .Invoke(this, new[] { item });
                }
            }
            foreach (var item in addedItems)
            {
                if (nonGenericList != null)
                    nonGenericList.Add(item);
                else
                {
                    addOriginalItem.MakeGenericMethod(Type.ElementType.Type)
                        .Invoke(this, new[] { item });
                }
            }
            foreach (var item in modifiedItems)
                item.Apply();
            if (Parent == null)
                Reset();
        }


        public override void Reset()
        {
            if (!this.originalItemsLoaded)
            {
                // No nested deltas possible when original items has not been loaded
                this.trackedItems.Clear();
            }
            else
            {
                // Only keep delta proxies, but reset them
                this.trackedItems = TrackedItems.Where(x => x is Delta).ToList();
                foreach (var item in this.trackedItems.OfType<Delta>())
                    item.Reset();
            }
            base.Reset();
        }


        public void AddItem(object item)
        {
            this.trackedItems.Add(item);
            SetDirty();
        }


        public void RemoveItem(object item)
        {
            this.trackedItems.Remove(item);
            DetachFromParent(item);
            SetDirty();
        }


        internal static object CreateTypedCollectionDelta(object original,
            TypeSpec type,
            ITypeMapper typeMapper,
            Delta parent)
        {
            var collectionType = typeof(CollectionDelta<,>).MakeGenericType(type.ElementType.Type,
                type.Type);
            return Activator.CreateInstance(collectionType, original, type, typeMapper, parent);
        }


        private void AddOriginalItem<T>(object item)
        {
            ((ICollection<T>)Original).Add((T)item);
        }


        private IEnumerable<object> CreateItemsWrapper()
        {
            foreach (var origItem in (IEnumerable)Original)
            {
                if (origItem == null)
                    yield return null;
                else
                {
                    var origItemType = TypeMapper.GetClassMapping(origItem.GetType());
                    if (origItemType.SerializationMode == TypeSerializationMode.Complex)
                        yield return CreateNestedDelta(origItem, origItemType);
                }
            }
        }


        private void RemoveOriginalItem<T>(object item)
        {
            ((ICollection<T>)Original).Remove((T)item);
        }
    }

    public abstract class CollectionDelta<TElement> : CollectionDelta, IList<TElement>
    {
        public CollectionDelta(object original, TypeSpec type, ITypeMapper typeMapper, Delta parent = null)
            : base(original, type, typeMapper, parent)
        {
            if (!type.IsCollection)
                throw new ArgumentException("Original value must be collection type!");
        }


        public TElement this[int index]
        {
            get { return (TElement)TrackedItems[index]; }
            set
            {
                var oldItem = TrackedItems[index];
                if (oldItem == (object)value)
                    return;
                DetachFromParent(oldItem);
                TrackedItems[index] = value;
                SetDirty();
            }
        }

        public new IEnumerable<TElement> AddedItems
        {
            get { return base.AddedItems.Cast<TElement>(); }
        }

        public int Count
        {
            get { return TrackedItems.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public new IEnumerable<TElement> ModifiedItems
        {
            get { return base.ModifiedItems.Cast<TElement>(); }
        }

        public new IEnumerable<TElement> RemovedItems
        {
            get { return base.RemovedItems.Cast<TElement>(); }
        }


        public void Add(TElement item)
        {
            AddItem(item);
        }


        public void Clear()
        {
            TrackedItems.Clear();
            SetDirty();
        }


        public bool Contains(TElement item)
        {
            return TrackedItems.Contains(item);
        }


        public void CopyTo(TElement[] array, int arrayIndex)
        {
            TrackedItems.Cast<TElement>().ToList().CopyTo(array, arrayIndex);
        }


        public IEnumerator<TElement> GetEnumerator()
        {
            return TrackedItems.Cast<TElement>().GetEnumerator();
        }


        public int IndexOf(TElement item)
        {
            return TrackedItems.IndexOf(item);
        }


        public void Insert(int index, TElement item)
        {
            TrackedItems.Insert(index, item);
            SetDirty();
        }


        public bool Remove(TElement item)
        {
            RemoveItem(item);
            SetDirty();
            return true;
        }


        public void RemoveAt(int index)
        {
            TrackedItems.RemoveAt(index);
            SetDirty();
        }


        protected override object CreateNestedDelta(object propValue, TypeSpec propValueType)
        {
            return ObjectDeltaProxyBase.CreateDeltaProxy(propValue, propValueType, TypeMapper, this);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class CollectionDelta<TElement, TCollection> : CollectionDelta<TElement>, IDelta<TCollection>
    {
        public CollectionDelta(object original, TypeSpec type, ITypeMapper typeMapper, Delta parent = null)
            : base(original, type, typeMapper, parent)
        {
        }


        public new TCollection Original
        {
            get { return (TCollection)base.Original; }
        }
    }
}