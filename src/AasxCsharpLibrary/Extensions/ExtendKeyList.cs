using AasCore.Aas3_0_RC02;
using AasxCompatibilityModels;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Extensions;

namespace Extensions
{
    public static class ExtendKeyList
    {
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

        public static List<Key> ReplaceLastKey(this List<Key> keys, List<Key> newKeys)
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

        public static bool StartsWith(this List<Key> keyList, List<Key> head, bool emptyIsTrue = false,
                MatchMode matchMode = MatchMode.Relaxed)
        {
            // access
            if (head == null)
                return false;
            if (head.Count == 0)
                return emptyIsTrue;

            // simply test element-wise
            for (int i = 0; i < head.Count; i++)
            {
                // does head have more elements than this list?
                if (i >= keyList.Count)
                    return false;

                if (!head[i].Matches(keyList[i], matchMode))
                    return false;
            }

            // ok!
            return true;
        }

        public static string ToStringExtended(this List<Key> keys, int format = 1, string delimiter = ",")
        {
            return string.Join(delimiter, keys.Select((k) => k.ToStringExtended(format)));
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

        public static bool MatchesSetOfTypes(this List<Key> key, IEnumerable<KeyTypes> set)
        {
            var res = true;
            foreach (var kt in key)
                if (!key.MatchesSetOfTypes(set))
                    res = false;
            return res;
        }

        public static List<Key> Parse(string input)
        {
            // access
            if (input == null)
                return null;

            // split
            var parts = input.Split(',', ';');
            var kl = new List<Key>();

            foreach (var p in parts)
            {
                var k = ExtendKey.Parse(p);
                if (k != null)
                    kl.Add(k);
            }

            return kl;
        }

        /// <summary>
        /// Take only idShort, ignore all other key-types and create a '/'-separated list
        /// </summary>
        /// <returns>Empty string or list of idShorts</returns>
        public static string BuildIdShortPath(this List<Key> keyList,
            int startPos = 0, int count = int.MaxValue)
        {
            if (keyList == null || startPos >= keyList.Count)
                return "";
            int nr = 0;
            var res = "";
            for (int i = startPos; i < keyList.Count && nr < count; i++)
            {
                nr++;
                //// if (keyList[i].Type.Trim().ToLower() == Key.IdShort.Trim().ToLower())
                {
                    if (res != "")
                        res += "/";
                    res += keyList[i].Value;
                }
            }
            return res;
        }

        public static List<Key> SubList(this List<Key> keyList,
            int startPos, int count = int.MaxValue)
        {
            var res = new List<Key>();
            if (startPos >= keyList.Count())
                return res;
            int nr = 0;
            for (int i = startPos; i < keyList.Count() && nr < count; i++)
            {
                nr++;
                res.Add(keyList[i]);
            }
            return res;
        }
    }
}
