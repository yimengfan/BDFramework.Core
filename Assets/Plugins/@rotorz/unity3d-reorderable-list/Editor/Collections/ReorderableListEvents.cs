// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.ComponentModel;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Arguments which are passed to <see cref="AddMenuClickedEventHandler"/>.
    /// </summary>
    public sealed class AddMenuClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemMovedEventArgs"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="buttonPosition">Position of the add menu button.</param>
        public AddMenuClickedEventArgs(IReorderableListAdaptor adaptor, Rect buttonPosition)
        {
            this.Adaptor = adaptor;
            this.ButtonPosition = buttonPosition;
        }


        /// <summary>
        /// Gets adaptor to reorderable list container.
        /// </summary>
        public IReorderableListAdaptor Adaptor { get; private set; }
        /// <summary>
        /// Gets position of the add menu button.
        /// </summary>
        public Rect ButtonPosition { get; internal set; }
    }


    /// <summary>
    /// An event handler which is invoked when the "Add Menu" button is clicked.
    /// </summary>
    /// <param name="sender">Object which raised event.</param>
    /// <param name="args">Event arguments.</param>
    public delegate void AddMenuClickedEventHandler(object sender, AddMenuClickedEventArgs args);


    /// <summary>
    /// Arguments which are passed to <see cref="ItemInsertedEventHandler"/>.
    /// </summary>
    public sealed class ItemInsertedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemInsertedEventArgs"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        /// <param name="wasDuplicated">Indicates if inserted item was duplicated from another item.</param>
        public ItemInsertedEventArgs(IReorderableListAdaptor adaptor, int itemIndex, bool wasDuplicated)
        {
            this.Adaptor = adaptor;
            this.ItemIndex = itemIndex;
            this.WasDuplicated = wasDuplicated;
        }


        /// <summary>
        /// Gets adaptor to reorderable list container which contains element.
        /// </summary>
        public IReorderableListAdaptor Adaptor { get; private set; }
        /// <summary>
        /// Gets zero-based index of item which was inserted.
        /// </summary>
        public int ItemIndex { get; private set; }

        /// <summary>
        /// Indicates if inserted item was duplicated from another item.
        /// </summary>
        public bool WasDuplicated { get; private set; }
    }


    /// <summary>
    /// An event handler which is invoked after new list item is inserted.
    /// </summary>
    /// <param name="sender">Object which raised event.</param>
    /// <param name="args">Event arguments.</param>
    public delegate void ItemInsertedEventHandler(object sender, ItemInsertedEventArgs args);


    /// <summary>
    /// Arguments which are passed to <see cref="ItemRemovingEventHandler"/>.
    /// </summary>
    public sealed class ItemRemovingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemRemovingEventArgs"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        public ItemRemovingEventArgs(IReorderableListAdaptor adaptor, int itemIndex)
        {
            this.Adaptor = adaptor;
            this.ItemIndex = itemIndex;
        }


        /// <summary>
        /// Gets adaptor to reorderable list container which contains element.
        /// </summary>
        public IReorderableListAdaptor Adaptor { get; private set; }
        /// <summary>
        /// Gets zero-based index of item which is being removed.
        /// </summary>
        public int ItemIndex { get; internal set; }
    }


    /// <summary>
    /// An event handler which is invoked before a list item is removed.
    /// </summary>
    /// <remarks>
    /// <para>Item removal can be cancelled by setting <see cref="CancelEventArgs.Cancel"/>
    /// to <c>true</c>.</para>
    /// </remarks>
    /// <param name="sender">Object which raised event.</param>
    /// <param name="args">Event arguments.</param>
    public delegate void ItemRemovingEventHandler(object sender, ItemRemovingEventArgs args);


    /// <summary>
    /// Arguments which are passed to <see cref="ItemMovingEventHandler"/>.
    /// </summary>
    public sealed class ItemMovingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemMovingEventArgs"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        /// <param name="destinationItemIndex">Xero-based index of item destination.</param>
        public ItemMovingEventArgs(IReorderableListAdaptor adaptor, int itemIndex, int destinationItemIndex)
        {
            this.Adaptor = adaptor;
            this.ItemIndex = itemIndex;
            this.DestinationItemIndex = destinationItemIndex;
        }


        /// <summary>
        /// Gets adaptor to reorderable list container which contains element.
        /// </summary>
        public IReorderableListAdaptor Adaptor { get; private set; }

        /// <summary>
        /// Gets current zero-based index of item which is going to be moved.
        /// </summary>
        public int ItemIndex { get; internal set; }

        /// <summary>
        /// Gets the new candidate zero-based index for the item.
        /// </summary>
        /// <seealso cref="NewItemIndex"/>
        public int DestinationItemIndex { get; internal set; }

        /// <summary>
        /// Gets zero-based index of item <strong>after</strong> it has been moved.
        /// </summary>
        /// <seealso cref="DestinationItemIndex"/>
        public int NewItemIndex {
            get {
                int result = this.DestinationItemIndex;
                if (result > this.ItemIndex) {
                    --result;
                }
                return result;
            }
        }
    }


    /// <summary>
    /// An event handler which is invoked before a list item is moved.
    /// </summary>
    /// <remarks>
    /// <para>Moving of item can be cancelled by setting <see cref="CancelEventArgs.Cancel"/>
    /// to <c>true</c>.</para>
    /// </remarks>
    /// <param name="sender">Object which raised event.</param>
    /// <param name="args">Event arguments.</param>
    public delegate void ItemMovingEventHandler(object sender, ItemMovingEventArgs args);


    /// <summary>
    /// Arguments which are passed to <see cref="ItemMovedEventHandler"/>.
    /// </summary>
    public sealed class ItemMovedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ItemMovedEventArgs"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="oldItemIndex">Old zero-based index of item.</param>
        /// <param name="newItemIndex">New zero-based index of item.</param>
        public ItemMovedEventArgs(IReorderableListAdaptor adaptor, int oldItemIndex, int newItemIndex)
        {
            this.Adaptor = adaptor;
            this.OldItemIndex = oldItemIndex;
            this.NewItemIndex = newItemIndex;
        }


        /// <summary>
        /// Gets adaptor to reorderable list container which contains element.
        /// </summary>
        public IReorderableListAdaptor Adaptor { get; private set; }

        /// <summary>
        /// Gets old zero-based index of the item which was moved.
        /// </summary>
        public int OldItemIndex { get; internal set; }

        /// <summary>
        /// Gets new zero-based index of the item which was moved.
        /// </summary>
        public int NewItemIndex { get; internal set; }
    }


    /// <summary>
    /// An event handler which is invoked after a list item is moved.
    /// </summary>
    /// <param name="sender">Object which raised event.</param>
    /// <param name="args">Event arguments.</param>
    public delegate void ItemMovedEventHandler(object sender, ItemMovedEventArgs args);
}
