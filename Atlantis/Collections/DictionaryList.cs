// -----------------------------------------------------------------------------
//  <copyright file="DictionaryList.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

namespace Atlantis.Collections;

using System.Collections.Generic;
using JetBrains.Annotations;

/// <summary>
///     Represents a dictionary that contains a list of values for any given key.
/// </summary>
/// <typeparam name="T"></typeparam>
[PublicAPI]
public class DictionaryList<T>
{
    private readonly Dictionary<string, List<T>> _dict;

    public DictionaryList()
    {
        _dict = new Dictionary<string, List<T>>();
    }

    public DictionaryList(IEqualityComparer<string> comparer)
    {
        _dict = new Dictionary<string, List<T>>(comparer);
    }

    public List<T> this[string key]
    {
        get
        {
            if (_dict.TryGetValue(key, out var values))
            {
                return values;
            }

            values = [];
            _dict[key] = values;

            return values;
        }
    }

    public IEnumerable<string> Keys => _dict.Keys;

    public void Add(string key, T value)
    {
        if (!_dict.TryGetValue(key, out var values))
        {
            values = new List<T> { value };
            _dict[key] = values;
        }
        else
        {
            values.Add(value);
        }
    }

    public bool ContainsKey(string key)
    {
        return _dict.ContainsKey(key);
    }

    public bool Remove(string key, T value)
    {
        return _dict.TryGetValue(key, out var values) && values.Remove(value);
    }

    public bool TryGetValue(string key, out List<T> value)
    {
        return _dict.TryGetValue(key, out value);
    }
}
