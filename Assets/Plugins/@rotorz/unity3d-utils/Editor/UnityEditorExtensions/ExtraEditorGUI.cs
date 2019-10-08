// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Extra GUI functionality for editor interfaces.
    /// </summary>
    public static class ExtraEditorGUI
    {
        static ExtraEditorGUI()
        {
            InitSpecial();
        }


        /// <summary>
        /// Indicates that no layout options are to be specified.
        /// </summary>
        /// <remarks>
        /// <para>Avoids implied allocation of empty arrays when invoking GUI layout functions.</para>
        /// </remarks>
        internal static readonly GUILayoutOption[] None = null;


        #region Special

        private static void InitSpecial()
        {
            var tyGUIClip = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
            if (tyGUIClip != null) {
                var piVisibleRect = tyGUIClip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (piVisibleRect != null) {
                    var getMethod = piVisibleRect.GetGetMethod(true) ?? piVisibleRect.GetGetMethod(false);
                    s_VisibleRect = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getMethod);
                }
            }
        }

        private static Func<Rect> s_VisibleRect;

        /// <summary>
        /// Gets rectangle of visible GUI area (TopmostRect + scrollViewOffsets).
        /// </summary>
        public static Rect VisibleRect {
            get { return s_VisibleRect(); }
        }

        #endregion


        #region Labels

        /// <summary>
        /// Output description text using small label font.
        /// </summary>
        /// <param name="label">Label text.</param>
        public static void MiniFieldDescription(string label)
        {
            Color restore = GUI.contentColor;
            GUI.color = new Color32(92, 92, 92, 255);
            GUILayout.Label(label, ExtraEditorStyles.Instance.WhiteWordWrappedMiniLabel, None);
            GUI.color = restore;
        }

        #endregion


        #region Link Buttons

        private static int s_HoverControlID;

        internal static bool LinkButton(Rect position, string text, GUIStyle style)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            bool isMouseOverControl = position.Contains(Event.current.mousePosition);

            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (isMouseOverControl) {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (controlID == GUIUtility.hotControl) {
                        Event.current.Use();
                        if (isMouseOverControl) {
                            return true;
                        }
                        else {
                            s_HoverControlID = 0;
                        }
                    }
                    break;

                case EventType.MouseMove:
                    int newHoverControlID = s_HoverControlID;

                    if (isMouseOverControl) {
                        newHoverControlID = controlID;
                    }
                    else if (s_HoverControlID == controlID) {
                        newHoverControlID = 0;
                    }

                    if (newHoverControlID != s_HoverControlID) {
                        s_HoverControlID = newHoverControlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (controlID == GUIUtility.hotControl) {
                        s_HoverControlID = isMouseOverControl ? controlID : 0;
                        Event.current.Use();
                    }
                    break;

                case EventType.Repaint:
                    Color restoreColor = GUI.color;
                    GUI.color = ExtraEditorStyles.Skin.LinkColor;
                    style.Draw(position, text, isMouseOverControl, false, false, false);
                    GUI.color = restoreColor;
                    break;
            }

            return false;
        }

        internal static bool MetaLinkButton(Rect position, string text)
        {
            return LinkButton(position, text, ExtraEditorStyles.Instance.MetaLinkButton);
        }

        #endregion


        #region Icon Buttons

        private static readonly int s_IconButtonHint = "_IconButton_".GetHashCode();


        public static bool IconButton(Rect position, bool visible, Texture2D iconNormal, Texture2D iconActive, GUIStyle style)
        {
            int controlID = GUIUtility.GetControlID(s_IconButtonHint, FocusType.Passive);
            bool result = false;

            position.height += 1;

            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    // Do not allow button to be pressed using right mouse button since
                    // context menu should be shown instead!
                    if (GUI.enabled && Event.current.button != 1 && position.Contains(Event.current.mousePosition)) {
                        GUIUtility.hotControl = controlID;
                        GUIUtility.keyboardControl = 0;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) {
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        GUIUtility.hotControl = 0;
                        result = position.Contains(Event.current.mousePosition);
                        Event.current.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (visible) {
                        bool isActive = GUIUtility.hotControl == controlID && position.Contains(Event.current.mousePosition);
                        using (var tempContent = ControlContent.Basic(isActive ? iconActive : iconNormal)) {
                            position.height -= 1;
                            style.Draw(position, tempContent, isActive, isActive, false, false);
                        }
                    }
                    break;
            }

            return result;
        }

        public static bool IconButton(Rect position, Texture2D iconNormal, Texture2D iconActive, GUIStyle style)
        {
            return IconButton(position, true, iconNormal, iconActive, style);
        }

        #endregion


        #region Prefix Labels

        /// <summary>
        /// Prefix label to show above control.
        /// </summary>
        /// <param name="text">Text for label.</param>
        /// <param name="style">Style for prefix label.</param>
        public static void AbovePrefixLabel(string text, GUIStyle style)
        {
            using (var labelContent = ControlContent.Basic(text)) {
                Rect position = GUILayoutUtility.GetRect(labelContent, style);
                EditorGUI.HandlePrefixLabel(position, position, labelContent, 0, style);
            }
        }

        /// <summary>
        /// Prefix label to show above control.
        /// </summary>
        /// <param name="text">Text for label.</param>
        public static void AbovePrefixLabel(string text)
        {
            AbovePrefixLabel(text, EditorStyles.label);
        }

        /// <summary>
        /// Prefix label to display above multi-part field.
        /// </summary>
        /// <example>
        /// <para>Example of control which behaves similar to <c>EditorGUI.Vector3Field</c>:</para>
        /// <code language="csharp"><![CDATA[
        /// ExtraEditorGUI.MultiPartPrefixLabel("Several Values:");
        /// GUILayout.BeginHorizontal();
        /// a = EditorGUILayout.IntField(a);
        /// b = EditorGUILayout.IntField(b);
        /// c = EditorGUILayout.IntField(c);
        /// GUILayout.EndHorizontal();
        /// ]]></code>
        /// </example>
        /// <param name="text">Text for label.</param>
        public static void MultiPartPrefixLabel(string text)
        {
            AbovePrefixLabel(text);
            GUIUtility.GetControlID(FocusType.Keyboard);
        }

        #endregion


        #region Separators

        private static void SeparatorHelper(int marginTop, int marginBottom, int thickness, Color color)
        {
            Rect position = GUILayoutUtility.GetRect(0, marginTop + thickness + marginBottom, None);

            if (Event.current.type == EventType.Repaint) {
                position.y += marginTop;
                position.height = thickness;

                position = EditorGUI.IndentedRect(position);

                Color restoreColor = GUI.color;
                GUI.color = color;
                ExtraEditorStyles.Instance.Separator.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }

        /// <summary>
        /// Draw splitter of specified color.
        /// </summary>
        /// <param name="color">Color for splitter.</param>
        /// <param name="marginTop">Margin above splitter.</param>
        /// <param name="marginBottom">Margin below splitter.</param>
        /// <param name="thickness">Thickness of splitter in pixels.</param>
        public static void Separator(Color color, int marginTop = 3, int marginBottom = 3, int thickness = 1)
        {
            SeparatorHelper(marginTop, marginBottom, thickness, color);
        }

        /// <summary>
        /// Draw splitter of specified color.
        /// </summary>
        /// <param name="position">Position of which to draw splitter.</param>
        /// <param name="color">Color for splitter.</param>
        public static void Separator(Rect position, Color color)
        {
            if (Event.current.type == EventType.Repaint) {
                Color restoreColor = GUI.color;
                GUI.color = color;
                ExtraEditorStyles.Instance.Separator.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }

        /// <summary>
        /// Draw simple grey splitter.
        /// </summary>
        /// <param name="marginTop">Margin above splitter.</param>
        /// <param name="marginBottom">Margin below splitter.</param>
        /// <param name="thickness">Thickness of splitter in pixels.</param>
        public static void Separator(int marginTop = 3, int marginBottom = 3, int thickness = 1)
        {
            SeparatorHelper(marginTop, marginBottom, thickness, ExtraEditorStyles.Skin.SeparatorColor);
        }

        /// <summary>
        /// Draw simple grey splitter (lighter than <see cref="Separator(int, int, int)"/>).
        /// </summary>
        /// <param name="marginTop">Margin above splitter.</param>
        /// <param name="marginBottom">Margin below splitter.</param>
        /// <param name="thickness">Thickness of splitter in pixels.</param>
        public static void SeparatorLight(int marginTop = 3, int marginBottom = 3, int thickness = 1)
        {
            SeparatorHelper(marginTop, marginBottom, thickness, ExtraEditorStyles.Skin.SeparatorLightColor);
        }

        /// <summary>
        /// Draw simple grey splitter.
        /// </summary>
        /// <param name="position">Position of which to draw splitter.</param>
        public static void Separator(Rect position)
        {
            Separator(position, ExtraEditorStyles.Skin.SeparatorColor);
        }

        /// <summary>
        /// Draw simple light grey splitter (lighter than <see cref="Separator(Rect)"/>).
        /// </summary>
        /// <param name="position">Position of which to draw splitter.</param>
        public static void SeparatorLight(Rect position)
        {
            if (Event.current.type == EventType.Repaint) {
                Color restoreColor = GUI.color;
                GUI.color = ExtraEditorStyles.Skin.SeparatorLightColor;
                ExtraEditorStyles.Instance.Separator.Draw(position, false, false, false, false);
                GUI.color = restoreColor;
            }
        }

        #endregion


        #region Toggle Left Controls

        public static void ToggleLeft(Rect position, SerializedProperty prop, GUIContent label)
        {
            bool hasMultipleDifferentValues = prop.hasMultipleDifferentValues;
            bool value = hasMultipleDifferentValues ? false : prop.boolValue;

            bool restoreShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.ToggleLeft(position, label, value);
            if (EditorGUI.EndChangeCheck()) {
                prop.boolValue = value;
            }

            EditorGUI.showMixedValue = restoreShowMixedValue;
        }

        public static void ToggleLeft(Rect position, SerializedProperty prop, string label)
        {
            using (var labelContent = ControlContent.Basic(label)) {
                ToggleLeft(position, prop, labelContent);
            }
        }

        public static void ToggleLeft(SerializedProperty prop, GUIContent label)
        {
            Rect position = EditorGUILayout.GetControlRect(true);
            ToggleLeft(position, prop, label);
        }

        public static void ToggleLeft(SerializedProperty prop, string label)
        {
            using (var labelContent = ControlContent.Basic(label)) {
                ToggleLeft(prop, labelContent);
            }
        }

        #endregion


        #region Vertical Splitter

        private static int s_AnchorContainerListWidth;
        private static int s_AnchorMousePosition;

        /// <summary>
        /// Handles interaction with a vertical splitter in a user interface. This control
        /// does not paint a visual representation of the vertical splitter; consider using
        /// <see cref="Separator(Rect)"/> for that.
        /// </summary>
        /// <param name="position">Position of vertical splitter in GUI.</param>
        /// <param name="width">Current width of vertical panel.</param>
        /// <param name="defaultWidth">Default width of vertical panel is restored when user
        /// double-clicks on the vertical splitter control.</param>
        /// <returns>
        /// New width of vertical panel.
        /// </returns>
        public static int VerticalSplitter(Rect position, int width, int defaultWidth)
        {
            position.x -= 3;
            position.width += 6;

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            EditorGUIUtility.AddCursorRect(position, MouseCursor.SplitResizeLeftRight);

            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition)) {
                        if (Event.current.clickCount == 2) {
                            width = defaultWidth;
                        }
                        else {
                            GUIUtility.hotControl = controlID;
                            s_AnchorContainerListWidth = width;
                            s_AnchorMousePosition = (int)Event.current.mousePosition.x;
                        }
                        GUIUtility.keyboardControl = 0;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) {
                        int mousePosition = (int)Event.current.mousePosition.x;
                        width = s_AnchorContainerListWidth + (mousePosition - s_AnchorMousePosition);
                        Event.current.Use();
                    }
                    break;
            }

            return width;
        }

        #endregion


        #region Trailing Tips

        public static void TrailingTip(string tipText)
        {
            if (string.IsNullOrEmpty(tipText)) {
                return;
            }

            using (var labelContent = ControlContent.Basic(tipText)) {
                Rect position = GUILayoutUtility.GetRect(labelContent, ExtraEditorStyles.Instance.TrailingTip);
                position.y -= 2;

                EditorGUI.LabelField(position, labelContent, ExtraEditorStyles.Instance.TrailingTip);
            }
        }

        public static void TrailingTip(ControlContent content)
        {
            TrailingTip(content.TrailingTipText);
        }

        #endregion


        #region Texture Drawing

        private static GUIStyle s_DrawTextureTempStyle = new GUIStyle();


        /// <summary>
        /// Draw texture using <see cref="GUIStyle"/> to workaround bug in Unity where
        /// <see cref="GUI.DrawTexture"/> flickers when embedded inside a property drawer.
        /// </summary>
        /// <param name="position">Position of which to draw texture in space of GUI.</param>
        /// <param name="texture">Texture.</param>
        public static void DrawTexture(Rect position, Texture2D texture)
        {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            s_DrawTextureTempStyle.normal.background = texture;
            s_DrawTextureTempStyle.Draw(position, GUIContent.none, false, false, false, false);
            s_DrawTextureTempStyle.normal.background = null;
        }

        #endregion


        #region Input Handling

        /// <summary>
        /// Test for "Return" key-down event; this is useful for accepting input.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// if (ExtraEditorGUI.AcceptKeyboardReturn()) {
        ///     this.DoSomething();
        /// }
        /// ]]></code>
        /// </example>
        /// <returns>
        /// A value of <c>true</c> if return key was used to accept user input; otherwise,
        /// a value of <c>false</c>.
        /// </returns>
        public static bool AcceptKeyboardReturn()
        {
            if (Event.current.rawType == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) {
                GUIUtility.keyboardControl = 0;
                Event.current.Use();
                return true;
            }
            return false;
        }

        #endregion


        #region Metrics

        /// <summary>
        /// Convert rectangle from window space to screen space.
        /// </summary>
        /// <param name="guiRect">GUI Rectangle.</param>
        /// <returns>
        /// Screen rectangle.
        /// </returns>
        public static Rect GUIToScreenRect(Rect guiRect)
        {
            Vector2 screenPoint = EditorGUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            return new Rect(screenPoint.x, screenPoint.y, guiRect.width, guiRect.height);
        }

        #endregion
    }
}
