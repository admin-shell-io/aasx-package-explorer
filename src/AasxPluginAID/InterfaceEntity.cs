/*
Copyright (c) 2024 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Harish Kumar Pakala

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using AasxIntegrationBase;
using AasxIntegrationBaseGdi;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Threading.Tasks;
using System.Net.Mime;
using System.Net.Sockets;

namespace AasxPluginAID
{
    public class InterfaceEntity
    {
        public delegate void InterfaceEntityEvent(InterfaceEntity e);
        public event InterfaceEntityEvent DoubleClick = null;

        public delegate Task MenuClickDelegate(InterfaceEntity e, string menuItemHeader, object tag);
        public event MenuClickDelegate MenuClick = null;

        public event InterfaceEntityEvent DragStart = null;

        public string Title = "";
        public string Created = "";
        public string ContentType = "";
        public string ProtocolType = "";
        public string InterfaceName = "";

#if USE_WPF
        public System.Windows.Controls.Viewbox ImgContainerWpf = null;
#endif
        public AnyUiImage ImgContainerAnyUi = null;
        public string ReferableHash = null;

        public List<Aas.ISubmodelElement> SourceElementsInterface = null;
        public List<Aas.ISubmodelElement> SourceElementsDocumentVersion = null;

        public string ImageReadyToBeLoaded = null; // adding Image to ImgContainer needs to be done by the GUI thread!!
        public string[] DeleteFilesAfterLoading = null;

        public enum DocRelationType { DocumentedEntity, RefersTo, BasedOn, Affecting, TranslationOf };
        public List<Tuple<DocRelationType, Aas.IReference>> Relations =
            new List<Tuple<DocRelationType, Aas.IReference>>();

        /// <summary>
        /// The parsing might add a dedicated, version-specific action to add.
        /// </summary>        
        public delegate bool AddPreviewFileDelegate(InterfaceEntity e, string path, string contentType);

        public AddPreviewFileDelegate AddPreviewFile;

        public class FileInfo
        {
            public string Path = "";
            public string MimeType = "";

            public FileInfo() { }

            public FileInfo(Aas.File file)
            {
                Path = file?.Value;
                MimeType = file?.ContentType;
            }
        }

        public InterfaceEntity() { }

        public InterfaceEntity(string Title, string Created, string ContentType, string ProtocolType,
            string interfaceName)
        {
            this.Title = Title;
            this.Created = Created;
            this.ContentType = ContentType;
            this.ProtocolType = ProtocolType;
            InterfaceName = interfaceName;
        }

        public void RaiseDoubleClick()
        {
            if (DoubleClick != null)
                DoubleClick(this);
        }

        public async Task RaiseMenuClick(string menuItemHeader, object tag)
        {
            await MenuClick?.Invoke(this, menuItemHeader, tag);
        }

        public void RaiseDragStart()
        {
            DragStart?.Invoke(this);
        }

        /// <summary>
        /// This function needs to be called as part of tick-Thread in STA / UI thread
        /// </summary>
        public AnyUiBitmapInfo LoadImageFromPath(string fn)
        {
            // be a bit suspicous ..
            if (!System.IO.File.Exists(fn))
                return null;

            // convert here, as the tick-Thread in STA / UI thread
            try
            {
#if USE_WPF
                var bi = new BitmapImage(new Uri(fn, UriKind.RelativeOrAbsolute));

                if (ImgContainerWpf != null)
                {
                    var img = new Image();
                    img.Source = bi;
                    ImgContainerWpf.Child = img;
                }

                if (ImgContainerAnyUi != null)
                {
                    ImgContainerAnyUi.BitmapInfo = AnyUiHelper.CreateAnyUiBitmapInfo(bi);
                }
                return bi;
#else
                ImgContainerAnyUi.BitmapInfo = AnyUiGdiHelper.CreateAnyUiBitmapInfo(fn);
#endif
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
            return null;
        }
    }

    public class ListOfInterfaceEntity : List<InterfaceEntity>
    {
        public static AasxPredefinedConcepts.IDTAAid idtaDef = AasxPredefinedConcepts.IDTAAid.Static;
        
        public static InterfaceEntity ParseSCInterfaceDescription(Aas.SubmodelElementCollection smcInterface,
                                                                  string referableHash)
        {
            string title = "", created = "", contentype = "", protocolType = "";

            foreach (var elem in smcInterface.Value)
            {
                if (elem.IdShort == "title")
                {
                    title = (elem as Aas.Property).Value.ToString();
                }
                else if (elem.IdShort == "created")
                {
                    created = (elem as Aas.Property).Value.ToString();
                }
                else if (elem.IdShort == "InteractionMetadata")
                {
                    Aas.SubmodelElementCollection InterfaceMetaData = (elem as Aas.SubmodelElementCollection);
                    foreach (var imdElem in InterfaceMetaData.Value)
                    {
                        if (imdElem.IdShort == "properties")
                        {
                            Aas.SubmodelElementCollection properties = (imdElem as Aas.SubmodelElementCollection);
                            foreach (var property in properties.Value)
                            {
                                Aas.SubmodelElementCollection propertySMC = (property as Aas.SubmodelElementCollection);
                                foreach (var propertyElem in propertySMC.Value)
                                {
                                    if (propertyElem.IdShort == "forms")
                                    {
                                        Aas.SubmodelElementCollection formsSMC = (propertyElem as Aas.SubmodelElementCollection);
                                        foreach(var formElem in formsSMC.Value)
                                        {
                                            if (formElem.IdShort == "htv_methodName")
                                            {
                                                protocolType = "HTTP";
                                                break;
                                            }
                                            else if (idtaDef.mqttFormElemList.Contains(formElem.IdShort))
                                            {
                                                protocolType = "MQTT";
                                                break;
                                            }
                                            else if (idtaDef.modvFormElemList.Contains(formElem.IdShort))
                                            {
                                                protocolType = "MODBUS";
                                                break;
                                            }
                                        }
                                        foreach (var formElem in formsSMC.Value)
                                        {
                                            if (formElem.IdShort == "contentType")
                                            {
                                                contentype = (formElem as Aas.Property).Value.ToString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var InterfaceName = smcInterface.IdShort;
            var ent = new InterfaceEntity(title, created, contentype, protocolType, InterfaceName);
            ent.SourceElementsInterface = smcInterface.Value;
            ent.ReferableHash = referableHash;

            return ent;
        }

        public static ListOfInterfaceEntity ParseSubmodelAID(AdminShellPackageEnv thePackage,
                                                             Aas.Submodel subModel)
        {
            var interfaceEntities = new ListOfInterfaceEntity();
            var defs1 = AasxPredefinedConcepts.IDTAAid.Static;

            if (thePackage == null || subModel == null)
                return interfaceEntities;

            // look for Interfaces
            if (subModel.SubmodelElements != null)
                foreach (var smcInterface in
                    subModel.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        defs1.AID_Interface, MatchMode.Relaxed))
                {
                    if (smcInterface == null || smcInterface.Value == null)
                        continue;
                    string referableHash = String.Format(
                       "{0:X14} {1:X14}", thePackage.GetHashCode(), smcInterface.GetHashCode());
                    interfaceEntities.Add(ParseSCInterfaceDescription(smcInterface, referableHash));
                }
            return interfaceEntities;
        }
    }
}