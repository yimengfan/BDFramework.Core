// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using dnlib.DotNet;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    public interface IGroupByModuleEntity
    {
        GroupByModuleEntityManager Manager { get; set; }

        ModuleDef Module { get; set; }

        EncryptionScopeProvider EncryptionScopeProvider { get; }

        EncryptionScopeInfo EncryptionScope { get; set; }

        void Init();

        void Done();
    }

    public abstract class GroupByModuleEntityBase : IGroupByModuleEntity
    {
        public GroupByModuleEntityManager Manager { get; set; }

        public ModuleDef Module { get; set; }

        public EncryptionScopeInfo EncryptionScope { get; set; }

        public EncryptionScopeProvider EncryptionScopeProvider => Manager.EncryptionScopeProvider;

        public T GetEntity<T>() where T : IGroupByModuleEntity, new()
        {
            return Manager.GetEntity<T>(Module);
        }

        public abstract void Init();

        public abstract void Done();
    }

    public class GroupByModuleEntityManager
    {
        private readonly Dictionary<(ModuleDef, Type), IGroupByModuleEntity> _moduleEntityManagers = new Dictionary<(ModuleDef, Type), IGroupByModuleEntity>();

        public EncryptionScopeProvider EncryptionScopeProvider { get; set; }

        public T GetEntity<T>(ModuleDef mod) where T : IGroupByModuleEntity, new()
        {
            var key = (mod, typeof(T));
            if (_moduleEntityManagers.TryGetValue(key, out var emitManager))
            {
                return (T)emitManager;
            }
            else
            {
                T newEmitManager = new T();
                newEmitManager.Manager = this;
                newEmitManager.Module = mod;
                newEmitManager.EncryptionScope = EncryptionScopeProvider.GetScope(mod);
                newEmitManager.Init();
                _moduleEntityManagers[key] = newEmitManager;
                return newEmitManager;
            }
        }

        public List<T> GetEntities<T>() where T : IGroupByModuleEntity, new()
        {
            var managers = new List<T>();
            foreach (var kv in _moduleEntityManagers)
            {
                if (kv.Key.Item2 == typeof(T))
                {
                    managers.Add((T)kv.Value);
                }
            }
            return managers;
        }

        public void Done<T>() where T : IGroupByModuleEntity, new()
        {
            var managers = GetEntities<T>();
            foreach (var manager in managers)
            {
                manager.Done();
            }
        }
    }
}
