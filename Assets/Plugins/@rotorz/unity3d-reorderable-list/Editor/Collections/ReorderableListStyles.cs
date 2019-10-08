// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.UnityEditorExtensions;
using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Styles for the <see cref="ReorderableListControl"/>.
    /// </summary>
    public sealed class ReorderableListStyles : EditorSingletonScriptableObject
    {
        private static ReorderableListStyles s_Instance;
        private static SkinInfo s_Skin;


        /// <summary>
        /// Gets the one-and-only <see cref="ReorderableListStyles"/> instance.
        /// </summary>
        public static ReorderableListStyles Instance {
            get {
                EditorSingletonUtility.GetAssetInstance<ReorderableListStyles>(ref s_Instance);
                return s_Instance;
            }
        }

        /// <summary>
        /// Gets the current skin.
        /// </summary>
        public static SkinInfo Skin {
            get {
                if (s_Skin == null) {
                    s_Skin = EditorGUIUtility.isProSkin ? Instance.darkSkin : Instance.lightSkin;
                }
                return s_Skin;
            }
        }


        [SerializeField]
        private SkinInfo darkSkin = new SkinInfo();
        [SerializeField]
        private SkinInfo lightSkin = new SkinInfo();


        /// <inheritdoc/>
        protected override void OnInitialize()
        {
            this.Title = new GUIStyle();
            this.Title.border = new RectOffset(2, 2, 2, 1);
            this.Title.margin = new RectOffset(5, 5, 5, 0);
            this.Title.padding = new RectOffset(5, 5, 3, 3);
            this.Title.alignment = TextAnchor.MiddleLeft;
            this.Title.normal.background = Skin.TitleBackground;
            this.Title.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.8f, 0.8f, 0.8f)
                : new Color(0.2f, 0.2f, 0.2f);

            this.Container = new GUIStyle();
            this.Container.border = new RectOffset(2, 2, 2, 2);
            this.Container.margin = new RectOffset(5, 5, 0, 0);
            this.Container.padding = new RectOffset(2, 2, 2, 2);
            this.Container.normal.background = Skin.ContainerBackground;

            this.Container2 = new GUIStyle(this.Container);
            this.Container2.normal.background = Skin.Container2Background;

            this.FooterButton = new GUIStyle();
            this.FooterButton.fixedHeight = 16;
            this.FooterButton.alignment = TextAnchor.MiddleCenter;
            this.FooterButton.normal.background = Skin.Button_Normal;
            this.FooterButton.active.background = Skin.Button_Active;
            this.FooterButton.border = new RectOffset(3, 3, 1, 3);
            this.FooterButton.padding = new RectOffset(2, 2, 0, 2);
            this.FooterButton.clipping = TextClipping.Overflow;

            this.FooterButton2 = new GUIStyle();
            this.FooterButton2.fixedHeight = 18;
            this.FooterButton2.alignment = TextAnchor.MiddleCenter;
            this.FooterButton2.normal.background = Skin.Button2_Normal;
            this.FooterButton2.active.background = Skin.Button2_Active;
            this.FooterButton2.border = new RectOffset(3, 3, 3, 3);
            this.FooterButton2.padding = new RectOffset(2, 2, 2, 2);
            this.FooterButton2.clipping = TextClipping.Overflow;

            this.ItemButton = new GUIStyle();
            this.ItemButton.active.background = Skin.ItemBackground_Active;
            this.ItemButton.imagePosition = ImagePosition.ImageOnly;
            this.ItemButton.alignment = TextAnchor.MiddleCenter;
            this.ItemButton.overflow = new RectOffset(0, 0, -1, 0);
            this.ItemButton.padding = new RectOffset(0, 0, 1, 0);
            this.ItemButton.contentOffset = new Vector2(0, -1f);

            this.SelectedItem = new GUIStyle();
            this.SelectedItem.normal.background = Skin.SelectionBackground;
            this.SelectedItem.normal.textColor = Color.white;
            this.SelectedItem.fontSize = 12;
        }


        /// <summary>
        /// Gets style for title header.
        /// </summary>
        public GUIStyle Title { get; private set; }

        /// <summary>
        /// Gets style for the background of list control.
        /// </summary>
        public GUIStyle Container { get; private set; }
        /// <summary>
        /// Gets an alternative style for the background of list control.
        /// </summary>
        public GUIStyle Container2 { get; private set; }
        /// <summary>
        /// Gets style for footer button.
        /// </summary>
        public GUIStyle FooterButton { get; private set; }
        /// <summary>
        /// Gets an alternative style for footer button.
        /// </summary>
        public GUIStyle FooterButton2 { get; private set; }
        /// <summary>
        /// Gets style for remove item button.
        /// </summary>
        public GUIStyle ItemButton { get; private set; }

        /// <summary>
        /// Gets style for the background of a selected item.
        /// </summary>
        public GUIStyle SelectedItem { get; private set; }

        /// <summary>
        /// Gets color for the horizontal lines that appear between list items.
        /// </summary>
        public Color HorizontalLineColor {
            get { return EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.14f) : new Color(0.59f, 0.59f, 0.59f, 0.55f); }
        }


        [System.Serializable]
        public sealed class SkinInfo
        {
            [SerializeField]
            private Texture2D button_Active = null;
            [SerializeField]
            private Texture2D button_Normal = null;
            [SerializeField]
            private Texture2D button2_Active = null;
            [SerializeField]
            private Texture2D button2_Normal = null;
            [SerializeField]
            private Texture2D container2Background = null;
            [SerializeField]
            private Texture2D containerBackground = null;
            [SerializeField]
            private Texture2D grabHandle = null;
            [SerializeField]
            private Texture2D icon_Add_Active = null;
            [SerializeField]
            private Texture2D icon_Add_Normal = null;
            [SerializeField]
            private Texture2D icon_AddMenu_Active = null;
            [SerializeField]
            private Texture2D icon_AddMenu_Normal = null;
            [SerializeField]
            private Texture2D icon_Menu_Active = null;
            [SerializeField]
            private Texture2D icon_Menu_Normal = null;
            [SerializeField]
            private Texture2D icon_Remove_Active = null;
            [SerializeField]
            private Texture2D icon_Remove_Normal = null;
            [SerializeField]
            private Texture2D itemBackground_Active = null;
            [SerializeField]
            private Texture2D selectionBackground = null;
            [SerializeField]
            private Texture2D titleBackground = null;


            public Texture2D Button_Active {
                get { return this.button_Active; }
            }

            public Texture2D Button_Normal {
                get { return this.button_Normal; }
            }

            public Texture2D Button2_Active {
                get { return this.button2_Active; }
            }

            public Texture2D Button2_Normal {
                get { return this.button2_Normal; }
            }

            public Texture2D Container2Background {
                get { return this.container2Background; }
            }

            public Texture2D ContainerBackground {
                get { return this.containerBackground; }
            }

            public Texture2D GrabHandle {
                get { return this.grabHandle; }
            }

            public Texture2D Icon_Add_Active {
                get { return this.icon_Add_Active; }
            }

            public Texture2D Icon_Add_Normal {
                get { return this.icon_Add_Normal; }
            }

            public Texture2D Icon_AddMenu_Active {
                get { return this.icon_AddMenu_Active; }
            }

            public Texture2D Icon_AddMenu_Normal {
                get { return this.icon_AddMenu_Normal; }
            }

            public Texture2D Icon_Menu_Active {
                get { return this.icon_Menu_Active; }
            }

            public Texture2D Icon_Menu_Normal {
                get { return this.icon_Menu_Normal; }
            }

            public Texture2D Icon_Remove_Active {
                get { return this.icon_Remove_Active; }
            }

            public Texture2D Icon_Remove_Normal {
                get { return this.icon_Remove_Normal; }
            }

            public Texture2D ItemBackground_Active {
                get { return this.itemBackground_Active; }
            }

            public Texture2D SelectionBackground {
                get { return this.selectionBackground; }
            }

            public Texture2D TitleBackground {
                get { return this.titleBackground; }
            }
        }
    }
}
