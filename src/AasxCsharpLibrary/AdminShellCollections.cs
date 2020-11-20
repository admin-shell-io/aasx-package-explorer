﻿/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
