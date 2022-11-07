using AasCore.Aas3_0_RC02;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extenstions
{
    public static class ExtendKey
    {
        public static bool Matches(this Key key,
                KeyTypes type, string id, MatchMode matchMode = MatchMode.Strict)
        {
            if (matchMode == MatchMode.Strict)
                return key.Type == type && key.Value == id;

            if (matchMode == MatchMode.Relaxed)
                return (key.Type == type || key.Type == KeyTypes.GlobalReference || type == KeyTypes.GlobalReference)
                     && key.Value == id;

            if (matchMode == MatchMode.Identification)
                return key.Value == id;

            return false;
        }
        public static bool Matches(this Key key, Key otherKey)
        {
            if (otherKey == null)
            {
                return false;
            }

            if (key.Type == otherKey.Type && key.Value.Equals(otherKey.Value))
            {
                return true;
            }

            return false;
        }

        public static bool Matches(this Key key, Key otherKey, MatchMode matchMode = MatchMode.Strict)
        {
            if (matchMode == MatchMode.Strict)
                return key.Type == otherKey.Type && key.Value == otherKey.Value;

            if (matchMode == MatchMode.Relaxed)
                return key.Type == otherKey.Type || key.Type == KeyTypes.GlobalReference || otherKey.Type == KeyTypes.GlobalReference;

            if (matchMode == MatchMode.Identification)
                return key.Value == otherKey.Value;

            return false;
        }

       

        public static AasValidationAction Validate(this Key key, AasValidationRecordList results, IReferable container)
        {
            // access
            if (results == null || container == null)
                return AasValidationAction.No;

            var res = AasValidationAction.No;

            // check
            if (key == null)
            {
                // violation case
                results.Add(new AasValidationRecord(
                    AasValidationSeverity.SpecViolation, container,
                    "Key: is null",
                    () =>
                    {
                        res = AasValidationAction.ToBeDeleted;
                    }));
            }
            else
            {

                // check type
                var tf = AdminShellUtil.CheckIfInConstantStringArray(Enum.GetNames(typeof(KeyTypes)), Stringification.ToString(key.Type));
                if (tf == AdminShellUtil.ConstantFoundEnum.No)
                    // violation case
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, container,
                        "Key: type is not in allowed enumeration values",
                        () =>
                        {
                            key.Type = KeyTypes.GlobalReference;
                        }));
                if (tf == AdminShellUtil.ConstantFoundEnum.AnyCase)
                    // violation case
                    results.Add(new AasValidationRecord(
                        AasValidationSeverity.SchemaViolation, container,
                        "Key: type in wrong casing",
                        () =>
                        {
                            //NO IdType in V3
                            //key.idType = AdminShellUtil.CorrectCasingForConstantStringArray(
                            //    KeyElements, key.type);
                        }));
            }

            // may give result "to be deleted"
            return res;
        }

        

        public static string ToStringExtended(this Key key)
        {
            return $"[{key.Type}, {key.Value}]";
        }

        public static bool IsAbsolute(this Key key)
        {
            return key.Type == KeyTypes.GlobalReference || key.Type == KeyTypes.AssetAdministrationShell || key.Type == KeyTypes.Submodel;
        }



        #region KeyList

        public static bool IsEmpty(this List<Key> keys)
        {
            return keys.Count < 1;
        }

        public static bool Matches(this List<Key> keys, List<Key> other, MatchMode matchMode = MatchMode.Strict)
        {
            if (other == null || other.Count != keys.Count)
                return false;

            var same = true;
            for (int i = 0; i < keys.Count; i++)
                same = same && keys[i].Matches(other[i], matchMode);

            return same;
        }

        public static List<Key> ReplaceLastKey(this List<Key> keys,List<Key> newKeys)
        {
            var res = new List<Key>(keys);
            if (res.Count < 1 || newKeys == null || newKeys.Count < 1)
                return res;

            res.Remove(res.Last());
            res.AddRange(newKeys);
            return res;
        }

        public static bool StartsWith(this List<Key> keyList, List<Key> otherKeyList)
        {
            if (otherKeyList == null || otherKeyList.Count == 0)
                return false;

            // simply test element-wise
            for (int i = 0; i < otherKeyList.Count; i++)
            {
                // does head have more elements than this list?
                if (i >= keyList.Count)
                    return false;

                if (!otherKeyList[i].Matches(keyList[i]))
                    return false;
            }

            // ok!
            return true;
        }

        public static string ToStringExtended(this List<Key> keys, string delimiter = ",")
        {
            return string.Join(delimiter, keys.Select((k) => k.ToStringExtended()));
        }

        public static void Validate(this List<Key> keys, AasValidationRecordList results,
                IReferable container)
        {
            // access
            if (results == null || keys == null || container == null)
                return;

            // iterate thru
            var idx = 0;
            while (idx < keys.Count)
            {
                var act = keys[idx].Validate(results, container);
                if (act == AasValidationAction.ToBeDeleted)
                {
                    keys.RemoveAt(idx);
                    continue;
                }
                idx++;
            }
        }
        #endregion
    }
}
