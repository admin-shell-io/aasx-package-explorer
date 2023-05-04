/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;

namespace AdminShellNS
{
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

        public bool ContainsKey(K key) => dict.ContainsKey(key);

        public List<V> this[K key] => dict[key];

        public IEnumerable<List<V>> Keys
        {
            get
            {
                return dict.Values;
            }
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
}
