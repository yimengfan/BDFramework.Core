// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.Reflection;
using Rotorz.Games.UnityEditorExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// An object that can populate <see cref="EditorMenu"/> instances with the commands
    /// and concrete element types of a given context object.
    /// </summary>
    /// <example>
    /// <para>The following example demonstrates how to build and display a menu which
    /// allows the user to add compatible elements to a given context object from a
    /// dropdown menu:</para>
    /// <code language="csharp"><![CDATA[
    /// public class ShoppingListElementAdder : IElementAdder<ShoppingList>
    /// {
    ///     private readonly ShoppingList shoppingList;
    ///
    ///
    ///     public ShoppingListElementAdder(ShoppingList shoppingList)
    ///     {
    ///         this.shoppingList = shoppingList;
    ///         Object = shoppingList;
    ///     }
    ///
    ///
    ///     public ShoppingList Object { get; private set; }
    ///
    ///
    ///     public bool CanAddElement(Type type)
    ///     {
    ///         return true;
    ///     }
    ///
    ///     public object AddElement(Type type)
    ///     {
    ///         var instance = Activator.CreateInstance(type);
    ///         this.shoppingList.Add((ShoppingItem)instance);
    ///         return instance;
    ///     }
    /// }
    ///
    ///
    /// private void DrawAddMenuButton(ShoppingList shoppingList)
    /// {
    ///     if (EditorGUILayout.DropdownButton(new GUIContent("Add Menu"), FocusType.Keyboard)) {
    ///         var menu = new EditorMenu();
    ///
    ///         var elementContractType = typeof(ShoppingItem);
    ///         var elementAdder = new ShoppingListElementAdder(this.shoppingList);
    ///         var elementPopulator = new ElementAdderMenuPopulator<ShoppingList>(elementContractType, elementAdder);
    ///         elementPopulator.Populate(menu);
    ///
    ///         menu.ShowAsDropdown(GUILayoutUtility.GetLastRect());
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// <typeparam name="TContext">Type of the context object that elements can be added to.</typeparam>
    public sealed class ElementAdderMenuPopulator<TContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementAdderMenuPopulator{TContext}"/> class.
        /// </summary>
        /// <param name="elementContractType">Contract type of addable elements.</param>
        /// <param name="elementAdder">Object that is used to add new elements to some
        /// context object.</param>
        public ElementAdderMenuPopulator(Type elementContractType, IElementAdder<TContext> elementAdder)
        {
            ExceptionUtility.CheckArgumentNotNull(elementContractType, "contractType");

            this.ElementContractType = elementContractType;
            this.ElementAdder = elementAdder;
            this.TypeFilters = new List<Func<Type, bool>>();
        }


        /// <summary>
        /// Gets the contract type of the elements that can be included in the menu.
        /// </summary>
        public Type ElementContractType { get; private set; }

        /// <summary>
        /// Gets the <see cref="IElementAdder{TContext}"/> that is used when adding new
        /// elements to some context object.
        /// </summary>
        public IElementAdder<TContext> ElementAdder { get; private set; }

        /// <summary>
        /// Gets the list of filter functions that are used when determining whether
        /// types can be included when populating menu.
        /// </summary>
        public IList<Func<Type, bool>> TypeFilters { get; private set; }

        /// <summary>
        /// Gets or sets a filter function that formats type names for display. Assign a
        /// value of <c>null</c> to assume the default formatting.
        /// </summary>
        public Func<Type, string> TypeDisplayNameFormatter { get; set; }


        /// <summary>
        /// Populates a menu with element adder commands and concrete element types.
        /// </summary>
        /// <remarks>
        /// <para>Use <see cref="PopulateWithCommands(EditorMenu)"/> or <see cref="PopulateWithConcreteTypes(EditorMenu)"/>
        /// instead for greater control over how the editor menu is populated.</para>
        /// </remarks>
        /// <param name="menu">Editor menu.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="menu"/> is <c>null</c>.
        /// </exception>
        public void Populate(EditorMenu menu)
        {
            this.PopulateWithCommands(menu);
            this.PopulateWithConcreteTypes(menu);
        }


        /// <summary>
        /// Populates a menu with element adder commands.
        /// </summary>
        /// <param name="menu">Editor menu.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="menu"/> is <c>null</c>.
        /// </exception>
        public void PopulateWithCommands(EditorMenu menu)
        {
            ExceptionUtility.CheckArgumentNotNull(menu, "menu");

            menu.AddSeparator();

            foreach (var command in ElementAdderMenuCommandMeta.InstantiateAnnotatedCommands<TContext>(this.ElementContractType)) {
                menu.AddCommand(command.FullPath)
                    .Enabled(this.ElementAdder != null && command.CanExecute(this.ElementAdder))
                    .Action(command.Execute, this.ElementAdder);
            }
        }

        /// <summary>
        /// Populates a menu with concrete element types.
        /// </summary>
        /// <param name="menu">Editor menu.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="menu"/> is <c>null</c>.
        /// </exception>
        public void PopulateWithConcreteTypes(EditorMenu menu)
        {
            ExceptionUtility.CheckArgumentNotNull(menu, "menu");

            menu.AddSeparator();

            foreach (var concreteType in ApplyTypeFilter(TypeMeta.DiscoverImplementations(this.ElementContractType))) {
                menu.AddCommand(this.FormatTypeDisplayName(concreteType))
                    .Enabled(this.ElementAdder != null && this.ElementAdder.CanAddElement(concreteType))
                    .Action(() => {
                        if (this.ElementAdder.CanAddElement(concreteType)) {
                            this.ElementAdder.AddElement(concreteType);
                        }
                    });
            }
        }


        private string FormatTypeDisplayName(Type type)
        {
            var formatter = this.TypeDisplayNameFormatter;
            return formatter != null
                ? formatter.Invoke(type)
                : TypeMeta.NicifyName(type.Name);
        }

        private IEnumerable<Type> ApplyTypeFilter(IEnumerable<Type> types)
        {
            return from type in types
                   where this.IsTypeFiltered(type)
                   select type;
        }

        private bool IsTypeFiltered(Type type)
        {
            bool wasExcludedByFilter = this.TypeFilters.Any(filter => !filter.Invoke(type));
            return !wasExcludedByFilter;
        }
    }
}
