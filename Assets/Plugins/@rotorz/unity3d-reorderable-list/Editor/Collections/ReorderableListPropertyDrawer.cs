// Copyright (c) Rotorz Limited. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <exclude/>
    [CustomPropertyDrawer(typeof(ReorderableListAttribute))]
    public sealed class ReorderableListPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attribute = (ReorderableListAttribute)this.attribute;
            var elementsProperty = property.FindPropertyRelative(attribute.ElementsPropertyName);
            var listAdaptor = new SerializedPropertyWithDropTargetAdaptor(elementsProperty, attribute.DroppableObjectType);

            float height = 0f;

            if (label != GUIContent.none) {
                height += 21f;
            }

            height += ReorderableListGUI.CalculateListFieldHeight(listAdaptor, attribute.Flags);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (ReorderableListAttribute)this.attribute;
            var elementsProperty = property.FindPropertyRelative(attribute.ElementsPropertyName);
            var listAdaptor = new SerializedPropertyWithDropTargetAdaptor(elementsProperty, attribute.DroppableObjectType);

            var titlePosition = new Rect(position.x, position.y, position.width, 0f);
            if (label != GUIContent.none) {
                titlePosition.height = 21f;
                ReorderableListGUI.Title(titlePosition, label);
            }

            var listPosition = new Rect(position.x, titlePosition.yMax - 1f, position.width, position.height - titlePosition.height);
            ReorderableListGUI.ListFieldAbsolute(listPosition, listAdaptor, attribute.Flags);
        }
    }
}
