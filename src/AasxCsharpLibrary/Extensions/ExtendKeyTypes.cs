/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/
namespace Extensions
{
    public static class ExtendKeyTypes
    {
        public static bool IsSME(this KeyTypes keyType)
        {
            foreach (var kt in Constants.AasSubmodelElementsAsKeys)
                if (kt.HasValue && kt.Value == keyType)
                    return true;
            return false;
        }
    }
}
