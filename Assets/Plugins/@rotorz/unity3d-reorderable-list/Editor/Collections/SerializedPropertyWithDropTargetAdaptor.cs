// Copyright (c) Rotorz Limited. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// A reorderable list adaptor that adds drop insertion support to the standard
    /// <see cref="SerializedPropertyAdaptor"/> implementation.
    /// </summary>
    public class SerializedPropertyWithDropTargetAdaptor : SerializedPropertyAdaptor, IReorderableListDropTarget
    {
        private readonly Type droppableObjectType;


        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedPropertyWithDropTargetAdaptor"/> class.
        /// </summary>
        /// <param name="arrayProperty">Serialized property representing the array.</param>
        /// <param name="droppableObjectType">Type of <see cref="UnityEngine.Object"/>
        /// that can be drop inserted into the array.</param>
        public SerializedPropertyWithDropTargetAdaptor(SerializedProperty arrayProperty, Type droppableObjectType = null)
            : base(arrayProperty)
        {
            this.droppableObjectType = droppableObjectType;
        }


        /// <inheritdoc/>
        public virtual bool CanDropInsert(int insertionIndex)
        {
            if (this.droppableObjectType == null) {
                return false;
            }

            if (!ReorderableListControl.CurrentListPosition.Contains(Event.current.mousePosition)) {
                return false;
            }

            return this.GetDraggedObjects().Any();
        }

        /// <inheritdoc/>
        public virtual void ProcessDropInsertion(int insertionIndex)
        {
            if (this.droppableObjectType == null) {
                return;
            }

            if (Event.current.type == EventType.DragPerform) {
                var draggedItems = this.GetDraggedObjects().ToArray();

                if (draggedItems.Length > 0) {
                    // Allocate array elements for the dropped item(s).
                    for (int i = 0; i < draggedItems.Length; ++i) {
                        this.Insert(insertionIndex);
                    }

                    // Assign dropped item(s) to the new array elements.
                    for (int i = 0; i < draggedItems.Length; ++i) {
                        var elementProperty = this.ArrayProperty.GetArrayElementAtIndex(insertionIndex + i);
                        elementProperty.objectReferenceValue = draggedItems[i];
                    }

                    GUI.changed = true;
                }
            }
        }


        /// <summary>
        /// Gets the collection of <see cref="UnityEngine.Object"/> instances that are
        /// currently being dragged.
        /// </summary>
        /// <returns>
        /// An enumerable collection of zero-or-more objects.
        /// </returns>
        protected virtual IEnumerable<Object> GetDraggedObjects()
        {
            return DragAndDrop.objectReferences
                .Where(reference => reference != null && this.droppableObjectType.IsAssignableFrom(reference.GetType()));
        }
    }
}
