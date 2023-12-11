﻿using AasCore.Aas3_0_RC02;
using AasxCompatibilityModels;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Extensions
{
    public static class ExtendKey
    {
        public static Key CreateFrom(Reference r)
        {
            if (r == null || r.Count() != 1)
                return null;
            return r.Keys[0].Copy();
        }

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
                return (key.Type == otherKey.Type || key.Type == KeyTypes.GlobalReference || otherKey.Type == KeyTypes.GlobalReference)
                    && (key.Value == otherKey.Value);

            if (matchMode == MatchMode.Identification)
                return key.Value == otherKey.Value;

            return false;
        }

        public static bool MatchesSetOfTypes(this Key key, IEnumerable<KeyTypes> set)
        {
            foreach (var kt in set)
                if (key.Type == kt)
                    return true;
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

        

        public static string ToStringExtended(this Key key, int format = 1)
        {
            if (format == 2)
                return "" + key.Value;
            return $"[{key.Type}, {key.Value}]";
        }

        public static bool IsAbsolute(this Key key)
        {
            return key.Type == KeyTypes.GlobalReference || key.Type == KeyTypes.AssetAdministrationShell || key.Type == KeyTypes.Submodel;
        }

        public static Key Parse(string cell, KeyTypes typeIfNotSet = KeyTypes.GlobalReference,
                bool allowFmtAll = false, bool allowFmt0 = false,
                bool allowFmt1 = false, bool allowFmt2 = false)
        {
            // access and defaults?
            if (cell == null || cell.Trim().Length < 1)
                return null;

            // format == 1
            if (allowFmtAll || allowFmt1)
            {
                var m = Regex.Match(cell, @"\((\w+)\)( ?)(.*)$");
                if (m.Success)
                {
                    return new Key(
                            Stringification.KeyTypesFromString(m.Groups[1].ToString()) ?? KeyTypes.GlobalReference, 
                            m.Groups[3].ToString());
                }
            }

            // format == 2
            if (allowFmtAll || allowFmt2)
            {
                var m = Regex.Match(cell, @"( ?)(.*)$");
                if (m.Success)
                {
                    return new Key(
                            typeIfNotSet, m.Groups[2].ToString());
                }
            }

            // format == 0
            if (allowFmtAll || allowFmt0)
            {
                var m = Regex.Match(cell, @"\[(\w+),( ?)(.*)\]");
                if (m.Success)
                {
                    return new Key(
                            Stringification.KeyTypesFromString(m.Groups[1].ToString()) ?? KeyTypes.GlobalReference,
                            m.Groups[3].ToString());
                }
            }

            // no
            return null;
        }

        // -------------------------------------------------------------------------------------------------------------
        #region Handling with enums for KeyTypes

        // see: https://stackoverflow.com/questions/27372816/how-to-read-the-value-for-an-enummember-attribute
        //public static string? GetEnumMemberValue<T>(this T value)
        //    where T : Enum
        //{
        //    return typeof(T)
        //        .GetTypeInfo()
        //        .DeclaredMembers
        //        .SingleOrDefault(x => x.Name == value.ToString())
        //        ?.GetCustomAttribute<EnumMemberAttribute>(false)
        //        ?.Value;
        //}

        //public static KeyTypes? MapFrom(AasReferables input)
        //{
        //    var st = input.GetEnumMemberValue();
        //    var res = Stringification.KeyTypesFromString(st);
        //    return res;
        //}

        //public static List<KeyTypes> MapFrom(IEnumerable<AasReferables> input)
        //{
        //    List<KeyTypes> res = new();
        //    foreach (var i in input)
        //    {
        //        var x = MapFrom(i);
        //        if (x.HasValue)
        //            res.Add(x.Value);
        //    }
        //    return res;
        //}

        //public static List<KeyTypes> GetAllKeyTypesForAasReferables()
        //    => ExtendKey.MapFrom(Enum.GetValues(typeof(AasReferables)).OfType<AasReferables>());

        #endregion

    }
}
