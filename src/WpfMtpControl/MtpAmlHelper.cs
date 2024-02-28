/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AasxIntegrationBase;
using AdminShellNS;
using Aml.Engine.CAEX;
using WpfMtpControl.DataSources;

namespace WpfMtpControl
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MtpAmlHelper
    {
        public static bool CheckForRole(CAEXSequence<SupportedRoleClassType> seq, string refRoleClassPath)
        {
            if (seq == null)
                return false;
            foreach (var src in seq)
                if (src.RefRoleClassPath != null && src.RefRoleClassPath.Trim() != "")
                    if (src.RefRoleClassPath.Trim().ToLower() == refRoleClassPath.Trim().ToLower())
                        return true;
            return false;
        }

        public static bool CheckForRole(CAEXSequence<RoleRequirementsType> seq, string refBaseRoleClassPath)
        {
            if (seq == null)
                return false;
            foreach (var src in seq)
                if (src.RefBaseRoleClassPath != null && src.RefBaseRoleClassPath.Trim() != "")
                    if (src.RefBaseRoleClassPath.Trim().ToLower() == refBaseRoleClassPath.Trim().ToLower())
                        return true;
            return false;
        }

        public static bool CheckForRoleClassOrRoleRequirements(SystemUnitClassType ie, string classPath)
        {
            // HACK (MIHO, 2020-08-03): see equivalent function in AmlImport.cs; may be re-use
            if (ie is InternalElementType)
                if (CheckForRole((ie as InternalElementType).RoleRequirements, classPath))
                    return true;

            return
                CheckForRole(ie.SupportedRoleClass, classPath);
        }

        public static bool CheckAttributeFoRefSemantic(AttributeType a, string correspondingAttributePath)
        {
            if (a.RefSemantic != null)
                foreach (var rf in a.RefSemantic)
                    if (rf.CorrespondingAttributePath != null && rf.CorrespondingAttributePath.Trim() != ""
                        && rf.CorrespondingAttributePath.Trim().ToLower()
                           == correspondingAttributePath.Trim().ToLower())
                        // found!
                        return true;
            return false;
        }

        public static AttributeType FindAttributeByRefSemantic(
            AttributeSequence aseq, string correspondingAttributePath)
        {
            foreach (var a in aseq)
            {
                // check attribute itself
                if (CheckAttributeFoRefSemantic(a, correspondingAttributePath))
                    // found!
                    return a;

                // could be childs
                var x = FindAttributeByRefSemantic(a.Attribute, correspondingAttributePath);
                if (x != null)
                    return x;
            }
            return null;
        }

        public static string FindAttributeValueByRefSemantic(
            AttributeSequence aseq, string correspondingAttributePath)
        {
            var a = FindAttributeByRefSemantic(aseq, correspondingAttributePath);
            return a?.Value;
        }

        public static AttributeType FindAttributeByName(AttributeSequence aseq, string name)
        {
            if (aseq != null)
                foreach (var a in aseq)
                    if (a.Name.Trim() == name.Trim())
                        return a;
            return null;
        }

        public static string FindAttributeValueByName(AttributeSequence aseq, string name)
        {
            var a = FindAttributeByName(aseq, name);
            return a?.Value;
        }

        public static Nullable<int> FindAttributeValueByNameFromInt(AttributeSequence aseq, string name)
        {
            var astr = FindAttributeValueByName(aseq, name);
            if (astr == null)
                return (null);
            return Convert.ToInt32(astr);
        }

        public static Nullable<double> FindAttributeValueByNameFromDouble(AttributeSequence aseq, string name)
        {
            var astr = FindAttributeValueByName(aseq, name);
            if (astr == null)
                return (null);
            if (!double.TryParse(astr, NumberStyles.Any, CultureInfo.InvariantCulture, out var res))
                return null;
            return res;
        }

        public static List<Tuple<string, SystemUnitClassType>> FindAllMtpPictures(CAEXFileType aml)
        {
            // start
            var res = new List<Tuple<string, SystemUnitClassType>>();

            // assumption: all pictures are on the 1st level of a instance hierarchy ..
            foreach (var ih in aml.InstanceHierarchy)
                foreach (var ie in ih.InternalElement)
                    if (ie.RefBaseSystemUnitPath.Trim() == "MTPHMISUCLib/Picture")
                        res.Add(new Tuple<string, SystemUnitClassType>(ie.Name, ie));

            // ok
            return res;
        }

        public static Dictionary<string, SystemUnitClassType> FindAllDynamicInstances(CAEXFileType aml)
        {
            // start           
            var res = new Dictionary<string, SystemUnitClassType>(StringComparer.InvariantCultureIgnoreCase);
            if (aml == null)
                return res;

            // assumption: all instances are on a fixed level of a instance hierarchy ..
            foreach (var ih in aml.InstanceHierarchy)                   // e.g.: ModuleTypePackage
                foreach (var ie in ih.InternalElement)                  // e.g. Class = ModuleTypePackage
                    foreach (var ie2 in ie.InternalElement)             // e.g. CommunicationSet
                        foreach (var ie3 in ie2.InternalElement)        // e.g. InstanceList
                            if (ie3.RefBaseSystemUnitPath.Trim() == "MTPSUCLib/CommunicationSet/InstanceList")
                                foreach (var ie4 in ie3.InternalElement)     // now ALWAYS an dynamic instance
                                {
                                    var refID = MtpAmlHelper.FindAttributeValueByName(ie4.Attribute, "RefID");
                                    if (refID != null && refID.Length > 0)
                                        res.Add(refID, ie4);
                                }

            // ok
            return res;
        }

        public static void CreateDataSources(
            IMtpDataSourceFactoryOpcUa dataSourceFactory,
            MtpDataSourceOpcUaPreLoadInfo preLoadInfo,
            CAEXFileType aml)
        {
            // access
            if (dataSourceFactory == null || aml == null)
                return;

            // assumption: all instances are on a fixed level of a instance hierarchy ..
            foreach (var ih in aml.InstanceHierarchy)                               // e.g.: ModuleTypePackage
                foreach (var ie in ih.InternalElement)                              // e.g. Class = ModuleTypePackage
                    foreach (var ie2 in ie.InternalElement)                         // e.g. CommunicationSet
                        foreach (var ie3 in ie2.InternalElement)                    // e.g. InstanceList
                            if (ie3.RefBaseSystemUnitPath.Trim() == "MTPSUCLib/CommunicationSet/SourceList")
                                foreach (var server in ie3.InternalElement)     // now on server
                                {
                                    // check if server valid
                                    if (server.RefBaseSystemUnitPath.Trim() !=
                                        "MTPCommunicationSUCLib/ServerAssembly/OPCUAServer")
                                        continue;
                                    if (!server.Name.HasContent())
                                        continue;

                                    // get attributes
                                    var ep = FindAttributeValueByName(server.Attribute, "Endpoint");

                                    // mapping??
                                    if (preLoadInfo?.EndpointMapping != null)
                                        foreach (var epm in preLoadInfo.EndpointMapping)
                                            if (epm?.IsValid == true &&
                                                (server.Name.Trim() == epm.ForName?.Trim()
                                                 || server.ID.Trim() == epm.ForId?.Trim()))
                                            {
                                                ep = epm.NewEndpoint?.Trim();
                                            }

                                    // check endpoint
                                    if (!ep.HasContent())
                                        continue;

                                    // make server
                                    var serv = dataSourceFactory.CreateOrUseUaServer(ep);
                                    if (serv == null)
                                        continue;

                                    // go into items
                                    foreach (var item in server.ExternalInterface)
                                    {
                                        // check to item
                                        // TODO (MIHO, 2020-08-06): spec/example files seem not to be in a final state
                                        // check for the final role/class names to be used
                                        if (!item.RefBaseClassPath.Trim().Contains("OPCUAItem"))
                                            continue;

                                        // get attributes
                                        var id = FindAttributeValueByName(item.Attribute, "Identifier");
                                        var ns = FindAttributeValueByName(item.Attribute, "Namespace");
                                        var ac = FindAttributeValueByName(item.Attribute, "Access");

                                        // potential renaming?
                                        if (preLoadInfo?.IdentifierRenaming != null)
                                            foreach (var ren in preLoadInfo.IdentifierRenaming)
                                                id = ren.DoReplacement(id);

                                        if (preLoadInfo?.NamespaceRenaming != null)
                                            foreach (var ren in preLoadInfo.NamespaceRenaming)
                                                ns = ren.DoReplacement(ns);

                                        // create
                                        // ReSharper disable once UnusedVariable
                                        var it = dataSourceFactory.CreateOrUseItem(serv, id, ns, ac, item.ID);
                                    }
                                }

        }

        public static double[] ConvertStringToDoubleArray(string input, char[] separator)
        {
            var pieces = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (pieces == null)
                // ReSharper disable once HeuristicUnreachableCode
                return null;
            var res = new List<double>();
            foreach (var p in pieces)
            {
                if (!double.TryParse(p.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    // fail immediately
                    return null;
                res.Add(num);
            }
            return res.ToArray();
        }

        /// <summary>
        /// Edges delimited by ';', coordinates by ','. Example: "1,2;3,4;5,6".
        /// </summary>
        public static PointCollection PointCollectionFromString(string edgepath)
        {
            var edges = edgepath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (edges == null || edges.Length < 2)
                return null;

            var pc = new PointCollection();
            foreach (var e in edges)
            {
                var coord = ConvertStringToDoubleArray(e, new[] { ',' });
                if (coord != null && coord.Length == 2)
                    pc.Add(new Point(coord[0], coord[1]));
            }
            return pc;
        }

    }
}
