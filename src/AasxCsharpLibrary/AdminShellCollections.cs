/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using System.Linq;

namespace AdminShellNS
{
    /// <summary>
    /// Just add some convenience methods to <c>Dictionary</c>
    /// Note: Not an extension class in order to not interfere with really 
    /// commonly used standard class.
    /// </summary>
    public class ConvenientDictionary<K, V> : Dictionary<K, V>
    {
        public V GetValueOrDefault(K key)
        {
            if (key != null && this.ContainsKey(key))
                return this[key];
            return default(V);
        }
    }

    public class MultiValueDictionary<K, V>
    {
        private Dictionary<K, List<V>> dict = new Dictionary<K, List<V>>();
        
        public void Add(K key, V value)
        {
            if (dict.TryGetValue(key, out var list))
                list.Add(value);
            else
                dict.Add(key, new List<V> { value });
        }

        public void Remove(K key)
        {
            if (dict.ContainsKey(key))
                dict.Remove(key);
        }

        public bool ContainsKey(K key) => dict.ContainsKey(key);

        public List<V> this[K key] => dict[key];

        public IEnumerable<List<V>> ValueLists
        {
            get
            {
                return dict.Values;
            }
        }

        public IEnumerable<V> Values
        {
            get
            {
                foreach (var vl in dict.Values)
                    foreach (var v in vl)
                        yield return v;
            }
        }

        public IEnumerable<K> Keys
		{
			get
			{
				return dict.Keys;
			}
		}

		public void Clear() => dict.Clear();

        public IEnumerable<V> All(K key)
        {
            if (!dict.ContainsKey(key))
                yield break;
            foreach (var x in dict[key])
                yield return x;
        }
    }

    public class DoubleSidedDict<T1, T2>
    {
        private Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
        private Dictionary<T2, T1> _backward = new Dictionary<T2, T1>();

        public void AddPair(T1 item1, T2 item2)
        {
            _forward.Add(item1, item2);
            _backward.Add(item2, item1);
        }

        public bool Contains1(T1 key1) => _forward.ContainsKey(key1);
        public bool Contains2(T2 key2) => _backward.ContainsKey(key2);

        public T2 Get2(T1 key1) => _forward[key1];
        public T1 Get1(T2 key2) => _backward[key2];

        public T2 Get2OrDefault(T1 key1)
            => (key1 != null && _forward.ContainsKey(key1)) ? _forward[key1] : default(T2);
        public T1 Get1OrDefault(T2 key2)
            => (key2 != null && _backward.ContainsKey(key2)) ? _backward[key2] : default(T1);

        public void Clear() { _forward.Clear(); _backward.Clear(); }
    }

    public class IntValueDictionary<K> : Dictionary<K, int>
    {
        public void IncKey(K key)
        {
            if (!this.ContainsKey(key))
                this.Add(key, 1);
            else
            {
                var i = this[key];
                this.Remove(key);
                this.Add(key, i + 1);
            }
        }
    }
}
