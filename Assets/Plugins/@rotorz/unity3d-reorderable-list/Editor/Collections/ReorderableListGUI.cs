// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Utility class for drawing reorderable lists.
    /// </summary>
    public static class ReorderableListGUI
    {
        /// <summary>
        /// Default list item height is 18 pixels.
        /// </summary>
        public const float DefaultItemHeight = 18;

        /// <summary>
        /// Gets or sets the zero-based index of the last item that was changed. A value of -1
        /// indicates that no item was changed by list.
        /// </summary>
        /// <remarks>
        /// <para>This property should not be set when items are added or removed.</para>
        /// </remarks>
        public static int IndexOfChangedItem { get; internal set; }

        /// <summary>
        /// Gets the control ID of the list that is currently being drawn.
        /// </summary>
        public static int CurrentListControlID {
            get { return ReorderableListControl.CurrentListControlID; }
        }

        /// <summary>
        /// Gets the position of the list control that is currently being drawn.
        /// </summary>
        /// <remarks>
        /// <para>The value of this property should be ignored for <see cref="EventType.Layout"/>
        /// type events when using reorderable list controls with automatic layout.</para>
        /// </remarks>
        /// <see cref="CurrentItemTotalPosition"/>
        public static Rect CurrentListPosition {
            get { return ReorderableListControl.CurrentListPosition; }
        }

        /// <summary>
        /// Gets the zero-based index of the list item that is currently being drawn;
        /// or a value of -1 if no item is currently being drawn.
        /// </summary>
        public static int CurrentItemIndex {
            get { return ReorderableListControl.CurrentItemIndex; }
        }

        /// <summary>
        /// Gets the total position of the list item that is currently being drawn.
        /// </summary>
        /// <remarks>
        /// <para>The value of this property should be ignored for <see cref="EventType.Layout"/>
        /// type events when using reorderable list controls with automatic layout.</para>
        /// </remarks>
        /// <see cref="CurrentItemIndex"/>
        /// <see cref="CurrentListPosition"/>
        public static Rect CurrentItemTotalPosition {
            get { return ReorderableListControl.CurrentItemTotalPosition; }
        }

        #region Basic Item Drawers

        /// <summary>
        /// Default list item drawer implementation.
        /// </summary>
        /// <remarks>
        /// <para>Always presents the label "Item drawer not implemented.".</para>
        /// </remarks>
        /// <param name="position">Position to draw list item control(s).</param>
        /// <param name="item">Value of list item.</param>
        /// <returns>
        /// Unmodified value of list item.
        /// </returns>
        /// <typeparam name="T">Type of list item.</typeparam>
        public static T DefaultItemDrawer<T>(Rect position, T item)
        {
            GUI.Label(position, "Item drawer not implemented.");
            return item;
        }

        /// <summary>
        /// Draws text field allowing list items to be edited.
        /// </summary>
        /// <remarks>
        /// <para>Null values are automatically changed to empty strings since null
        /// values cannot be edited using a text field.</para>
        /// <para>Value of <c>GUI.changed</c> is set to <c>true</c> if value of item
        /// is modified.</para>
        /// </remarks>
        /// <param name="position">Position to draw list item control(s).</param>
        /// <param name="item">Value of list item.</param>
        /// <returns>
        /// Modified value of list item.
        /// </returns>
        public static string TextFieldItemDrawer(Rect position, string item)
        {
            if (item == null) {
                item = "";
                GUI.changed = true;
            }
            return EditorGUI.TextField(position, item);
        }

        #endregion

        /// <summary>
        /// Gets the default list control implementation.
        /// </summary>
        private static ReorderableListControl DefaultListControl { get; set; }

        static ReorderableListGUI()
        {
            DefaultListControl = new ReorderableListControl();

            // Duplicate default styles to prevent user scripts from interferring with
            // the default list control instance.
            DefaultListControl.ContainerStyle = new GUIStyle(ReorderableListStyles.Instance.Container);
            DefaultListControl.FooterButtonStyle = new GUIStyle(ReorderableListStyles.Instance.FooterButton);
            DefaultListControl.ItemButtonStyle = new GUIStyle(ReorderableListStyles.Instance.ItemButton);

            IndexOfChangedItem = -1;
        }

        private static GUIContent s_Temp = new GUIContent();

        #region Title Control

        /// <summary>
        /// Draw title control for list field.
        /// </summary>
        /// <remarks>
        /// <para>When needed, should be shown immediately before list field.</para>
        /// </remarks>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// ReorderableListGUI.Title(titleContent);
        /// ReorderableListGUI.ListField(list, DynamicListGU.TextFieldItemDrawer);
        /// ]]></code>
        /// </example>
        /// <param name="title">Content for title control.</param>
        public static void Title(GUIContent title)
        {
            Rect position = GUILayoutUtility.GetRect(title, ReorderableListStyles.Instance.Title);
            Title(position, title);
            GUILayout.Space(-1);
        }

        /// <summary>
        /// Draw title control for list field.
        /// </summary>
        /// <remarks>
        /// <para>When needed, should be shown immediately before list field.</para>
        /// </remarks>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// ReorderableListGUI.Title("Your Title");
        /// ReorderableListGUI.ListField(list, DynamicListGU.TextFieldItemDrawer);
        /// ]]></code>
        /// </example>
        /// <param name="title">Text for title control.</param>
        public static void Title(string title)
        {
            s_Temp.text = title;
            Title(s_Temp);
        }

        /// <summary>
        /// Draw title control for list field with absolute positioning.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="title">Content for title control.</param>
        public static void Title(Rect position, GUIContent title)
        {
            if (Event.current.type == EventType.Repaint)
                ReorderableListStyles.Instance.Title.Draw(position, title, false, false, false, false);
        }

        /// <summary>
        /// Draw title control for list field with absolute positioning.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="text">Text for title control.</param>
        public static void Title(Rect position, string text)
        {
            s_Temp.text = text;
            Title(position, s_Temp);
        }

        #endregion

        #region List<T> Control

        /// <summary>
        /// Draw list field control.
        /// </summary>
        /// <param name="list">The list which can be reordered.</param>
        /// <param name="drawItem">Callback to draw list item.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="itemHeight">Height of a single list item.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        /// <typeparam name="T">Type of list item.</typeparam>
        private static void DoListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmpty drawEmpty, float itemHeight, ReorderableListFlags flags)
        {
            var adaptor = new GenericListAdaptor<T>(list, drawItem, itemHeight);
            ReorderableListControl.DrawControlFromState(adaptor, drawEmpty, flags);
        }
        /// <summary>
        /// Draw list field control with absolute positioning.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="list">The list which can be reordered.</param>
        /// <param name="drawItem">Callback to draw list item.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="itemHeight">Height of a single list item.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        /// <typeparam name="T">Type of list item.</typeparam>
        private static void DoListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmptyAbsolute drawEmpty, float itemHeight, ReorderableListFlags flags)
        {
            var adaptor = new GenericListAdaptor<T>(list, drawItem, itemHeight);
            ReorderableListControl.DrawControlFromState(position, adaptor, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmpty drawEmpty, float itemHeight, ReorderableListFlags flags)
        {
            DoListField<T>(list, drawItem, drawEmpty, itemHeight, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmptyAbsolute drawEmpty, float itemHeight, ReorderableListFlags flags)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, drawEmpty, itemHeight, flags);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmpty drawEmpty, float itemHeight)
        {
            DoListField<T>(list, drawItem, drawEmpty, itemHeight, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmptyAbsolute drawEmpty, float itemHeight)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, drawEmpty, itemHeight, 0);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            DoListField<T>(list, drawItem, drawEmpty, DefaultItemHeight, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, drawEmpty, DefaultItemHeight, flags);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmpty drawEmpty)
        {
            DoListField<T>(list, drawItem, drawEmpty, DefaultItemHeight, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListControl.DrawEmptyAbsolute drawEmpty)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, drawEmpty, DefaultItemHeight, 0);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, float itemHeight, ReorderableListFlags flags)
        {
            DoListField<T>(list, drawItem, null, itemHeight, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, float itemHeight, ReorderableListFlags flags)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, null, itemHeight, flags);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, float itemHeight)
        {
            DoListField<T>(list, drawItem, null, itemHeight, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, float itemHeight)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, null, itemHeight, 0);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListFlags flags)
        {
            DoListField<T>(list, drawItem, null, DefaultItemHeight, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem, ReorderableListFlags flags)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, null, DefaultItemHeight, flags);
        }


        /// <inheritdoc cref="DoListField{T}(IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmpty, float, ReorderableListFlags)"/>
        public static void ListField<T>(IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem)
        {
            DoListField<T>(list, drawItem, null, DefaultItemHeight, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute{T}(Rect, IList{T}, ReorderableListControl.ItemDrawer{T}, ReorderableListControl.DrawEmptyAbsolute, float, ReorderableListFlags)"/>
        public static void ListFieldAbsolute<T>(Rect position, IList<T> list, ReorderableListControl.ItemDrawer<T> drawItem)
        {
            DoListFieldAbsolute<T>(position, list, drawItem, null, DefaultItemHeight, 0);
        }


        /// <summary>
        /// Calculate height of list field for absolute positioning.
        /// </summary>
        /// <param name="itemCount">Count of items in list.</param>
        /// <param name="itemHeight">Fixed height of list item.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        /// <returns>
        /// Required list height in pixels.
        /// </returns>
        public static float CalculateListFieldHeight(int itemCount, float itemHeight, ReorderableListFlags flags)
        {
            // We need to push/pop flags so that nested controls are properly calculated.
            var restoreFlags = DefaultListControl.Flags;
            try {
                DefaultListControl.Flags = flags;
                return DefaultListControl.CalculateListHeight(itemCount, itemHeight);
            }
            finally {
                DefaultListControl.Flags = restoreFlags;
            }
        }

        /// <inheritdoc cref="CalculateListFieldHeight(int, float, ReorderableListFlags)"/>
        public static float CalculateListFieldHeight(int itemCount, ReorderableListFlags flags)
        {
            return CalculateListFieldHeight(itemCount, DefaultItemHeight, flags);
        }
        /// <inheritdoc cref="CalculateListFieldHeight(int, float, ReorderableListFlags)"/>
        public static float CalculateListFieldHeight(int itemCount, float itemHeight)
        {
            return CalculateListFieldHeight(itemCount, itemHeight, 0);
        }
        /// <inheritdoc cref="CalculateListFieldHeight(int, float, ReorderableListFlags)"/>
        public static float CalculateListFieldHeight(int itemCount)
        {
            return CalculateListFieldHeight(itemCount, DefaultItemHeight, 0);
        }

        #endregion

        #region SerializedProperty Control

        /// <summary>
        /// Draw list field control for serializable property array.
        /// </summary>
        /// <param name="arrayProperty">Serializable property.</param>
        /// <param name="fixedItemHeight">Use fixed height for items rather than <see cref="UnityEditor.EditorGUI.GetPropertyHeight(SerializedProperty)"/>.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        private static void DoListField(SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            var adaptor = new SerializedPropertyAdaptor(arrayProperty, fixedItemHeight);
            ReorderableListControl.DrawControlFromState(adaptor, drawEmpty, flags);
        }
        /// <summary>
        /// Draw list field control for serializable property array.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="arrayProperty">Serializable property.</param>
        /// <param name="fixedItemHeight">Use fixed height for items rather than <see cref="UnityEditor.EditorGUI.GetPropertyHeight(SerializedProperty)"/>.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        private static void DoListFieldAbsolute(Rect position, SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            var adaptor = new SerializedPropertyAdaptor(arrayProperty, fixedItemHeight);
            ReorderableListControl.DrawControlFromState(position, adaptor, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            DoListField(arrayProperty, 0, drawEmpty, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, arrayProperty, 0, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, ReorderableListControl.DrawEmpty drawEmpty)
        {
            DoListField(arrayProperty, 0, drawEmpty, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, ReorderableListControl.DrawEmptyAbsolute drawEmpty)
        {
            DoListFieldAbsolute(position, arrayProperty, 0, drawEmpty, 0);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, ReorderableListFlags flags)
        {
            DoListField(arrayProperty, 0, null, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, arrayProperty, 0, null, flags);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty)
        {
            DoListField(arrayProperty, 0, null, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty)
        {
            DoListFieldAbsolute(position, arrayProperty, 0, null, 0);
        }


        /// <summary>
        /// Calculate height of list field for absolute positioning.
        /// </summary>
        /// <param name="arrayProperty">Serializable property.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        /// <returns>
        /// Required list height in pixels.
        /// </returns>
        public static float CalculateListFieldHeight(SerializedProperty arrayProperty, ReorderableListFlags flags)
        {
            // We need to push/pop flags so that nested controls are properly calculated.
            var restoreFlags = DefaultListControl.Flags;
            try {
                DefaultListControl.Flags = flags;
                return DefaultListControl.CalculateListHeight(new SerializedPropertyAdaptor(arrayProperty));
            }
            finally {
                DefaultListControl.Flags = restoreFlags;
            }
        }

        /// <inheritdoc cref="CalculateListFieldHeight(SerializedProperty, ReorderableListFlags)"/>
        public static float CalculateListFieldHeight(SerializedProperty arrayProperty)
        {
            return CalculateListFieldHeight(arrayProperty, 0);
        }

        #endregion

        #region SerializedProperty Control (Fixed Item Height)

        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            DoListField(arrayProperty, fixedItemHeight, drawEmpty, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, arrayProperty, fixedItemHeight, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmpty drawEmpty)
        {
            DoListField(arrayProperty, fixedItemHeight, drawEmpty, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListControl.DrawEmptyAbsolute drawEmpty)
        {
            DoListFieldAbsolute(position, arrayProperty, fixedItemHeight, drawEmpty, 0);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListFlags flags)
        {
            DoListField(arrayProperty, fixedItemHeight, null, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, float fixedItemHeight, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, arrayProperty, fixedItemHeight, null, flags);
        }


        /// <inheritdoc cref="DoListField(SerializedProperty, float, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(SerializedProperty arrayProperty, float fixedItemHeight)
        {
            DoListField(arrayProperty, fixedItemHeight, null, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, SerializedProperty, float, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, SerializedProperty arrayProperty, float fixedItemHeight)
        {
            DoListFieldAbsolute(position, arrayProperty, fixedItemHeight, null, 0);
        }

        #endregion

        #region Adaptor Control

        /// <summary>
        /// Draw list field control for adapted collection.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        private static void DoListField(IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags = 0)
        {
            ReorderableListControl.DrawControlFromState(adaptor, drawEmpty, flags);
        }
        /// <summary>
        /// Draw list field control for adapted collection.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Callback to draw custom content for empty list (optional).</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        private static void DoListFieldAbsolute(Rect position, IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags = 0)
        {
            ReorderableListControl.DrawControlFromState(position, adaptor, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField(IReorderableListAdaptor, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            DoListField(adaptor, drawEmpty, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, IReorderableListAdaptor, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, adaptor, drawEmpty, flags);
        }


        /// <inheritdoc cref="DoListField(IReorderableListAdaptor, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmpty drawEmpty)
        {
            DoListField(adaptor, drawEmpty, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, IReorderableListAdaptor, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, IReorderableListAdaptor adaptor, ReorderableListControl.DrawEmptyAbsolute drawEmpty)
        {
            DoListFieldAbsolute(position, adaptor, drawEmpty, 0);
        }


        /// <inheritdoc cref="DoListField(IReorderableListAdaptor, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(IReorderableListAdaptor adaptor, ReorderableListFlags flags)
        {
            DoListField(adaptor, null, flags);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, IReorderableListAdaptor, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, IReorderableListAdaptor adaptor, ReorderableListFlags flags)
        {
            DoListFieldAbsolute(position, adaptor, null, flags);
        }


        /// <inheritdoc cref="DoListField(IReorderableListAdaptor, ReorderableListControl.DrawEmpty, ReorderableListFlags)"/>
        public static void ListField(IReorderableListAdaptor adaptor)
        {
            DoListField(adaptor, null, 0);
        }
        /// <inheritdoc cref="DoListFieldAbsolute(Rect, IReorderableListAdaptor, ReorderableListControl.DrawEmptyAbsolute, ReorderableListFlags)"/>
        public static void ListFieldAbsolute(Rect position, IReorderableListAdaptor adaptor)
        {
            DoListFieldAbsolute(position, adaptor, null, 0);
        }


        /// <summary>
        /// Calculate height of list field for adapted collection.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        /// <returns>
        /// Required list height in pixels.
        /// </returns>
        public static float CalculateListFieldHeight(IReorderableListAdaptor adaptor, ReorderableListFlags flags)
        {
            // We need to push/pop flags so that nested controls are properly calculated.
            var restoreFlags = DefaultListControl.Flags;
            try {
                DefaultListControl.Flags = flags;
                return DefaultListControl.CalculateListHeight(adaptor);
            }
            finally {
                DefaultListControl.Flags = restoreFlags;
            }
        }

        /// <inheritdoc cref="CalculateListFieldHeight(IReorderableListAdaptor, ReorderableListFlags)"/>
        public static float CalculateListFieldHeight(IReorderableListAdaptor adaptor)
        {
            return CalculateListFieldHeight(adaptor, 0);
        }

        #endregion
    }
}
