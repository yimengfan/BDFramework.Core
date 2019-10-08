// Copyright (c) Rotorz Limited. All rights reserved.

using System;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Array or list fields that are serialized by Unity can be annotated with this
    /// attribute so that the collection is presented as a reorderable list in the
    /// Unity inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReorderableListAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderableListAttribute"/> class.
        /// </summary>
        /// <param name="elementsPropertyName">Name of the 'elements' property inside the
        /// serializable collection wrapper class.</param>
        /// <param name="droppableObjectType">The type of object that can be inserted
        /// into the reorderable list using drag and drop. When <see langword="null"/> no
        /// drop insertion is supported.</param>
        /// <param name="flags">Flags for the reorderable list field GUI.</param>
        public ReorderableListAttribute(string elementsPropertyName = "elements", Type droppableObjectType = null, ReorderableListFlags flags = 0)
        {
            this.ElementsPropertyName = elementsPropertyName;
            this.DroppableObjectType = droppableObjectType;
            this.Flags = flags;
        }


        /// <summary>
        /// Gets the name of the 'elements' property inside the serializable collection
        /// wrapper class.
        /// </summary>
        public string ElementsPropertyName { get; private set; }

        /// <summary>
        /// Gets the type of object that can be inserted into the reorderable list using
        /// drag and drop. When <see langword="null"/> no drop insertion is supported.
        /// </summary>
        public Type DroppableObjectType { get; private set; }

        /// <summary>
        /// Gets flags that are provided to the reorderable list field GUI.
        /// </summary>
        public ReorderableListFlags Flags { get; private set; }
    }
}
