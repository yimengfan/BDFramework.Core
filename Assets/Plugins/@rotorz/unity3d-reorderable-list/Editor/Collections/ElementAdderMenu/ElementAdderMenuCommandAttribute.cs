// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Annotate <see cref="IElementAdderMenuCommand{TContext}"/> implementations with a
    /// <see cref="ElementAdderMenuCommandAttribute"/> to associate it with the contract
    /// type of addable elements.
    /// </summary>
    /// <example>
    /// <para>The following source code demonstrates how to add a helper menu command to
    /// the add element menu of a shopping list:</para>
    /// <code language="csharp"><![CDATA[
    /// [ElementAdderMenuCommand(typeof(ShoppingItem))]
    /// public class AddFavoriteShoppingItemsCommand : IElementAdderMenuCommand<ShoppingList>
    /// {
    ///     public AddFavoriteShoppingItemsCommand()
    ///     {
    ///         this.Content = new GUIContent("Add Favorite Items");
    ///     }
    ///
    ///
    ///     public GUIContent Content { get; private set; }
    ///
    ///
    ///     public bool CanExecute(IElementAdder<ShoppingList> elementAdder)
    ///     {
    ///         return true;
    ///     }
    ///
    ///     public void Execute(IElementAdder<ShoppingList> elementAdder)
    ///     {
    ///         // TODO: Add favorite items to the shopping list!
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ElementAdderMenuCommandAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementAdderMenuCommandAttribute"/> class.
        /// </summary>
        /// <param name="contractType">Contract type of addable elements.</param>
        public ElementAdderMenuCommandAttribute(Type contractType)
        {
            this.ContractType = contractType;
        }

        /// <summary>
        /// Gets the contract type of addable elements.
        /// </summary>
        public Type ContractType { get; private set; }
    }
}
