/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
using System.Runtime.Intrinsics.X86;

namespace Extensions
{
    public static class ExtendIHasSemantics
	{
        public static string GetConceptDescriptionId(this IHasSemantics ihs)
        {
            if (ihs?.SemanticId != null 
                && ihs.SemanticId.IsValid() == true
                && ihs.SemanticId.Count() == 1
				&& (ihs.SemanticId.Keys[0].Type == KeyTypes.ConceptDescription
                    || ihs.SemanticId.Keys[0].Type == KeyTypes.Submodel
					|| ihs.SemanticId.Keys[0].Type == KeyTypes.GlobalReference)
				&& ihs.SemanticId.Keys[0].Value != null
				&& ihs.SemanticId.Keys[0].Value.Trim().Length > 0)
            {
                return ihs.SemanticId.Keys[0].Value;
			}
            return null;
        }
    }
}
