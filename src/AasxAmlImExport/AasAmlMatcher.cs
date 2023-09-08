/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Aml.Engine.CAEX;
using System.Collections.Generic;

namespace AasxAmlImExport
{
    /// <summary>
    /// Maintains a bidirectinal dictionary between AAS Referables and AML / CAEX Objects
    /// </summary>
    public class AasAmlMatcher
    {
        private Dictionary<IReferable, CAEXObject> aasToAml =
            new Dictionary<IReferable, CAEXObject>();

        private Dictionary<CAEXObject, IReferable> amlToAas =
            new Dictionary<CAEXObject, IReferable>();

        public void AddMatch(IReferable aasReferable, CAEXObject amlObject)
        {
            aasToAml.Add(aasReferable, amlObject);
            amlToAas.Add(amlObject, aasReferable);
        }

        public ICollection<IReferable> GetAllAasReferables()
        {
            return aasToAml.Keys;
        }

        public CAEXObject GetAmlObject(IReferable aasReferable)
        {
            if (aasToAml.ContainsKey(aasReferable))
                return aasToAml[aasReferable];
            return null;
        }

        public IReferable GetAasObject(CAEXObject amlObject)
        {
            if (amlToAas.ContainsKey(amlObject))
                return amlToAas[amlObject];
            return null;
        }

        public bool ContainsAasObject(IReferable aasReferable)
        {
            return aasToAml.ContainsKey(aasReferable);
        }

        public bool ContainsAmlObject(CAEXObject amlObject)
        {
            return amlToAas.ContainsKey(amlObject);
        }
    }
}
