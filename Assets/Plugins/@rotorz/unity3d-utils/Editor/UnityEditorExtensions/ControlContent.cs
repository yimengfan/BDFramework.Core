// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// A structure that holds editor control content. When used correctly the wrapped
    /// <see cref="UnityEngine.GUIContent"/> instance will be recycled which helps to
    /// improve editor GUI performance by avoiding allocation / garbage collection.
    /// </summary>
    /// <remarks>
    /// <para>This structure implements the <see cref="IDisposable"/> interface and is
    /// intended to be used with the <c>using</c> language construct:</para>
    /// <code language="csharp"><![CDATA[
    /// using (var content = ControlContent.Basic(
    ///     "Some setting label",
    ///     "A tooltip revealing more detail about the setting toggle."
    /// )) {
    ///     value = EditorGUILayout.ToggleLeft(content, value);
    /// }
    /// ]]></code>
    /// <para>As a reminder, multiple <c>using</c> constructs can be defined adjacently:</para>
    /// <code language="csharp"><![CDATA[
    /// using (var content1 = ControlContent.Basic("Some content 1"))
    /// using (var content2 = ControlContent.Basic("Some content 2")) {
    ///     Foo(content1, content2);
    /// }
    /// ]]></code>
    /// </remarks>
    public struct ControlContent : IDisposable
    {
        #region User-defined Display Preference

        private static readonly string EDITORPREFS_KEY_TRAILINGTIPSVISIBLE = typeof(ControlContent).FullName + ".TrailingTipsVisible";


        private static bool s_TrailingTipsVisibleInitialized = false;
        private static bool s_TrailingTipsVisible;


        /// <summary>
        /// Gets or sets whether trailing tips are currently visible in user interfaces.
        /// This property is persisted using <see cref="UnityEditor.EditorPrefs"/> and
        /// can be user-controlled when exposed in user interfaces with some sort of
        /// "Show Tips" toggle control.
        /// </summary>
        public static bool TrailingTipsVisible {
            get {
                if (!s_TrailingTipsVisibleInitialized) {
                    s_TrailingTipsVisible = EditorPrefs.GetBool(EDITORPREFS_KEY_TRAILINGTIPSVISIBLE, false);
                    s_TrailingTipsVisibleInitialized = true;
                }
                return s_TrailingTipsVisible;
            }
            set {
                if (value == s_TrailingTipsVisible) {
                    return;
                }

                s_TrailingTipsVisibleInitialized = true;
                s_TrailingTipsVisible = value;
                EditorPrefs.SetBool(EDITORPREFS_KEY_TRAILINGTIPSVISIBLE, s_TrailingTipsVisible);

                OnTrailingTipsVisibleChanged();
            }
        }


        /// <summary>
        /// Occurs when the value of the <see cref="TrailingTipsVisible"/> property
        /// changes. This is usefulw hen you want to repaint user interfaces whenever
        /// this preference is changed.
        /// </summary>
        public static event Action TrailingTipsVisibleChanged;

        private static void OnTrailingTipsVisibleChanged()
        {
            var handler = TrailingTipsVisibleChanged;
            if (handler != null) {
                handler.Invoke();
            }
        }

        #endregion


        #region GUIContent Pooling

        private static readonly Stack<GUIContent> s_GUIContentPool = new Stack<GUIContent>();


        private static GUIContent SpawnGUIContent(string labelText, Texture image, string tipText)
        {
            GUIContent content;
            if (s_GUIContentPool.Count != 0) {
                content = s_GUIContentPool.Pop();
            }
            else {
                content = new GUIContent();
            }

            content.text = labelText;
            content.image = image;
            content.tooltip = tipText;

            return content;
        }

        private static void DespawnGUIContent(GUIContent content)
        {
            ResetContent(content);
            s_GUIContentPool.Push(content);
        }

        private static void ResetContent(GUIContent content)
        {
            content.text = null;
            content.image = null;
            content.tooltip = null;
        }

        #endregion


        /// <summary>
        /// Gets basic control content with label text, an image and optional tip text.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.Basic(labelText, image, tipText)) {
        ///     // Do something with the content...
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="labelText">Label text.</param>
        /// <param name="image">Image texture.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent Basic(string labelText, Texture image, string tipText = null)
        {
            ControlContent content = default(ControlContent);

            content.LabelContent = SpawnGUIContent(labelText, image, tipText);
            content.TrailingTipText = !string.IsNullOrEmpty(tipText)
                ? tipText
                : null;

            return content;
        }

        /// <summary>
        /// Gets basic control content with label text and optional tip text.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.Basic(labelText, tipText)) {
        ///     // Do something with the content...
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="labelText">Label text.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent Basic(string labelText, string tipText = null)
        {
            return Basic(labelText, null, tipText);
        }

        /// <summary>
        /// Gets basic control content with an image and optional tip text.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.Basic(image, tipText)) {
        ///     // Do something with the content...
        ///     GUILayout.Label(content);
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="image">Image texture.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent Basic(Texture image, string tipText = null)
        {
            return Basic(null, image, tipText);
        }


        /// <summary>
        /// Gets control content with label text, an image and trailing tip text that is
        /// either shown as a tooltip on the main content or as a trailing tip below the
        /// control content when <see cref="TrailingTipsVisible"/> is <c>true</c>.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.WithTrailableTip(labelText, image, tipText)) {
        ///     // Do something with the content...
        ///     GUILayout.Label(content);
        ///
        ///     // The following will automatically display the trailing tip when the
        ///     // `ControlContent.TrailingTipsVisible` property is `true`.
        ///     ExtraEditorGUI.TrailingTip(content);
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="labelText">Label text.</param>
        /// <param name="image">Image texture.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent WithTrailableTip(string labelText, Texture image, string tipText)
        {
            ControlContent content = default(ControlContent);

            if (TrailingTipsVisible) {
                content.LabelContent = SpawnGUIContent(labelText, image, null);
                content.TrailingTipText = !string.IsNullOrEmpty(tipText)
                    ? tipText
                    : null;
            }
            else {
                content.LabelContent = SpawnGUIContent(labelText, image, tipText);
                content.TrailingTipText = null;
            }

            return content;
        }

        /// <summary>
        /// Gets control content with label text and trailing tip text that is either
        /// shown as a tooltip on the main content or as a trailing tip below the control
        /// content when <see cref="TrailingTipsVisible"/> is <c>true</c>.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.WithTrailableTip(labelText, tipText)) {
        ///     // Do something with the content...
        ///     GUILayout.Label(content);
        ///
        ///     // The following will automatically display the trailing tip when the
        ///     // `ControlContent.TrailingTipsVisible` property is `true`.
        ///     ExtraEditorGUI.TrailingTip(content);
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="labelText">Label text.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent WithTrailableTip(string labelText, string tipText)
        {
            return WithTrailableTip(labelText, null, tipText);
        }

        /// <summary>
        /// Gets control content with an image and trailing tip text that is either shown
        /// as a tooltip on the main content or as a trailing tip below the control
        /// content when <see cref="TrailingTipsVisible"/> is <c>true</c>.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// using (var content = ControlContent.WithTrailableTip(image, tipText)) {
        ///     // Do something with the content...
        ///     GUILayout.Label(content);
        ///
        ///     // The following will automatically display the trailing tip when the
        ///     // `ControlContent.TrailingTipsVisible` property is `true`.
        ///     ExtraEditorGUI.TrailingTip(content);
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="image">Image texture.</param>
        /// <param name="tipText">Tip text.</param>
        /// <returns>
        /// The control content.
        /// </returns>
        public static ControlContent WithTrailableTip(Texture image, string tipText)
        {
            return WithTrailableTip(null, image, tipText);
        }


        /// <summary>
        /// Gets the main control label content.
        /// </summary>
        /// <remarks>
        /// <para>Only includes tip text when <see cref="TrailingTipsVisible"/> is
        /// <c>false</c> since the tip will otherwise be shown as a trailing tip below.</para>
        /// </remarks>
        public GUIContent LabelContent { get; private set; }

        /// <summary>
        /// Gets the trailing tip text.
        /// </summary>
        public string TrailingTipText { get; private set; }


        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.LabelContent != null && !ReferenceEquals(this.LabelContent, GUIContent.none)) {
                var content = this.LabelContent;
                DespawnGUIContent(content);
                this.LabelContent = GUIContent.none;
            }
        }


        /// <summary>
        /// Implicitly converts <see cref="ControlContent"/> into <see cref="GUIContent"/>.
        /// </summary>
        /// <param name="content">The control content.</param>
        public static implicit operator GUIContent(ControlContent content)
        {
            return content.LabelContent;
        }
    }
}
