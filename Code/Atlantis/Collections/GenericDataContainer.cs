// -----------------------------------------------------------------------------
//  <copyright file="GenericDataContainer.cs" author="Zack Loveless">
//      Copyright (c) Zack Loveless All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    ///     <para>Represents a container to store generic pieces of data of any data type.</para>
    /// </summary>
    public class GenericDataContainer
    {
        private readonly IDictionary<string, object> _data;
        private readonly ISet<string> _readonlyValues;

        public GenericDataContainer() : this(StringComparer.OrdinalIgnoreCase)
        {
        }

        public GenericDataContainer(IEqualityComparer<string> equalityComparer)
        {
            _data           = new ConcurrentDictionary<string, object>(equalityComparer);
            _readonlyValues = new HashSet<string>(equalityComparer);
        }

        /// <summary>
        ///     <para>Returns a boolean value indicating whether the specified key is readonly.</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsReadOnly(string key)
        {
            return _readonlyValues.Contains(key);
        }

        /// <summary>
        ///     <para>Gets the associated value with the specified key.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default(T))
        {
            object value;
            if (!_data.TryGetValue(key, out value))
            {
                return defaultValue;
            }

            return (T)value;
        }

        /// <summary>
        ///     <para>Gets the associated value with the specified key.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="value"></param>
        public void TryGetValue<T>(string key, T defaultValue, out T value)
        {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            // Not readable as a ternary operator.
            if (_data.ContainsKey(key))
            {
                value = Get(key, defaultValue);
            }
            else
            {
                value = defaultValue;
            }
        }

        /// <summary>
        ///     <para>Sets the value with the associated key.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="setAsReadonly">Indicates whether to set the specified key to be readonly.</param>
        /// <param name="ignoreReadonly">Indicates whether to ignore readonly prefixes of data keys.</param>
        public void Set<T>(string key, T value, bool setAsReadonly = false, bool ignoreReadonly = false)
        {
            if (IsReadOnly(key) && !ignoreReadonly)
            {
                throw new InvalidOperationException($"The specified data value '{key}' cannot be modified.");
            }
            
            _data[key] = value;

            if (setAsReadonly && !IsReadOnly(key))
            {
                _readonlyValues.Add(key);
            }
        }
        
        /// <summary>
        ///     <para>Unflags the specified key as read-only.</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool UnsetReadOnlyKey(string key)
        {
            if (_readonlyValues.Contains(key))
            {
                return _readonlyValues.Remove(key);
            }

            return false;
        }
    }
}
