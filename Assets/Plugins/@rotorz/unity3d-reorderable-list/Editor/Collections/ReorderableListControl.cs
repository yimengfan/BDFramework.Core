// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.UnityEditorExtensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Base class for custom reorderable list control.
    /// </summary>
    public class ReorderableListControl
    {
        /// <summary>
        /// Invoked to draw list item.
        /// </summary>
        /// <remarks>
        /// <para>GUI controls must be positioned absolutely within the given rectangle since
        /// list items must be sized consistently.</para>
        /// </remarks>
        /// <example>
        /// <para>The following listing presents a text field for each list item:</para>
        /// <code language="csharp"><![CDATA[
        /// using Rotorz.Games.Collections;
        /// using System.Collections.Generic;
        /// using UnityEditor;
        /// using UnityEngine;
        /// 
        /// public class ExampleWindow : EditorWindow
        /// {
        ///     public List<string> wishlist = new List<string>();
        /// 
        /// 
        ///     private void OnGUI()
        ///     {
        ///         ReorderableListGUI.ListField(this.wishlist, DrawListItem);
        ///     }
        /// 
        /// 
        ///     private string DrawListItem(Rect position, string value)
        ///     {
        ///         // Text fields do not like `null` values!
        ///         if (value == null) {
        ///             value = "";
        ///         }
        ///         return EditorGUI.TextField(position, value);
        ///     }
        /// }
        /// ]]></code>
        /// </example>
        /// <typeparam name="T">Type of item list.</typeparam>
        /// <param name="position">Position of list item.</param>
        /// <param name="item">The list item.</param>
        /// <returns>
        /// The modified value.
        /// </returns>
        public delegate T ItemDrawer<T>(Rect position, T item);

        /// <summary>
        /// Invoked to draw content for empty list.
        /// </summary>
        /// <remarks>
        /// <para>Callback should make use of <c>GUILayout</c> to present controls.</para>
        /// </remarks>
        /// <example>
        /// <para>The following listing displays a label for empty list control:</para>
        /// <code language="csharp"><![CDATA[
        /// using Rotorz.Games.Collections;
        /// using System.Collections.Generic;
        /// using UnityEditor;
        /// using UnityEngine;
        /// 
        /// public class ExampleWindow : EditorWindow
        /// {
        ///     private List<string> list;
        /// 
        /// 
        ///     private void OnEnable()
        ///     {
        ///         this.list = new List<string>();
        ///     }
        /// 
        ///     private void OnGUI()
        ///     {
        ///         ReorderableListGUI.ListField(this.list, ReorderableListGUI.TextFieldItemDrawer, DrawEmptyMessage);
        ///     }
        /// 
        /// 
        ///     private string DrawEmptyMessage()
        ///     {
        ///         GUILayout.Label("List is empty!", EditorStyles.miniLabel);
        ///     }
        /// }
        /// ]]></code>
        /// </example>
        public delegate void DrawEmpty();
        /// <summary>
        /// Invoked to draw content for empty list with absolute positioning.
        /// </summary>
        /// <param name="position">Position of empty content.</param>
        public delegate void DrawEmptyAbsolute(Rect position);


        #region Custom Styles

        /// <summary>
        /// Background color of anchor list item.
        /// </summary>
        public static readonly Color AnchorBackgroundColor;
        /// <summary>
        /// Background color of target slot when dragging list item.
        /// </summary>
        public static readonly Color TargetBackgroundColor;

        /// <summary>
        /// Style for right-aligned label for element number prefix.
        /// </summary>
        private static GUIStyle s_RightAlignedLabelStyle;


        static ReorderableListControl()
        {
            s_CurrentListStack = new Stack<ListInfo>();
            s_CurrentListStack.Push(default(ListInfo));

            s_CurrentItemStack = new Stack<ItemInfo>();
            s_CurrentItemStack.Push(new ItemInfo(-1, default(Rect)));

            if (EditorGUIUtility.isProSkin) {
                AnchorBackgroundColor = new Color(85f / 255f, 85f / 255f, 85f / 255f, 0.85f);
                TargetBackgroundColor = new Color(0, 0, 0, 0.5f);
            }
            else {
                AnchorBackgroundColor = new Color(225f / 255f, 225f / 255f, 225f / 255f, 0.85f);
                TargetBackgroundColor = new Color(0, 0, 0, 0.5f);
            }
        }

        #endregion


        #region Utility

        private static readonly int s_ReorderableListControlHint = "_ReorderableListControl_".GetHashCode();

        private static int GetReorderableListControlID()
        {
            return GUIUtility.GetControlID(s_ReorderableListControlHint, FocusType.Passive);
        }

        /// <summary>
        /// Generate and draw control from state object.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Delegate for drawing empty list.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        public static void DrawControlFromState(IReorderableListAdaptor adaptor, DrawEmpty drawEmpty, ReorderableListFlags flags)
        {
            int controlID = GetReorderableListControlID();

            var control = GUIUtility.GetStateObject(typeof(ReorderableListControl), controlID) as ReorderableListControl;
            control.Flags = flags;
            control.Draw(controlID, adaptor, drawEmpty);
        }

        /// <summary>
        /// Generate and draw control from state object.
        /// </summary>
        /// <param name="position">Position of control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Delegate for drawing empty list.</param>
        /// <param name="flags">Optional flags to pass into list field.</param>
        public static void DrawControlFromState(Rect position, IReorderableListAdaptor adaptor, DrawEmptyAbsolute drawEmpty, ReorderableListFlags flags)
        {
            int controlID = GetReorderableListControlID();

            var control = GUIUtility.GetStateObject(typeof(ReorderableListControl), controlID) as ReorderableListControl;
            control.Flags = flags;
            control.Draw(position, controlID, adaptor, drawEmpty);
        }

        #endregion


        /// <summary>
        /// Position of mouse upon anchoring item for drag.
        /// </summary>
        private static float s_AnchorMouseOffset;
        /// <summary>
        /// Zero-based index of anchored list item.
        /// </summary>
        private static int s_AnchorIndex = -1;
        /// <summary>
        /// Zero-based index of target list item for reordering.
        /// </summary>
        private static int s_TargetIndex = -1;

        /// <summary>
        /// Unique ID of list control which should be automatically focused. A value
        /// of zero indicates that no control is to be focused.
        /// </summary>
        private static int s_AutoFocusControlID = 0;
        /// <summary>
        /// Zero-based index of item which should be focused.
        /// </summary>
        private static int s_AutoFocusIndex = -1;


        private struct ListInfo
        {
            public int ControlID;
            public Rect Position;

            public ListInfo(int controlID, Rect position)
            {
                this.ControlID = controlID;
                this.Position = position;
            }
        }


        private struct ItemInfo
        {
            public int ItemIndex;
            public Rect ItemPosition;

            public ItemInfo(int itemIndex, Rect itemPosition)
            {
                this.ItemIndex = itemIndex;
                this.ItemPosition = itemPosition;
            }
        }


        /// <summary>
        /// Represents the current stack of nested reorderable list control positions.
        /// </summary>
        private static Stack<ListInfo> s_CurrentListStack;

        /// <summary>
        /// Represents the current stack of nested reorderable list items.
        /// </summary>
        private static Stack<ItemInfo> s_CurrentItemStack;

        /// <summary>
        /// Gets the control ID of the list that is currently being drawn.
        /// </summary>
        public static int CurrentListControlID {
            get { return s_CurrentListStack.Peek().ControlID; }
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
            get { return s_CurrentListStack.Peek().Position; }
        }

        /// <summary>
        /// Gets the zero-based index of the list item that is currently being drawn;
        /// or a value of -1 if no item is currently being drawn.
        /// </summary>
        /// <remarks>
        /// <para>Use <see cref="ReorderableListGUI.CurrentItemIndex"/> instead.</para>
        /// </remarks>
        internal static int CurrentItemIndex {
            get { return s_CurrentItemStack.Peek().ItemIndex; }
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
            get { return s_CurrentItemStack.Peek().ItemPosition; }
        }


        #region Properties

        private ReorderableListFlags flags;

        /// <summary>
        /// Gets or sets flags which affect behavior of control.
        /// </summary>
        public ReorderableListFlags Flags {
            get { return this.flags; }
            set { this.flags = value; }
        }

        /// <summary>
        /// Gets a value indicating whether any footer controls are shown.
        /// </summary>
        private bool HasFooterControls {
            get { return this.HasSizeField || this.HasAddButton || this.HasAddMenuButton; }
        }
        /// <summary>
        /// Gets a value indicating whether the size field is shown.
        /// </summary>
        private bool HasSizeField {
            get { return (this.flags & ReorderableListFlags.ShowSizeField) != 0; }
        }
        /// <summary>
        /// Gets a value indicating whether add button is shown.
        /// </summary>
        private bool HasAddButton {
            get { return (this.flags & ReorderableListFlags.HideAddButton) == 0; }
        }
        /// <summary>
        /// Gets a value indicating whether add menu button is shown.
        /// </summary>
        private bool HasAddMenuButton { get; set; }

        /// <summary>
        /// Gets a value indicating whether remove buttons are shown.
        /// </summary>
        private bool HasRemoveButtons {
            get { return (this.flags & ReorderableListFlags.HideRemoveButtons) == 0; }
        }

        private float verticalSpacing = 10f;
        private GUIStyle containerStyle;
        private GUIStyle footerButtonStyle;
        private GUIStyle itemButtonStyle;

        /// <summary>
        /// Gets or sets the vertical spacing below the reorderable list control.
        /// </summary>
        public float VerticalSpacing {
            get { return this.verticalSpacing; }
            set { this.verticalSpacing = value; }
        }
        /// <summary>
        /// Gets or sets style used to draw background of list control.
        /// </summary>
        /// <seealso cref="ReorderableListStyles.Instance.Container"/>
        public GUIStyle ContainerStyle {
            get { return this.containerStyle; }
            set { this.containerStyle = value; }
        }
        /// <summary>
        /// Gets or sets style used to draw footer buttons.
        /// </summary>
        /// <seealso cref="ReorderableListStyles.Instance.FooterButton"/>
        public GUIStyle FooterButtonStyle {
            get { return this.footerButtonStyle; }
            set { this.footerButtonStyle = value; }
        }
        /// <summary>
        /// Gets or sets style used to draw list item buttons (like the remove button).
        /// </summary>
        /// <seealso cref="ReorderableListStyles.Instance.ItemButton"/>
        public GUIStyle ItemButtonStyle {
            get { return this.itemButtonStyle; }
            set { this.itemButtonStyle = value; }
        }

        private Color horizontalLineColor;
        private bool horizontalLineAtStart = false;
        private bool horizontalLineAtEnd = false;

        /// <summary>
        /// Gets or sets the color of the horizontal lines that appear between list items.
        /// </summary>
        public Color HorizontalLineColor {
            get { return this.horizontalLineColor; }
            set { this.horizontalLineColor = value; }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether a horizontal line should be
        /// shown above the first list item at the start of the list control.
        /// </summary>
        /// <remarks>
        /// <para>Horizontal line is not drawn for an empty list regardless of the value
        /// of this property.</para>
        /// </remarks>
        public bool HorizontalLineAtStart {
            get { return this.horizontalLineAtStart; }
            set { this.horizontalLineAtStart = value; }
        }

        /// <summary>
        /// Gets or sets a boolean value indicating whether a horizontal line should be
        /// shown below the last list item at the end of the list control.
        /// </summary>
        /// <remarks>
        /// <para>Horizontal line is not drawn for an empty list regardless of the value
        /// of this property.</para>
        /// </remarks>
        public bool HorizontalLineAtEnd {
            get { return this.horizontalLineAtEnd; }
            set { this.horizontalLineAtEnd = value; }
        }

        #endregion

        #region Events

        private event AddMenuClickedEventHandler addMenuClicked;
        private int addMenuClickedSubscriberCount = 0;

        /// <summary>
        /// Occurs when add menu button is clicked.
        /// </summary>
        /// <remarks>
        /// <para>Add menu button is only shown when there is at least one subscriber to this event.</para>
        /// </remarks>
        public event AddMenuClickedEventHandler AddMenuClicked {
            add {
                if (value == null) {
                    return;
                }

                this.addMenuClicked += value;
                ++this.addMenuClickedSubscriberCount;
                this.HasAddMenuButton = this.addMenuClickedSubscriberCount != 0;
            }
            remove {
                if (value == null) {
                    return;
                }

                this.addMenuClicked -= value;
                --this.addMenuClickedSubscriberCount;
                this.HasAddMenuButton = this.addMenuClickedSubscriberCount != 0;
            }
        }

        /// <summary>
        /// Raises event when add menu button is clicked.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        protected virtual void OnAddMenuClicked(AddMenuClickedEventArgs args)
        {
            if (this.addMenuClicked != null) {
                this.addMenuClicked(this, args);
            }
        }

        /// <summary>
        /// Occurs after list item is inserted or duplicated.
        /// </summary>
        public event ItemInsertedEventHandler ItemInserted;

        /// <summary>
        /// Raises event after list item is inserted or duplicated.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        protected virtual void OnItemInserted(ItemInsertedEventArgs args)
        {
            if (this.ItemInserted != null) {
                this.ItemInserted(this, args);
            }
        }

        /// <summary>
        /// Occurs before list item is removed and allowing for remove operation to be cancelled.
        /// </summary>
        public event ItemRemovingEventHandler ItemRemoving;

        /// <summary>
        /// Raises event before list item is removed and provides oppertunity to cancel.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        protected virtual void OnItemRemoving(ItemRemovingEventArgs args)
        {
            if (this.ItemRemoving != null) {
                this.ItemRemoving(this, args);
            }
        }

        /// <summary>
        /// Occurs immediately before list item is moved allowing for move operation to be cancelled.
        /// </summary>
        public event ItemMovingEventHandler ItemMoving;

        /// <summary>
        /// Raises event immediately before list item is moved and provides oppertunity to cancel.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        protected virtual void OnItemMoving(ItemMovingEventArgs args)
        {
            if (this.ItemMoving != null) {
                this.ItemMoving(this, args);
            }
        }

        /// <summary>
        /// Occurs after list item has been moved.
        /// </summary>
        public event ItemMovedEventHandler ItemMoved;

        /// <summary>
        /// Raises event after list item has been moved.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        protected virtual void OnItemMoved(ItemMovedEventArgs args)
        {
            if (this.ItemMoved != null) {
                this.ItemMoved(this, args);
            }
        }

        #endregion


        #region Construction

        /// <summary>
        /// Initializes a new instance of <see cref="ReorderableListControl"/>.
        /// </summary>
        public ReorderableListControl()
        {
            this.containerStyle = ReorderableListStyles.Instance.Container;
            this.footerButtonStyle = ReorderableListStyles.Instance.FooterButton;
            this.itemButtonStyle = ReorderableListStyles.Instance.ItemButton;

            this.horizontalLineColor = ReorderableListStyles.Instance.HorizontalLineColor;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ReorderableListControl"/>.
        /// </summary>
        /// <param name="flags">Optional flags which affect behavior of control.</param>
        public ReorderableListControl(ReorderableListFlags flags)
            : this()
        {
            this.Flags = flags;
        }

        #endregion


        #region Control State

        /// <summary>
        /// Unique Id of control.
        /// </summary>
        private int controlID;
        /// <summary>
        /// Visible rectangle of control.
        /// </summary>
        private Rect visibleRect;
        /// <summary>
        /// Width of index label in pixels (zero indicates no label).
        /// </summary>
        private float indexLabelWidth;
        /// <summary>
        /// Indicates whether item is currently being dragged within control.
        /// </summary>
        private bool tracking;
        /// <summary>
        /// Indicates if reordering is allowed.
        /// </summary>
        private bool allowReordering;

        /// <summary>
        /// A boolean value indicating whether drop insertion is allowed.
        /// </summary>
        private bool allowDropInsertion;
        /// <summary>
        /// Zero-based index for drop insertion when applicable; othewise, a value of -1.
        /// </summary>
        private int insertionIndex;
        /// <summary>
        /// Position of drop insertion on Y-axis in GUI space.
        /// </summary>
        private float insertionPosition;

        /// <summary>
        /// New size input value.
        /// </summary>
        private int newSizeInput;

        /// <summary>
        /// Prepare initial state for list control.
        /// </summary>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        private void PrepareState(int controlID, IReorderableListAdaptor adaptor)
        {
            this.controlID = controlID;
            this.visibleRect = ExtraEditorGUI.VisibleRect;

            if ((Flags & ReorderableListFlags.ShowIndices) != 0) {
                this.indexLabelWidth = CountDigits(adaptor.Count) * 8 + 8;
            }
            else {
                this.indexLabelWidth = 0;
            }

            this.tracking = IsTrackingControl(controlID);

            this.allowReordering = (this.Flags & ReorderableListFlags.DisableReordering) == 0;

            // The value of this field is reset each time the control is drawn and may
            // be invalidated when list items are drawn.
            this.allowDropInsertion = true;
        }

        private static int CountDigits(int number)
        {
            return Mathf.Max(2, Mathf.CeilToInt(Mathf.Log10((float)number)));
        }

        #endregion


        #region Event Handling

        // Indicates whether a "MouseDrag" event should be simulated on the next Layout/Repaint.
        private static int s_SimulateMouseDragControlID;

        /// <summary>
        /// Indicate that first control of list item should be automatically focused
        /// if possible.
        /// </summary>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <param name="itemIndex">Zero-based index of list item.</param>
        private void AutoFocusItem(int controlID, int itemIndex)
        {
            if ((Flags & ReorderableListFlags.DisableAutoFocus) == 0) {
                s_AutoFocusControlID = controlID;
                s_AutoFocusIndex = itemIndex;
            }
        }

        /// <summary>
        /// Draw remove button.
        /// </summary>
        /// <param name="position">Position of button.</param>
        /// <param name="visible">Indicates if control is visible within GUI.</param>
        /// <returns>
        /// A value of <c>true</c> if clicked; otherwise <c>false</c>.
        /// </returns>
        private bool DoRemoveButton(Rect position, bool visible)
        {
            var iconNormal = ReorderableListStyles.Skin.Icon_Remove_Normal;
            var iconActive = ReorderableListStyles.Skin.Icon_Remove_Active;

            return ExtraEditorGUI.IconButton(position, visible, iconNormal, iconActive, this.ItemButtonStyle);
        }

        private static bool s_TrackingCancelBlockContext;

        /// <summary>
        /// Begin tracking drag and drop within list.
        /// </summary>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <param name="itemIndex">Zero-based index of item which is going to be dragged.</param>
        private static void BeginTrackingReorderDrag(int controlID, int itemIndex)
        {
            GUIUtility.hotControl = controlID;
            GUIUtility.keyboardControl = 0;
            s_AnchorIndex = itemIndex;
            s_TargetIndex = itemIndex;
            s_TrackingCancelBlockContext = false;
        }

        /// <summary>
        /// Stop tracking drag and drop.
        /// </summary>
        private static void StopTrackingReorderDrag()
        {
            GUIUtility.hotControl = 0;
            s_AnchorIndex = -1;
            s_TargetIndex = -1;
        }

        /// <summary>
        /// Gets a value indicating whether item in current list is currently being tracked.
        /// </summary>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <returns>
        /// A value of <c>true</c> if item is being tracked; otherwise <c>false</c>.
        /// </returns>
        private static bool IsTrackingControl(int controlID)
        {
            return !s_TrackingCancelBlockContext && GUIUtility.hotControl == controlID;
        }

        /// <summary>
        /// Accept reordering.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        private void AcceptReorderDrag(IReorderableListAdaptor adaptor)
        {
            try {
                // Reorder list as needed!
                s_TargetIndex = Mathf.Clamp(s_TargetIndex, 0, adaptor.Count + 1);
                if (s_TargetIndex != s_AnchorIndex && s_TargetIndex != s_AnchorIndex + 1) {
                    this.MoveItem(adaptor, s_AnchorIndex, s_TargetIndex);
                }
            }
            finally {
                StopTrackingReorderDrag();
            }
        }

        private static Rect s_DragItemPosition;

        // Micro-optimisation to avoid repeated construction.
        private static Rect s_RemoveButtonPosition;

        private void DrawListItem(Rect position, IReorderableListAdaptor adaptor, int itemIndex)
        {
            bool isRepainting = Event.current.type == EventType.Repaint;
            bool isVisible = (position.y < this.visibleRect.yMax && position.yMax > this.visibleRect.y);
            bool isDraggable = this.allowReordering && adaptor.CanDrag(itemIndex);

            Rect itemContentPosition = position;
            itemContentPosition.x = position.x + 2;
            itemContentPosition.y += 1;
            itemContentPosition.width = position.width - 4;
            itemContentPosition.height = position.height - 4;

            // Make space for grab handle?
            if (isDraggable) {
                itemContentPosition.x += 20;
                itemContentPosition.width -= 20;
            }

            // Make space for element index.
            if (this.indexLabelWidth != 0) {
                itemContentPosition.width -= this.indexLabelWidth;

                if (isRepainting && isVisible) {
                    s_RightAlignedLabelStyle.Draw(new Rect(itemContentPosition.x, position.y, this.indexLabelWidth, position.height - 4), itemIndex + ":", false, false, false, false);
                }

                itemContentPosition.x += this.indexLabelWidth;
            }

            // Make space for remove button?
            if (this.HasRemoveButtons) {
                itemContentPosition.width -= 27;
            }

            try {
                s_CurrentItemStack.Push(new ItemInfo(itemIndex, position));
                EditorGUI.BeginChangeCheck();

                if (isRepainting && isVisible) {
                    // Draw background of list item.
                    var backgroundPosition = new Rect(position.x, position.y, position.width, position.height - 1);
                    adaptor.DrawItemBackground(backgroundPosition, itemIndex);

                    // Draw grab handle?
                    if (isDraggable) {
                        var texturePosition = new Rect(position.x + 6, position.y + position.height / 2f - 3, 9, 5);
                        ExtraEditorGUI.DrawTexture(texturePosition, ReorderableListStyles.Skin.GrabHandle);
                    }

                    // Draw horizontal line between list items.
                    if (!this.tracking || itemIndex != s_AnchorIndex) {
                        if (itemIndex != 0 || this.HorizontalLineAtStart) {
                            var horizontalLinePosition = new Rect(position.x, position.y - 1, position.width, 1);
                            ExtraEditorGUI.Separator(horizontalLinePosition, this.HorizontalLineColor);
                        }
                    }
                }

                // Allow control to be automatically focused.
                if (s_AutoFocusIndex == itemIndex) {
                    GUI.SetNextControlName("AutoFocus_" + this.controlID + "_" + itemIndex);
                }

                // Present actual control.
                adaptor.DrawItem(itemContentPosition, itemIndex);

                if (EditorGUI.EndChangeCheck()) {
                    ReorderableListGUI.IndexOfChangedItem = itemIndex;
                }

                // Draw remove button?
                if (this.HasRemoveButtons && adaptor.CanRemove(itemIndex)) {
                    s_RemoveButtonPosition = position;
                    s_RemoveButtonPosition.width = 27;
                    s_RemoveButtonPosition.x = itemContentPosition.xMax + 2;
                    s_RemoveButtonPosition.y -= 1;

                    if (this.DoRemoveButton(s_RemoveButtonPosition, isVisible)) {
                        this.RemoveItem(adaptor, itemIndex);
                    }
                }

                // Check for context click?
                if ((this.Flags & ReorderableListFlags.DisableContextMenu) == 0) {
                    if (Event.current.GetTypeForControl(this.controlID) == EventType.ContextClick && position.Contains(Event.current.mousePosition)) {
                        this.ShowContextMenu(itemIndex, adaptor);
                        Event.current.Use();
                    }
                }
            }
            finally {
                s_CurrentItemStack.Pop();
            }
        }

        private void DrawFloatingListItem(IReorderableListAdaptor adaptor, float targetSlotPosition)
        {
            if (Event.current.type == EventType.Repaint) {
                Color restoreColor = GUI.color;

                // Fill background of target area.
                Rect targetPosition = s_DragItemPosition;
                targetPosition.y = targetSlotPosition - 1;
                targetPosition.height = 1;

                ExtraEditorGUI.Separator(targetPosition, this.HorizontalLineColor);

                --targetPosition.x;
                ++targetPosition.y;
                targetPosition.width += 2;
                targetPosition.height = s_DragItemPosition.height - 1;

                GUI.color = TargetBackgroundColor;
                ExtraEditorGUI.DrawTexture(targetPosition, EditorGUIUtility.whiteTexture);

                // Fill background of item which is being dragged.
                --s_DragItemPosition.x;
                s_DragItemPosition.width += 2;
                --s_DragItemPosition.height;

                GUI.color = AnchorBackgroundColor;
                ExtraEditorGUI.DrawTexture(s_DragItemPosition, EditorGUIUtility.whiteTexture);

                ++s_DragItemPosition.x;
                s_DragItemPosition.width -= 2;
                ++s_DragItemPosition.height;

                // Draw horizontal splitter above and below.
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                targetPosition.y = s_DragItemPosition.y - 1;
                targetPosition.height = 1;
                ExtraEditorGUI.DrawTexture(targetPosition, EditorGUIUtility.whiteTexture);

                targetPosition.y += s_DragItemPosition.height;
                ExtraEditorGUI.DrawTexture(targetPosition, EditorGUIUtility.whiteTexture);

                GUI.color = restoreColor;
            }

            this.DrawListItem(s_DragItemPosition, adaptor, s_AnchorIndex);
        }

        // Counter is incremented whenever a reorderable list control reacts as a drop
        // target allowing parent reorderable list controls to suppress any reaction that
        // they might otherwise have.
        private static int s_DropTargetNestedCounter = 0;

        /// <summary>
        /// Draw list container and items.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        private void DrawListContainerAndItems(Rect position, IReorderableListAdaptor adaptor)
        {
            int initialDropTargetNestedCounterValue = s_DropTargetNestedCounter;

            // Get local copy of event information for efficiency.
            EventType eventType = Event.current.GetTypeForControl(this.controlID);
            Vector2 mousePosition = Event.current.mousePosition;

            int newTargetIndex = s_TargetIndex;

            // Position of first item in list.
            float firstItemY = position.y + this.ContainerStyle.padding.top;
            // Maximum position of dragged item.
            float dragItemMaxY = (position.yMax - this.ContainerStyle.padding.bottom) - s_DragItemPosition.height + 1;

            bool isMouseDragEvent = eventType == EventType.MouseDrag;
            if (s_SimulateMouseDragControlID == this.controlID && eventType == EventType.Repaint) {
                s_SimulateMouseDragControlID = 0;
                isMouseDragEvent = true;
            }
            if (isMouseDragEvent && this.tracking) {
                // Reset target index and adjust when looping through list items.
                if (mousePosition.y < firstItemY) {
                    newTargetIndex = 0;
                }
                else if (mousePosition.y >= position.yMax) {
                    newTargetIndex = adaptor.Count;
                }

                s_DragItemPosition.y = Mathf.Clamp(mousePosition.y + s_AnchorMouseOffset, firstItemY, dragItemMaxY);
            }

            switch (eventType) {
                case EventType.MouseDown:
                    if (this.tracking) {
                        // Cancel drag when other mouse button is pressed.
                        s_TrackingCancelBlockContext = true;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (this.controlID == GUIUtility.hotControl) {
                        // Allow user code to change control over reordering during drag.
                        if (!s_TrackingCancelBlockContext && this.allowReordering) {
                            this.AcceptReorderDrag(adaptor);
                        }
                        else {
                            StopTrackingReorderDrag();
                        }
                        Event.current.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (this.tracking && Event.current.keyCode == KeyCode.Escape) {
                        StopTrackingReorderDrag();
                        Event.current.Use();
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (s_ContextControlID == this.controlID) {
                        int itemIndex = s_ContextItemIndex;
                        try {
                            this.DoCommand(s_ContextCommandName, itemIndex, adaptor);
                            Event.current.Use();
                        }
                        finally {
                            s_ContextControlID = 0;
                            s_ContextItemIndex = 0;
                        }
                    }
                    break;

                case EventType.Repaint:
                    // Draw caption area of list.
                    this.ContainerStyle.Draw(position, GUIContent.none, false, false, false, false);
                    break;
            }

            ReorderableListGUI.IndexOfChangedItem = -1;

            // Draw list items!
            Rect itemPosition = new Rect(position.x + this.ContainerStyle.padding.left, firstItemY, position.width - this.ContainerStyle.padding.horizontal, 0);
            float targetSlotPosition = dragItemMaxY;

            this.insertionIndex = 0;
            this.insertionPosition = itemPosition.yMax;

            float lastMidPoint = 0f;
            float lastHeight = 0f;

            int count = adaptor.Count;
            for (int i = 0; i < count; ++i) {
                itemPosition.y = itemPosition.yMax;
                itemPosition.height = 0;

                lastMidPoint = itemPosition.y - lastHeight / 2f;

                if (this.tracking) {
                    // Does this represent the target index?
                    if (i == s_TargetIndex) {
                        targetSlotPosition = itemPosition.y;
                        itemPosition.y += s_DragItemPosition.height;
                    }

                    // Do not draw item if it is currently being dragged.
                    // Draw later so that it is shown in front of other controls.
                    if (i == s_AnchorIndex) {
                        continue;
                    }

                    // Update position for current item.
                    itemPosition.height = adaptor.GetItemHeight(i) + 4;
                    lastHeight = itemPosition.height;
                }
                else {
                    // Update position for current item.
                    itemPosition.height = adaptor.GetItemHeight(i) + 4;
                    lastHeight = itemPosition.height;

                    // Does this represent the drop insertion index?
                    float midpoint = itemPosition.y + itemPosition.height / 2f;
                    if (mousePosition.y > lastMidPoint && mousePosition.y <= midpoint) {
                        this.insertionIndex = i;
                        this.insertionPosition = itemPosition.y;
                    }
                }

                if (this.tracking && isMouseDragEvent) {
                    float midpoint = itemPosition.y + itemPosition.height / 2f;

                    if (s_TargetIndex < i) {
                        if (s_DragItemPosition.yMax > lastMidPoint && s_DragItemPosition.yMax < midpoint) {
                            newTargetIndex = i;
                        }
                    }
                    else if (s_TargetIndex > i) {
                        if (s_DragItemPosition.y > lastMidPoint && s_DragItemPosition.y < midpoint) {
                            newTargetIndex = i;
                        }
                    }

                    /*if (s_DragItemPosition.y > itemPosition.y && s_DragItemPosition.y <= midpoint) {
                        newTargetIndex = i;
                    }
                    else if (s_DragItemPosition.yMax > midpoint && s_DragItemPosition.yMax <= itemPosition.yMax) {
                        newTargetIndex = i + 1;
                    }*/
                }

                // Draw list item.
                this.DrawListItem(itemPosition, adaptor, i);

                // Did list count change (i.e. item removed)?
                if (adaptor.Count < count) {
                    // We assume that it was this item which was removed, so --i allows us
                    // to process the next item as usual.
                    count = adaptor.Count;
                    --i;
                    continue;
                }

                // Event has already been used, skip to next item.
                if (Event.current.type != EventType.Used) {
                    switch (eventType) {
                        case EventType.MouseDown:
                            if (GUI.enabled && itemPosition.Contains(mousePosition)) {
                                // Remove input focus from control before attempting a context click or drag.
                                GUIUtility.keyboardControl = 0;

                                if (this.allowReordering && adaptor.CanDrag(i) && Event.current.button == 0) {
                                    s_DragItemPosition = itemPosition;

                                    BeginTrackingReorderDrag(this.controlID, i);
                                    s_AnchorMouseOffset = itemPosition.y - mousePosition.y;
                                    s_TargetIndex = i;

                                    Event.current.Use();
                                }
                            }
                            break;
                            /* DEBUG
                            case EventType.Repaint:
                                GUI.color = Color.red;
                                GUI.DrawTexture(new Rect(0, lastMidPoint, 10, 1), EditorGUIUtility.whiteTexture);
                                GUI.color = Color.yellow;
                                GUI.DrawTexture(new Rect(5, itemPosition.y + itemPosition.height / 2f, 10, 1), EditorGUIUtility.whiteTexture);
                                GUI.color = Color.white;
                                break;
                            //*/
                    }
                }
            }

            if (this.HorizontalLineAtEnd) {
                var horizontalLinePosition = new Rect(itemPosition.x, position.yMax - this.ContainerStyle.padding.vertical, itemPosition.width, 1);
                ExtraEditorGUI.Separator(horizontalLinePosition, this.HorizontalLineColor);
            }

            lastMidPoint = position.yMax - lastHeight / 2f;

            // Assume that drop insertion is not allowed at this time; we can change our
            // mind a little further down ;)
            this.allowDropInsertion = false;

            // Item which is being dragged should be shown on top of other controls!
            if (IsTrackingControl(this.controlID)) {
                if (isMouseDragEvent) {
                    if (s_DragItemPosition.yMax >= lastMidPoint) {
                        newTargetIndex = count;
                    }

                    s_TargetIndex = newTargetIndex;

                    // Force repaint to occur so that dragging rectangle is visible.
                    // But only if this is a real MouseDrag event!!
                    if (eventType == EventType.MouseDrag) {
                        Event.current.Use();
                    }
                }

                this.DrawFloatingListItem(adaptor, targetSlotPosition);
                /* DEBUG
                if (eventType == EventType.Repaint) {
                    GUI.color = Color.blue;
                    GUI.DrawTexture(new Rect(100, lastMidPoint, 20, 1), EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                }
                //*/
            }
            else {
                // Cannot react to drop insertion if a nested drop target has already reacted!
                if (s_DropTargetNestedCounter == initialDropTargetNestedCounterValue) {
                    if (Event.current.mousePosition.y >= lastMidPoint) {
                        this.insertionIndex = adaptor.Count;
                        this.insertionPosition = itemPosition.yMax;
                    }
                    this.allowDropInsertion = true;
                }
            }

            // Fake control to catch input focus if auto focus was not possible.
            GUIUtility.GetControlID(FocusType.Keyboard);

            if (isMouseDragEvent && (Flags & ReorderableListFlags.DisableAutoScroll) == 0 && IsTrackingControl(this.controlID)) {
                this.AutoScrollTowardsMouse();
            }
        }

        private static bool ContainsRect(Rect a, Rect b)
        {
            return a.Contains(new Vector2(b.xMin, b.yMin)) && a.Contains(new Vector2(b.xMax, b.yMax));
        }

        private void AutoScrollTowardsMouse()
        {
            const float triggerPaddingInPixels = 8f;
            const float maximumRangeInPixels = 4f;

            Rect visiblePosition = ExtraEditorGUI.VisibleRect;
            Vector2 mousePosition = Event.current.mousePosition;
            Rect mouseRect = new Rect(mousePosition.x - triggerPaddingInPixels, mousePosition.y - triggerPaddingInPixels, triggerPaddingInPixels * 2, triggerPaddingInPixels * 2);

            if (!ContainsRect(visiblePosition, mouseRect)) {
                if (mousePosition.y < visiblePosition.center.y) {
                    mousePosition = new Vector2(mouseRect.xMin, mouseRect.yMin);
                }
                else {
                    mousePosition = new Vector2(mouseRect.xMax, mouseRect.yMax);
                }

                mousePosition.x = Mathf.Max(mousePosition.x - maximumRangeInPixels, mouseRect.xMax);
                mousePosition.y = Mathf.Min(mousePosition.y + maximumRangeInPixels, mouseRect.yMax);
                GUI.ScrollTo(new Rect(mousePosition.x, mousePosition.y, 1, 1));

                s_SimulateMouseDragControlID = this.controlID;

                var focusedWindow = EditorWindow.focusedWindow;
                if (focusedWindow != null) {
                    focusedWindow.Repaint();
                }
            }
        }

        private void HandleDropInsertion(Rect position, IReorderableListAdaptor adaptor)
        {
            var target = adaptor as IReorderableListDropTarget;
            if (target == null || !this.allowDropInsertion) {
                return;
            }

            if (target.CanDropInsert(this.insertionIndex)) {
                ++s_DropTargetNestedCounter;

                switch (Event.current.type) {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        DragAndDrop.activeControlID = this.controlID;
                        target.ProcessDropInsertion(this.insertionIndex);
                        Event.current.Use();
                        break;

                    case EventType.DragPerform:
                        target.ProcessDropInsertion(this.insertionIndex);

                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;
                        Event.current.Use();
                        break;

                    default:
                        target.ProcessDropInsertion(this.insertionIndex);
                        break;
                }

                if (DragAndDrop.activeControlID == this.controlID && Event.current.type == EventType.Repaint) {
                    this.DrawDropIndicator(new Rect(position.x, this.insertionPosition - 2, position.width, 3));
                }
            }
        }

        /// <summary>
        /// Draws drop insertion indicator.
        /// </summary>
        /// <remarks>
        /// <para>This method is only ever called during repaint events.</para>
        /// </remarks>
        /// <param name="position">Position if the drop indicator.</param>
        protected virtual void DrawDropIndicator(Rect position)
        {
            ExtraEditorGUI.Separator(position);
        }

        /// <summary>
        /// Checks to see if list control needs to be automatically focused.
        /// </summary>
        private void CheckForAutoFocusControl()
        {
            if (Event.current.type == EventType.Used) {
                return;
            }

            // Automatically focus control!
            if (s_AutoFocusControlID == this.controlID) {
                s_AutoFocusControlID = 0;
                EditorGUI.FocusTextInControl("AutoFocus_" + this.controlID + "_" + s_AutoFocusIndex);
                s_AutoFocusIndex = -1;
            }
        }

        /// <summary>
        /// Draw additional controls below list control and highlight drop target.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        private void DrawFooterControls(Rect position, IReorderableListAdaptor adaptor)
        {
            if (this.HasFooterControls) {
                Rect buttonPosition = new Rect(position.xMax - 30, position.yMax - 1, 30, this.FooterButtonStyle.fixedHeight);

                Rect menuButtonPosition = buttonPosition;
                var menuIconNormal = ReorderableListStyles.Skin.Icon_AddMenu_Normal;
                var menuIconActive = ReorderableListStyles.Skin.Icon_AddMenu_Active;

                if (this.HasSizeField) {
                    // Draw size field.
                    Rect sizeFieldPosition = new Rect(
                        position.x,
                        position.yMax + 1,
                        Mathf.Max(150f, position.width / 3f),
                        16f
                    );

                    this.DrawSizeFooterControl(sizeFieldPosition, adaptor);
                }

                if (this.HasAddButton) {
                    // Draw add menu drop-down button.
                    if (this.HasAddMenuButton) {
                        menuButtonPosition.x = buttonPosition.xMax - 14;
                        menuButtonPosition.xMax = buttonPosition.xMax;
                        menuIconNormal = ReorderableListStyles.Skin.Icon_Menu_Normal;
                        menuIconActive = ReorderableListStyles.Skin.Icon_Menu_Active;
                        buttonPosition.width -= 5;
                        buttonPosition.x = menuButtonPosition.x - buttonPosition.width + 1;
                    }

                    // Draw add item button.
                    var iconNormal = ReorderableListStyles.Skin.Icon_Add_Normal;
                    var iconActive = ReorderableListStyles.Skin.Icon_Add_Active;

                    if (ExtraEditorGUI.IconButton(buttonPosition, true, iconNormal, iconActive, this.FooterButtonStyle)) {
                        // Append item to list.
                        GUIUtility.keyboardControl = 0;
                        this.AddItem(adaptor);
                    }
                }

                if (this.HasAddMenuButton) {
                    // Draw add menu drop-down button.
                    if (ExtraEditorGUI.IconButton(menuButtonPosition, true, menuIconNormal, menuIconActive, this.FooterButtonStyle)) {
                        GUIUtility.keyboardControl = 0;
                        Rect totalAddButtonPosition = buttonPosition;
                        totalAddButtonPosition.xMax = position.xMax;
                        this.OnAddMenuClicked(new AddMenuClickedEventArgs(adaptor, totalAddButtonPosition));

                        // This will be helpful in many circumstances; including by default!
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void DrawSizeFooterControl(Rect position, IReorderableListAdaptor adaptor)
        {
            float restoreLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60f;

            this.DrawSizeField(position, adaptor);

            EditorGUIUtility.labelWidth = restoreLabelWidth;
        }

        /// <summary>
        /// Cache of container heights mapped by control ID.
        /// </summary>
        private static Dictionary<int, float> s_ContainerHeightCache = new Dictionary<int, float>();

        private Rect GetListRectWithAutoLayout(IReorderableListAdaptor adaptor, float padding)
        {
            float totalHeight;

            // Calculate position of list field using layout engine.
            if (Event.current.type == EventType.Layout) {
                totalHeight = this.CalculateListHeight(adaptor);
                s_ContainerHeightCache[this.controlID] = totalHeight;
            }
            else {
                totalHeight = s_ContainerHeightCache.ContainsKey(this.controlID)
                    ? s_ContainerHeightCache[this.controlID]
                    : 0;
            }

            totalHeight += padding;

            return GUILayoutUtility.GetRect(GUIContent.none, this.ContainerStyle, GUILayout.Height(totalHeight));
        }

        /// <summary>
        /// Do layout version of list field.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="padding">Padding in pixels.</param>
        /// <returns>
        /// Position of list container area in GUI (excludes footer area).
        /// </returns>
        private Rect DrawLayoutListField(IReorderableListAdaptor adaptor, float padding)
        {
            Rect position = this.GetListRectWithAutoLayout(adaptor, padding);

            // Make room for footer buttons?
            if (this.HasFooterControls) {
                position.height -= this.FooterButtonStyle.fixedHeight;
            }

            // Make room for vertical spacing below footer buttons.
            position.height -= this.VerticalSpacing;

            s_CurrentListStack.Push(new ListInfo(this.controlID, position));
            try {
                // Draw list as normal.
                adaptor.BeginGUI();
                this.DrawListContainerAndItems(position, adaptor);
                this.HandleDropInsertion(position, adaptor);
                adaptor.EndGUI();
            }
            finally {
                s_CurrentListStack.Pop();
            }

            this.CheckForAutoFocusControl();

            return position;
        }

        /// <summary>
        /// Draw content for empty list (layout version).
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Callback to draw empty content.</param>
        /// <returns>
        /// Position of list container area in GUI (excludes footer area).
        /// </returns>
        private Rect DrawLayoutEmptyList(IReorderableListAdaptor adaptor, DrawEmpty drawEmpty)
        {
            Rect position = EditorGUILayout.BeginVertical(this.ContainerStyle);
            {
                if (drawEmpty != null) {
                    drawEmpty();
                }
                else {
                    Debug.LogError("Unexpected call to 'DrawLayoutEmptyList'");
                }

                s_CurrentListStack.Push(new ListInfo(this.controlID, position));
                try {
                    adaptor.BeginGUI();
                    this.insertionIndex = 0;
                    this.insertionPosition = position.y + 2;
                    this.HandleDropInsertion(position, adaptor);
                    adaptor.EndGUI();
                }
                finally {
                    s_CurrentListStack.Pop();
                }
            }
            EditorGUILayout.EndVertical();

            // Allow room for footer buttons?
            if (this.HasFooterControls) {
                GUILayoutUtility.GetRect(0, this.FooterButtonStyle.fixedHeight - 1);
            }

            return position;
        }

        /// <summary>
        /// Draw content for empty list (layout version).
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="drawEmpty">Callback to draw empty content.</param>
        private void DrawEmptyListControl(Rect position, DrawEmptyAbsolute drawEmpty)
        {
            if (Event.current.type == EventType.Repaint) {
                this.ContainerStyle.Draw(position, GUIContent.none, false, false, false, false);
            }

            // Take padding into consideration when drawing empty content.
            position = this.ContainerStyle.padding.Remove(position);

            if (drawEmpty != null) {
                drawEmpty(position);
            }
        }

        /// <summary>
        /// Correct if for some reason one or more styles are missing!
        /// </summary>
        private void FixStyles()
        {
            this.ContainerStyle = this.ContainerStyle ?? ReorderableListStyles.Instance.Container;
            this.FooterButtonStyle = this.FooterButtonStyle ?? ReorderableListStyles.Instance.FooterButton;
            this.ItemButtonStyle = this.ItemButtonStyle ?? ReorderableListStyles.Instance.ItemButton;

            if (s_RightAlignedLabelStyle == null) {
                s_RightAlignedLabelStyle = new GUIStyle(GUI.skin.label);
                s_RightAlignedLabelStyle.alignment = TextAnchor.MiddleRight;
                s_RightAlignedLabelStyle.padding.right = 4;
            }
        }

        /// <summary>
        /// Draw layout version of list control.
        /// </summary>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Delegate for drawing empty list.</param>
        private void Draw(int controlID, IReorderableListAdaptor adaptor, DrawEmpty drawEmpty)
        {
            this.FixStyles();
            this.PrepareState(controlID, adaptor);

            Rect position;
            if (adaptor.Count > 0) {
                position = this.DrawLayoutListField(adaptor, 0f);
            }
            else if (drawEmpty == null) {
                position = this.DrawLayoutListField(adaptor, 5f);
            }
            else {
                position = this.DrawLayoutEmptyList(adaptor, drawEmpty);
            }

            this.DrawFooterControls(position, adaptor);
        }

        /// <inheritdoc cref="Draw(int, IReorderableListAdaptor, DrawEmpty)"/>
        public void Draw(IReorderableListAdaptor adaptor, DrawEmpty drawEmpty)
        {
            int controlID = GetReorderableListControlID();
            this.Draw(controlID, adaptor, drawEmpty);
        }

        /// <inheritdoc cref="Draw(int, IReorderableListAdaptor, DrawEmpty)"/>
        public void Draw(IReorderableListAdaptor adaptor)
        {
            int controlID = GetReorderableListControlID();
            this.Draw(controlID, adaptor, null);
        }

        /// <summary>
        /// Draw list control with absolute positioning.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="controlID">Unique ID of list control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Delegate for drawing empty list.</param>
        private void Draw(Rect position, int controlID, IReorderableListAdaptor adaptor, DrawEmptyAbsolute drawEmpty)
        {
            this.FixStyles();
            this.PrepareState(controlID, adaptor);

            // Allow for footer area.
            if (this.HasFooterControls) {
                position.height -= this.FooterButtonStyle.fixedHeight;
            }

            // Make room for vertical spacing below footer buttons.
            position.height -= this.VerticalSpacing;

            s_CurrentListStack.Push(new ListInfo(this.controlID, position));
            try {
                adaptor.BeginGUI();

                this.DrawListContainerAndItems(position, adaptor);
                this.HandleDropInsertion(position, adaptor);
                this.CheckForAutoFocusControl();

                if (adaptor.Count == 0) {
                    ReorderableListGUI.IndexOfChangedItem = -1;
                    this.DrawEmptyListControl(position, drawEmpty);
                }

                adaptor.EndGUI();
            }
            finally {
                s_CurrentListStack.Pop();
            }

            this.DrawFooterControls(position, adaptor);
        }

        /// <summary>
        /// Draw list control with absolute positioning.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="drawEmpty">Delegate for drawing empty list.</param>
        public void Draw(Rect position, IReorderableListAdaptor adaptor, DrawEmptyAbsolute drawEmpty)
        {
            int controlID = GetReorderableListControlID();
            this.Draw(position, controlID, adaptor, drawEmpty);
        }

        /// <inheritdoc cref="Draw(Rect, IReorderableListAdaptor, DrawEmptyAbsolute)"/>
        public void Draw(Rect position, IReorderableListAdaptor adaptor)
        {
            int controlID = GetReorderableListControlID();
            this.Draw(position, controlID, adaptor, null);
        }

        #endregion


        #region Size Field

        private static readonly GUIContent s_Temp = new GUIContent();
        private static readonly GUIContent s_SizePrefixLabel = new GUIContent("Size");

        /// <summary>
        /// Draw list size field with absolute positioning and a custom prefix label.
        /// </summary>
        /// <remarks>
        /// <para>Specify a value of <c>GUIContent.none</c> for argument <paramref name="label"/>
        /// to omit prefix label from the drawn control.</para>
        /// </remarks>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="label">Prefix label for the control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(Rect position, GUIContent label, IReorderableListAdaptor adaptor)
        {
            int sizeControlID = GUIUtility.GetControlID(FocusType.Passive);
            string sizeControlName = "ReorderableListControl.Size." + sizeControlID;
            GUI.SetNextControlName(sizeControlName);

            if (GUI.GetNameOfFocusedControl() == sizeControlName) {
                if (Event.current.rawType == EventType.KeyDown) {
                    switch (Event.current.keyCode) {
                        case KeyCode.Return:
                        case KeyCode.KeypadEnter:
                            this.ResizeList(adaptor, newSizeInput);
                            Event.current.Use();
                            break;
                    }
                }
                this.newSizeInput = EditorGUI.IntField(position, label, this.newSizeInput);
            }
            else {
                EditorGUI.IntField(position, label, adaptor.Count);
                this.newSizeInput = adaptor.Count;
            }
        }

        /// <summary>
        /// Draw list size field with absolute positioning and a custom prefix label.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="label">Prefix label for the control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(Rect position, string label, IReorderableListAdaptor adaptor)
        {
            s_Temp.text = label;
            this.DrawSizeField(position, s_Temp, adaptor);
        }

        /// <summary>
        /// Draw list size field with absolute positioning with the default prefix label.
        /// </summary>
        /// <param name="position">Position of list control in GUI.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(Rect position, IReorderableListAdaptor adaptor)
        {
            this.DrawSizeField(position, s_SizePrefixLabel, adaptor);
        }

        /// <summary>
        /// Draw list size field with automatic layout and a custom prefix label.
        /// </summary>
        /// <remarks>
        /// <para>Specify a value of <c>GUIContent.none</c> for argument <paramref name="label"/>
        /// to omit prefix label from the drawn control.</para>
        /// </remarks>
        /// <param name="label">Prefix label for the control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(GUIContent label, IReorderableListAdaptor adaptor)
        {
            Rect position = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
            this.DrawSizeField(position, label, adaptor);
        }

        /// <summary>
        /// Draw list size field with automatic layout and a custom prefix label.
        /// </summary>
        /// <param name="label">Prefix label for the control.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(string label, IReorderableListAdaptor adaptor)
        {
            s_Temp.text = label;
            this.DrawSizeField(s_Temp, adaptor);
        }

        /// <summary>
        /// Draw list size field with automatic layout and the default prefix label.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        public void DrawSizeField(IReorderableListAdaptor adaptor)
        {
            this.DrawSizeField(s_SizePrefixLabel, adaptor);
        }

        #endregion


        #region Context Menu

        /// <summary>
        /// Content for "Move to Top" command.
        /// </summary>
        protected static readonly GUIContent CommandMoveToTop = new GUIContent("Move to Top");
        /// <summary>
        /// Content for "Move to Bottom" command.
        /// </summary>
        protected static readonly GUIContent CommandMoveToBottom = new GUIContent("Move to Bottom");
        /// <summary>
        /// Content for "Insert Above" command.
        /// </summary>
        protected static readonly GUIContent CommandInsertAbove = new GUIContent("Insert Above");
        /// <summary>
        /// Content for "Insert Below" command.
        /// </summary>
        protected static readonly GUIContent CommandInsertBelow = new GUIContent("Insert Below");
        /// <summary>
        /// Content for "Duplicate" command.
        /// </summary>
        protected static readonly GUIContent CommandDuplicate = new GUIContent("Duplicate");
        /// <summary>
        /// Content for "Remove" command.
        /// </summary>
        protected static readonly GUIContent CommandRemove = new GUIContent("Remove");
        /// <summary>
        /// Content for "Clear All" command.
        /// </summary>
        protected static readonly GUIContent CommandClearAll = new GUIContent("Clear All");

        // Command control id and item index are assigned when context menu is shown.
        private static int s_ContextControlID;
        private static int s_ContextItemIndex;

        // Command name is assigned by default context menu handler.
        private static string s_ContextCommandName;

        private void ShowContextMenu(int itemIndex, IReorderableListAdaptor adaptor)
        {
            GenericMenu menu = new GenericMenu();

            s_ContextControlID = this.controlID;
            s_ContextItemIndex = itemIndex;

            this.AddItemsToMenu(menu, itemIndex, adaptor);

            if (menu.GetItemCount() > 0) {
                menu.ShowAsContext();
            }
        }

        /// <summary>
        /// Default functionality to handle context command.
        /// </summary>
        /// <example>
        /// <para>Can be used when adding custom items to the context menu:</para>
        /// <code language="csharp"><![CDATA[
        /// protected override void AddItemsToMenu(GenericMenu menu, int itemIndex, IReorderableListAdaptor adaptor)
        /// {
        ///     var specialCommand = new GUIContent("Special Command");
        ///     menu.AddItem(specialCommand, false, defaultContextHandler, specialCommand);
        /// }
        /// ]]></code>
        /// </example>
        /// <seealso cref="AddItemsToMenu"/>
        protected static readonly GenericMenu.MenuFunction2 DefaultContextHandler = DefaultContextMenuHandler;

        private static void DefaultContextMenuHandler(object userData)
        {
            var commandContent = userData as GUIContent;
            if (commandContent == null || string.IsNullOrEmpty(commandContent.text)) {
                return;
            }

            s_ContextCommandName = commandContent.text;

            var e = EditorGUIUtility.CommandEvent("ReorderableListContextCommand");
            EditorWindow.focusedWindow.SendEvent(e);
        }

        /// <summary>
        /// Invoked to generate context menu for list item.
        /// </summary>
        /// <param name="menu">Menu which can be populated.</param>
        /// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        protected virtual void AddItemsToMenu(GenericMenu menu, int itemIndex, IReorderableListAdaptor adaptor)
        {
            if ((this.Flags & ReorderableListFlags.DisableReordering) == 0) {
                if (itemIndex > 0) {
                    menu.AddItem(CommandMoveToTop, false, DefaultContextHandler, CommandMoveToTop);
                }
                else {
                    menu.AddDisabledItem(CommandMoveToTop);
                }

                if (itemIndex + 1 < adaptor.Count) {
                    menu.AddItem(CommandMoveToBottom, false, DefaultContextHandler, CommandMoveToBottom);
                }
                else {
                    menu.AddDisabledItem(CommandMoveToBottom);
                }

                if (this.HasAddButton) {
                    menu.AddSeparator("");

                    menu.AddItem(CommandInsertAbove, false, DefaultContextHandler, CommandInsertAbove);
                    menu.AddItem(CommandInsertBelow, false, DefaultContextHandler, CommandInsertBelow);

                    if ((Flags & ReorderableListFlags.DisableDuplicateCommand) == 0) {
                        menu.AddItem(CommandDuplicate, false, DefaultContextHandler, CommandDuplicate);
                    }
                }
            }

            if (this.HasRemoveButtons) {
                if (menu.GetItemCount() > 0) {
                    menu.AddSeparator("");
                }

                menu.AddItem(CommandRemove, false, DefaultContextHandler, CommandRemove);
                menu.AddSeparator("");
                menu.AddItem(CommandClearAll, false, DefaultContextHandler, CommandClearAll);
            }
        }

        #endregion


        #region Command Handling

        /// <summary>
        /// Invoked to handle context command.
        /// </summary>
        /// <remarks>
        /// <para>It is important to set the value of <c>GUI.changed</c> to <c>true</c> if any
        /// changes are made by command handler.</para>
        /// <para>Default command handling functionality can be inherited:</para>
        /// <code language="csharp"><![CDATA[
        /// protected override bool HandleCommand(string commandName, int itemIndex, IReorderableListAdaptor adaptor)
        /// {
        ///     if (base.HandleCommand(itemIndex, adaptor)) {
        ///         return true;
        ///     }
        ///     
        ///     // Place custom command handling code here...
        ///     switch (commandName) {
        ///         case "Your Command":
        ///             return true;
        ///     }
        /// 
        ///     return false;
        /// }
        /// ]]></code>
        /// </remarks>
        /// <param name="commandName">Name of command. This is the text shown in the context menu.</param>
        /// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <returns>
        /// A value of <c>true</c> if command was known; otherwise <c>false</c>.
        /// </returns>
        protected virtual bool HandleCommand(string commandName, int itemIndex, IReorderableListAdaptor adaptor)
        {
            switch (commandName) {
                case "Move to Top":
                    this.MoveItem(adaptor, itemIndex, 0);
                    return true;
                case "Move to Bottom":
                    this.MoveItem(adaptor, itemIndex, adaptor.Count);
                    return true;

                case "Insert Above":
                    this.InsertItem(adaptor, itemIndex);
                    return true;
                case "Insert Below":
                    this.InsertItem(adaptor, itemIndex + 1);
                    return true;
                case "Duplicate":
                    this.DuplicateItem(adaptor, itemIndex);
                    return true;

                case "Remove":
                    this.RemoveItem(adaptor, itemIndex);
                    return true;
                case "Clear All":
                    this.ClearAll(adaptor);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Call to manually perform command.
        /// </summary>
        /// <remarks>
        /// <para>Warning message is logged to console if attempted to execute unknown command.</para>
        /// </remarks>
        /// <param name="commandName">Name of command. This is the text shown in the context menu.</param>
        /// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <returns>
        /// A value of <c>true</c> if command was known; otherwise <c>false</c>.
        /// </returns>
        public bool DoCommand(string commandName, int itemIndex, IReorderableListAdaptor adaptor)
        {
            if (!this.HandleCommand(s_ContextCommandName, itemIndex, adaptor)) {
                Debug.LogWarning("Unknown context command.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Call to manually perform command.
        /// </summary>
        /// <remarks>
        /// <para>Warning message is logged to console if attempted to execute unknown command.</para>
        /// </remarks>
        /// <param name="command">Content representing command.</param>
        /// <param name="itemIndex">Zero-based index of item which was right-clicked.</param>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <returns>
        /// A value of <c>true</c> if command was known; otherwise <c>false</c>.
        /// </returns>
        public bool DoCommand(GUIContent command, int itemIndex, IReorderableListAdaptor adaptor)
        {
            return this.DoCommand(command.text, itemIndex, adaptor);
        }

        #endregion


        #region Methods

        /// <summary>
        /// Calculate height of list control in pixels.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <returns>
        /// Required list height in pixels.
        /// </returns>
        public float CalculateListHeight(IReorderableListAdaptor adaptor)
        {
            this.FixStyles();

            float totalHeight = this.ContainerStyle.padding.vertical - 1 + this.VerticalSpacing;

            // Take list items into consideration.
            int count = adaptor.Count;
            for (int i = 0; i < count; ++i) {
                totalHeight += adaptor.GetItemHeight(i);
            }
            // Add spacing between list items.
            totalHeight += 4 * count;

            // Add height of footer buttons.
            if (this.HasFooterControls) {
                totalHeight += this.FooterButtonStyle.fixedHeight;
            }

            return totalHeight;
        }

        /// <summary>
        /// Calculate height of list control in pixels.
        /// </summary>
        /// <param name="itemCount">Count of items in list.</param>
        /// <param name="itemHeight">Fixed height of list item.</param>
        /// <returns>
        /// Required list height in pixels.
        /// </returns>
        public float CalculateListHeight(int itemCount, float itemHeight)
        {
            this.FixStyles();

            float totalHeight = this.ContainerStyle.padding.vertical - 1 + this.VerticalSpacing;

            // Take list items into consideration.
            totalHeight += (itemHeight + 4) * itemCount;

            // Add height of footer buttons.
            if (this.HasFooterControls) {
                totalHeight += this.FooterButtonStyle.fixedHeight;
            }

            return totalHeight;
        }

        /// <summary>
        /// Move item from source index to destination index.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="sourceIndex">Zero-based index of source item.</param>
        /// <param name="destIndex">Zero-based index of destination index.</param>
        protected void MoveItem(IReorderableListAdaptor adaptor, int sourceIndex, int destIndex)
        {
            // Raise event before moving item so that the operation can be cancelled.
            var movingEventArgs = new ItemMovingEventArgs(adaptor, sourceIndex, destIndex);
            this.OnItemMoving(movingEventArgs);
            if (!movingEventArgs.Cancel) {
                adaptor.Move(sourceIndex, destIndex);

                // Item was actually moved!
                int newIndex = destIndex;
                if (newIndex > sourceIndex) {
                    --newIndex;
                }
                this.OnItemMoved(new ItemMovedEventArgs(adaptor, sourceIndex, newIndex));

                GUI.changed = true;
            }
            ReorderableListGUI.IndexOfChangedItem = -1;
        }

        /// <summary>
        /// Add item at end of list and raises the event <see cref="ItemInserted"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        protected void AddItem(IReorderableListAdaptor adaptor)
        {
            adaptor.Add();
            this.AutoFocusItem(s_ContextControlID, adaptor.Count - 1);

            GUI.changed = true;
            ReorderableListGUI.IndexOfChangedItem = -1;

            var args = new ItemInsertedEventArgs(adaptor, adaptor.Count - 1, false);
            this.OnItemInserted(args);
        }

        /// <summary>
        /// Insert item at specified index and raises the event <see cref="ItemInserted"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        protected void InsertItem(IReorderableListAdaptor adaptor, int itemIndex)
        {
            adaptor.Insert(itemIndex);
            this.AutoFocusItem(s_ContextControlID, itemIndex);

            GUI.changed = true;
            ReorderableListGUI.IndexOfChangedItem = -1;

            var args = new ItemInsertedEventArgs(adaptor, itemIndex, false);
            this.OnItemInserted(args);
        }

        /// <summary>
        /// Duplicate specified item and raises the event <see cref="ItemInserted"/>.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        protected void DuplicateItem(IReorderableListAdaptor adaptor, int itemIndex)
        {
            adaptor.Duplicate(itemIndex);
            this.AutoFocusItem(s_ContextControlID, itemIndex + 1);

            GUI.changed = true;
            ReorderableListGUI.IndexOfChangedItem = -1;

            var args = new ItemInsertedEventArgs(adaptor, itemIndex + 1, true);
            this.OnItemInserted(args);
        }

        /// <summary>
        /// Remove specified item.
        /// </summary>
        /// <remarks>
        /// <para>The event <see cref="ItemRemoving"/> is raised prior to removing item
        /// and allows removal to be cancelled.</para>
        /// </remarks>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="itemIndex">Zero-based index of item.</param>
        /// <returns>
        /// Returns a value of <c>false</c> if operation was cancelled.
        /// </returns>
        protected bool RemoveItem(IReorderableListAdaptor adaptor, int itemIndex)
        {
            var args = new ItemRemovingEventArgs(adaptor, itemIndex);
            this.OnItemRemoving(args);
            if (args.Cancel) {
                return false;
            }

            adaptor.Remove(itemIndex);

            GUI.changed = true;
            ReorderableListGUI.IndexOfChangedItem = -1;

            return true;
        }

        /// <summary>
        /// Remove all items from list.
        /// </summary>
        /// <remarks>
        /// <para>The event <see cref="ItemRemoving"/> is raised for each item prior to
        /// clearing array and allows entire operation to be cancelled.</para>
        /// </remarks>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <returns>
        /// Returns a value of <c>false</c> if operation was cancelled.
        /// </returns>
        protected bool ClearAll(IReorderableListAdaptor adaptor)
        {
            if (adaptor.Count == 0) {
                return true;
            }

            var args = new ItemRemovingEventArgs(adaptor, 0);
            int count = adaptor.Count;
            for (int i = 0; i < count; ++i) {
                args.ItemIndex = i;
                this.OnItemRemoving(args);
                if (args.Cancel) {
                    return false;
                }
            }

            adaptor.Clear();

            GUI.changed = true;
            ReorderableListGUI.IndexOfChangedItem = -1;

            return true;
        }

        /// <summary>
        /// Set count of items in list by adding or removing items.
        /// </summary>
        /// <param name="adaptor">Reorderable list adaptor.</param>
        /// <param name="newCount">New count of items.</param>
        /// <returns>
        /// Returns a value of <c>false</c> if operation was cancelled.
        /// </returns>
        protected bool ResizeList(IReorderableListAdaptor adaptor, int newCount)
        {
            if (newCount < 0) {
                // Do nothing when new count is negative.
                return true;
            }

            int removeCount = Mathf.Max(0, adaptor.Count - newCount);
            int addCount = Mathf.Max(0, newCount - adaptor.Count);

            while (removeCount-- > 0) {
                if (!this.RemoveItem(adaptor, adaptor.Count - 1)) {
                    return false;
                }
            }
            while (addCount-- > 0) {
                this.AddItem(adaptor);
            }

            return true;
        }

        #endregion
    }
}
