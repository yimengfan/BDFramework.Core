// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rotorz.Games.Collections
{
    /// <summary>
    /// Provides meta information about <see cref="IElementAdderMenuCommand{TContext}"/>
    /// implementations which the <see cref="ElementAdderMenuPopulator{TContext}"/> class
    /// uses to augment editor menus with.
    /// </summary>
    public static class ElementAdderMenuCommandMeta
    {
        private static readonly Dictionary<Type, Dictionary<Type, Type[]>> s_ContextElementContractCommandTypesMaps = new Dictionary<Type, Dictionary<Type, Type[]>>();


        private static Dictionary<Type, Type[]> GetContextElementContractCommandTypesMap<TContext>()
        {
            Dictionary<Type, Type[]> elementContractCommandTypesMap;
            if (!s_ContextElementContractCommandTypesMaps.TryGetValue(typeof(TContext), out elementContractCommandTypesMap)) {
                elementContractCommandTypesMap = new Dictionary<Type, Type[]>();
                s_ContextElementContractCommandTypesMaps[typeof(TContext)] = elementContractCommandTypesMap;
            }
            return elementContractCommandTypesMap;
        }


        /// <summary>
        /// Gets an array of all the instantiatable <see cref="IElementAdderMenuCommand{TContext}"/>
        /// implementations that are annotated with <see cref="ElementAdderMenuCommandAttribute"/>.
        private static IEnumerable<Type> GetAnnotatedCommandTypes<TContext>()
        {
            return
                from type in TypeMeta.DiscoverImplementations<IElementAdderMenuCommand<TContext>>()
                where type.IsClass && type.IsDefined(typeof(ElementAdderMenuCommandAttribute), false)
                select type;
        }

        /// <summary>
        /// Gets an array of the <see cref="IElementAdderMenuCommand{TContext}"/> types
        /// that are associated with the specified <paramref name="elementContractType"/>.
        /// </summary>
        private static Type[] GetAnnotatedCommandTypes<TContext>(Type elementContractType)
        {
            ExceptionUtility.CheckArgumentNotNull(elementContractType, "contractType");

            var contractMap = GetContextElementContractCommandTypesMap<TContext>();
            if (!contractMap.ContainsKey(elementContractType)) {
                contractMap[elementContractType] = (
                    from commandType in GetAnnotatedCommandTypes<TContext>()
                    let attributes = (ElementAdderMenuCommandAttribute[])Attribute.GetCustomAttributes(commandType, typeof(ElementAdderMenuCommandAttribute))
                    where attributes.Any(attribute => attribute.ContractType == elementContractType)
                    select commandType
                ).ToArray();
            }

            return contractMap[elementContractType].ToArray();
        }

        /// <summary>
        /// Gets an array of <see cref="IElementAdderMenuCommand{TContext}"/> instances
        /// that are associated with the specified <paramref name="elementContractType"/>.
        /// </summary>
        /// <typeparam name="TContext">Type of the context object that elements can be added to.</typeparam>
        /// <param name="elementContractType">Contract type of addable elements.</param>
        /// <returns>
        /// An array containing zero or more <see cref="IElementAdderMenuCommand{TContext}"/> instances.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="elementContractType"/> is <c>null</c>.
        /// </exception>
        /// <seealso cref="GetAnnotatedCommandTypes{TContext}(Type)"/>
        public static IElementAdderMenuCommand<TContext>[] InstantiateAnnotatedCommands<TContext>(Type elementContractType)
        {
            return (
                from commandType in GetAnnotatedCommandTypes<TContext>(elementContractType)
                select (IElementAdderMenuCommand<TContext>)Activator.CreateInstance(commandType)
            ).ToArray();
        }
    }
}
