/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable TypeParameterCanBeVariant

namespace AasxFormatCst
{
    public interface IUniqueness<T>
    {
        bool EqualsForUniqueness(T other);
    }

    public class ListOfUnique<T> : List<T> where T : IUniqueness<T>
    {
        public ListOfUnique() : base() { }
        public ListOfUnique(T[] arr) : base(arr) { }

        public T FindExisting(T other)
        {
            var exist = this.Find((x) => x.EqualsForUniqueness(other));
            return exist;
        }

        public void AddIfUnique(T other)
        {
            var exist = FindExisting(other);
            if (exist != null)
                return;
            this.Add(other);
        }
    }
}
