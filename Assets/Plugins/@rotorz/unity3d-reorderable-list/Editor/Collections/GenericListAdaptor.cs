// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Reorderable list adaptor for generic list.
    /// </summary>
    /// <remarks>
    /// <para>This adaptor can be subclassed to add special logic to item height calculation.
    /// You may want to implement a custom adaptor class where specialised functionality
    /// is needed.</para>
    /// <para>List elements which implement the <see cref="System.ICloneable"/> interface are
    /// cloned using that interface upon duplication; otherwise the item value or reference is
    /// simply copied.</para>
    /// </remarks>
    /// <typeparam name="T">Type of list element.</typeparam>
    public class GenericListAdaptor<T> : IReorderableListAdaptor
    {
        private IList<T> list;
        private ReorderableListControl.ItemDrawer<T> itemDrawer;

        /// <summary>
        /// Fixed height of each list item.
        /// </summary>
        public float FixedItemHeight;


        /// <summary>
        /// Initializes a new instance of <see cref="GenericListAdaptor{T}"/>.
        /// </summary>
        /// <param name="list">The list which can be reordered.</param>
        /// <param name="itemDrawer">Callback to draw list item.</param>
        /// <param name="itemHeight">Height of list item in pixels.</param>
        public GenericListAdaptor(IList<T> list, ReorderableListControl.ItemDrawer<T> itemDrawer, float itemHeight)
        {
            this.list = list;
            this.itemDrawer = itemDrawer ?? ReorderableListGUI.DefaultItemDrawer;
            this.FixedItemHeight = itemHeight;
        }


        /// <summary>
        /// Gets the underlying list data structure.
        /// </summary>
        public IList<T> List {
            get { return this.list; }
        }

        /// <summary>
        /// Gets element from list.
        /// </summary>
        /// <param name="index">Zero-based index of element.</param>
        /// <returns>
        /// The element.
        /// </returns>
        public T this[int index] {
            get { return this.list[index]; }
        }


        /// <inheritdoc/>
        public int Count {
            get { return this.list.Count; }
        }


        /// <inheritdoc/>
        public virtual bool CanDrag(int index)
        {
            return true;
        }

        /// <inheritdoc/>
        public virtual bool CanRemove(int index)
        {
            return true;
        }


        /// <inheritdoc/>
        public virtual void Add()
        {
            this.list.Add(default(T));
        }

        /// <inheritdoc/>
        public virtual void Insert(int index)
        {
            this.list.Insert(index, default(T));
        }

        /// <inheritdoc/>
        public virtual void Duplicate(int index)
        {
            T newItem = this.list[index];

            ICloneable existingItem = newItem as ICloneable;
            if (existingItem != null) {
                newItem = (T)existingItem.Clone();
            }

            this.list.Insert(index + 1, newItem);
        }

        /// <inheritdoc/>
        public virtual void Remove(int index)
        {
            this.list.RemoveAt(index);
        }

        /// <inheritdoc/>
        public virtual void Move(int sourceIndex, int destIndex)
        {
            if (destIndex > sourceIndex) {
                --destIndex;
            }

            T item = this.list[sourceIndex];
            this.list.RemoveAt(sourceIndex);
            this.list.Insert(destIndex, item);
        }

        /// <inheritdoc/>
        public virtual void Clear()
        {
            this.list.Clear();
        }


        /// <inheritdoc/>
        public virtual void BeginGUI()
        {
        }

        /// <inheritdoc/>
        public virtual void EndGUI()
        {
        }


        /// <inheritdoc/>
        public virtual void DrawItemBackground(Rect position, int index)
        {
        }

        /// <inheritdoc/>
        public virtual void DrawItem(Rect position, int index)
        {
            this.list[index] = this.itemDrawer(position, this.list[index]);
        }

        /// <inheritdoc/>
        public virtual float GetItemHeight(int index)
        {
            return this.FixedItemHeight;
        }
    }
}
