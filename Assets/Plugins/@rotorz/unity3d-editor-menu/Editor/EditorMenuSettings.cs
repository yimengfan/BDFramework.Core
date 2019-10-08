// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.EditorExtensions;
using System;
using System.IO;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Project-wide settings for the "@rotorz/unity3d-editor-menu" package.
    /// </summary>
    /// <remarks>
    /// <para>The default <see cref="IEditorMenuPresenter"/> implementation type can be
    /// overridden by creating a settings file at the path "{Project}/Assets/Plugins/PackageData/@rotorz/unity3d-editor-menu/EditorMenuSettings.json"
    /// and then specifying the type as follows:</para>
    /// <code language="json"><![CDATA[
    /// {
    ///   "DefaultPresenterTypeName": "MyNamespace.CustomEditorMenuPresenter"
    /// }
    /// ]]></code>
    /// </remarks>
    public sealed class EditorMenuSettings
    {
        private static EditorMenuSettings s_Instance;


        /// <summary>
        /// Gets the <see cref="EditorMenuSettings"/> instance for the end-user's project.
        /// </summary>
        public static EditorMenuSettings Instance {
            get {
                if (s_Instance == null) {
                    s_Instance = LoadSettings();
                }
                return s_Instance;
            }
        }


        private static EditorMenuSettings LoadSettings()
        {
            EditorMenuSettings settings = new EditorMenuSettings();

            string editorMenuSettingsFilePath = PackageUtility.ResolveDataPathAbsolute("@rotorz/unity3d-editor-menu", null, "EditorMenuSettings.json");
            if (File.Exists(editorMenuSettingsFilePath)) {
                var jsonRaw = File.ReadAllText(editorMenuSettingsFilePath);
                var data = JsonUtility.FromJson<JsonData>(jsonRaw);
                settings.FromSerializable(data);
            }

            return settings;
        }


        private Type defaultPresenterType;


        /// <summary>
        /// Initializes a new instance of the <see cref="EditorMenuSettings"/> class.
        /// </summary>
        public EditorMenuSettings()
        {
            this.defaultPresenterType = this.FallbackDefaultPresenterType;
        }


        /// <summary>
        /// Gets the fallback <see cref="IEditorMenuPresenter"/> implementation type.
        /// </summary>
        public Type FallbackDefaultPresenterType {
            get { return typeof(EditorMenuPresenter_GenericMenu); }
        }

        /// <summary>
        /// Gets the default <see cref="IEditorMenuPresenter"/> implementation type.
        /// </summary>
        public Type DefaultPresenterType {
            get { return this.defaultPresenterType; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
                if (!typeof(IEditorMenuPresenter).IsAssignableFrom(value)) {
                    throw new ArgumentException("Does not implement 'IEditorMenuPresenter' interface.");
                }

                this.defaultPresenterType = value;
            }
        }



        private void FromSerializable(JsonData data)
        {
            if (!string.IsNullOrEmpty(data.DefaultPresenterTypeName)) {
                var type = Type.GetType(data.DefaultPresenterTypeName, throwOnError: false);
                if (type != null && typeof(IEditorMenuPresenter).IsAssignableFrom(type)) {
                    this.defaultPresenterType = type;
                    return;
                }
            }

            this.defaultPresenterType = this.FallbackDefaultPresenterType;
        }



        [Serializable]
        private sealed class JsonData
        {
            public string DefaultPresenterTypeName;
        }
    }
}
