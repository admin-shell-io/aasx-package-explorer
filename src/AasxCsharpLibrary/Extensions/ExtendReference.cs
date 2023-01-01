using AasCore.Aas3_0_RC02;
using AdminShellNS.Exceptions;
using AdminShellNS.Extenstions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    public static class ExtendReference
    {
        #region AasxPackageExplorer

        public static AasElementSelfDescription GetSelfDescription(this Reference reference)
        {
            return new AasElementSelfDescription("Reference", "Rfc", null, null);
        }

        public static bool IsValid(this Reference reference)
        {
            return reference.Keys != null && !reference.Keys.IsEmpty();
        }

        /// <summary>
        /// Formaly a static constructor.
        /// Creates a Reference from a key, guessing Reference.Type.
        /// </summary>
        /// <param name="k">Given single Key</param>
        /// <returns>Reference with guessed type</returns>
        public static Reference CreateFromKey(Key k)
        {
            var res = new Reference(ReferenceTypes.GlobalReference, new List<Key> { k });   
            res.Type = res.GuessType();
            return res;
        }

        /// <summary>
        /// Formaly a static constructor.
        /// Creates a Reference from a list of keys, guessing Reference.Type.
        /// </summary>
        /// <param name="lk"></param>
        /// <returns></returns>
        public static Reference CreateNew(List<Key> lk)
        {
            var res = new Reference(ReferenceTypes.GlobalReference, new List<Key>());
            if (lk == null)
                return res;
            res.Keys.AddRange(lk.Copy());
            res.Type = res.GuessType();
            return res;
        }


        //This is alternative for operator overloding method +, as operator overloading cannot be done in extension classes
        public static Reference Add(this Reference a, Reference b)
        {
            a.Keys?.AddRange(b?.Keys);
            return a;
        }

        public static bool IsEmpty(this Reference reference)
        {
            if (reference == null || reference.Keys == null || reference.Keys.Count < 1)
            {
                return true;
            }

            return false;
        } 

        #endregion

        public static bool Matches (this Reference reference, KeyTypes keyType, string id, MatchMode matchMode = MatchMode.Strict)
        {
            if(reference.IsEmpty())
            {
                return false;
            }

            if(reference.Keys.Count == 1)
            {
                var key = reference.Keys[0];
                return key.Matches(new Key(keyType, id), matchMode);
            }

            return false;
        }

        public static bool Matches(this Reference reference, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (reference.Keys.Count == 1) // As per old implementation
            {
                if (reference.Keys[0].Value == id)
                    return true;
            }

            return false;
        }

        public static bool Matches(this Reference reference, Reference otherReference, MatchMode matchMode = MatchMode.Strict)
        {
            if (reference.Keys == null || reference.Keys.Count == 0 || otherReference?.Keys == null || otherReference.Keys.Count == 0)
            {
                return false;
            }

            bool match = true;
            for (int i = 0; i < reference.Keys.Count; i++)
            {
                match = match && reference.Keys[i].Matches(otherReference.Keys[i], matchMode);
            }

            return match;
        }

        public static bool MatchesExactlyOneKey(this Reference reference, Key key, MatchMode matchMode = MatchMode.Strict)
        {
            if(key == null || reference.Keys == null || reference.Keys.Count != 1)
            {
                return false;
            }

            var referenceKey = reference.Keys[0];
            return referenceKey.Matches(key, matchMode);
        }

        public static string GetAsIdentifier(this Reference reference)
        {
            if (reference.Type == ReferenceTypes.GlobalReference) // Applying only to Global Reference, based on older implementation, TODO:Make it Generic
            {
                if (reference.Keys == null || reference.Keys.Count < 1)
                {
                    return null;
                }

                return reference.Keys[0].Value;
            }

            return null;
        }

        public static string MostSignificantInfo(this Reference reference)
        {
            if (reference.Keys.Count < 1)
            {
                return "-";
            }

            var i = reference.Keys.Count - 1;
            var output = reference.Keys[i].Value;
            if (reference.Keys[i].Type == KeyTypes.FragmentReference && i > 0)
                output += reference.Keys[i - 1].Value;
            return output;
        }

        public static Key GetAsExactlyOneKey(this Reference reference)
        {
            if (reference.Keys == null || reference.Keys.Count != 1)
            {
                return null;
            }

            var key = reference.Keys[0];
            return new Key(key.Type, key.Value);
        }

        public static string ToStringExtended(this Reference reference, int format = 1, string delimiter = ",")
        {
            if(reference.Keys == null)
            {
                throw new NullValueException("Keys");
            }

            return reference.Keys.ToStringExtended(format, delimiter);
        }

        public static ReferenceTypes GuessType(this Reference reference)
        {
            var setAasRefs = Constants.AasReferables.Where((kt) => kt != null).Select(kt => kt.Value).ToArray();
            var allAasRefs = true;
            foreach (var k in reference.Keys)
                if (!k.MatchesSetOfTypes(setAasRefs))
                    allAasRefs = false;
            if (allAasRefs)
                return ReferenceTypes.ModelReference;
            else
                return ReferenceTypes.GlobalReference;
        }

        public static int Count(this Reference rf)
        {
            return rf.Keys.Count;
        }

        public static Key Last(this Reference rf)
        { 
            return rf.Keys.Last(); 
        }

    }

}
