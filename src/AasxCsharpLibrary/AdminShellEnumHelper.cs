/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxCompatibilityModels;
using Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdminShellNS
{
    /// <summary>
	/// Some string based flags to attach to the property
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class ExtensionFlagsAttribute : System.Attribute
    {
        public string Flags = "";

        public ExtensionFlagsAttribute(string flags)
        {
            Flags = flags;
        }
    }

    /// <summary>
    /// This attribute attaches some hint text.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
    public class ExtensionHintAttributeAttribute : System.Attribute
    {
        public string HintText = "";

        public ExtensionHintAttributeAttribute(string hintText)
        {
            if (hintText != null)
                HintText = hintText;
        }
    }

    /// <summary>
    /// This attribute attaches a max lines number to a property.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class ExtensionMultiLineAttribute : System.Attribute
    {
        public int? MaxLines = null;

        public ExtensionMultiLineAttribute(int maxLines = -1)
        {
            if (maxLines > 0)
                MaxLines = maxLines;
        }
    }

    /// <summary>
    /// This attribute attaches a display text.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class EnumMemberDisplayAttribute : System.Attribute
    {
        public string Text = "";

        public EnumMemberDisplayAttribute(string text)
        {
            if (text != null)
                Text = text;
        }
    }

    /// <summary>
	/// This class adds some helpers for handling enums.
	/// </summary>
	public static class AdminShellEnumHelper
    {
        public class EnumHelperMemberInfo
        {
            public string MemberValue = "";
            public string MemberDisplay = "";
            public object MemberInstance;
        }

        public static IEnumerable<EnumHelperMemberInfo> EnumHelperGetMemberInfo(Type underlyingType)
        {
            foreach (var enumMemberInfo in underlyingType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var enumInst = Activator.CreateInstance(underlyingType);

                var memVal = enumMemberInfo.GetCustomAttribute<EnumMemberAttribute>()?.Value;
                var memDisp = enumMemberInfo.GetCustomAttribute<EnumMemberDisplayAttribute>()?.Text;

                if (memVal?.HasContent() == true)
                {
                    var ev = enumMemberInfo.GetValue(enumInst);

                    yield return new EnumHelperMemberInfo()
                    {
                        MemberValue = memVal,
                        MemberDisplay = (memDisp?.HasContent() == true) ? memDisp : memVal,
                        MemberInstance = ev
                    };
                }
            }
        }

        public static T GetEnumMemberFromValueString<T>(string valStr, T valElse = default(T)) where T : struct
        {
            foreach (var em in EnumHelperGetMemberInfo(typeof(T)))
                if (em.MemberValue.Equals(valStr?.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    return (T)em.MemberInstance;
            return (T)valElse;
        }

        /// <summary>
        /// Use the EnumMemberDisplay attribute to resolve the text
        /// </summary>
        public static string GetEnumMemberDisplay(Type enumType, object enumValue)
        {
            // access
            if (enumType == null || enumValue == null)
                return "";
            
            // see: https://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value
            var memberInfos = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m =>
                    m.Name == enumValue.ToString()
                    && m.DeclaringType == enumType);
            var valueAttributes =
                enumValueMemberInfo.GetCustomAttributes(typeof(EnumMemberDisplayAttribute), false);
            return ((EnumMemberDisplayAttribute)valueAttributes[0]).Text;
        }
    }
}
