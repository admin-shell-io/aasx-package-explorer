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
        public delegate void DocumentEntityEvent(InterfaceEntity e);
        public event DocumentEntityEvent DoubleClick = null;

        public delegate Task MenuClickDelegate(InterfaceEntity e, string menuItemHeader, object tag);
        public event MenuClickDelegate MenuClick = null;

        public event DocumentEntityEvent DragStart = null;

        public string Title = "";
        public string Created = "";
        public string ContentType = "";
        public string ProtocolType = "";

        public FileInfo DigitalFile, PreviewFile;

#if USE_WPF
        public System.Windows.Controls.Viewbox ImgContainerWpf = null;
#endif
        public AnyUiImage ImgContainerAnyUi = null;
        public string ReferableHash = null;

        public List<Aas.ISubmodelElement> SourceElementsDocument = null;
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

        public InterfaceEntity(string Title, string Created, string ContentType, string ProtocolType)
        {
            this.Title = Title;
            this.Created = Created;
            this.ContentType = ContentType;
            this.ProtocolType = ProtocolType;
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


        private static void SearchForRelations(
            List<Aas.ISubmodelElement> smwc,
            InterfaceEntity.DocRelationType drt,
            Aas.IReference semId,
            InterfaceEntity intoDoc)
        {
            // access
            if (smwc == null || semId == null || intoDoc == null)
                return;

            foreach (var re in smwc.FindAllSemanticIdAs<Aas.ReferenceElement>(semId,
                MatchMode.Relaxed))
            {
                // access 
                if (re.Value == null || re.Value.Count() < 1)
                    continue;

                // be a bit picky
                if (re.Value.Last().Type != Aas.KeyTypes.Entity)
                    continue;

                // add
                intoDoc.Relations.Add(new Tuple<InterfaceEntity.DocRelationType, Aas.IReference>(
                    drt, re.Value));
            }
        }

        public static InterfaceEntity ParseSCInterfaceDescription(Aas.SubmodelElementCollection smcDoc,
                                                                  string referableHash)
        {
            var defs1 = AasxPredefinedConcepts.IDTAAid.Static;

            var title =
                "" +
                   smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(defs1.AID_title,
                MatchMode.Relaxed)?.Value;

            var Created =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_created, MatchMode.Relaxed)?
                    .Value;

            var ContentType =
                "" +
                smcDoc.Value.FindFirstSemanticIdAs<Aas.Property>(
                    defs1.AID_contentType, MatchMode.Relaxed)?
                    .Value;

            var ProtocolType = "HTTP";
            var ent = new InterfaceEntity(title, Created, ContentType, ProtocolType);
            ent.SourceElementsDocument = smcDoc.Value;
            ent.ReferableHash = referableHash;

            return ent;
        }

        public static ListOfInterfaceEntity ParseSubmodelAID(
            AdminShellPackageEnv thePackage,
            Aas.Submodel subModel,
            string selectedProtocol)
        {
            // set a new list
            
            var its = new ListOfInterfaceEntity();
            var defs1 = AasxPredefinedConcepts.IDTAAid.Static;

            if (thePackage == null || subModel == null )
                return its;

            // look for Documents
            if (subModel.SubmodelElements != null)
                foreach (var smcDoc in
                    subModel.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                        defs1.AID_Interface, MatchMode.Relaxed))
                {
                    // access
                    if (smcDoc == null || smcDoc.Value == null)
                        continue;
                     string referableHash = String.Format(
                        "{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());
                    its.Add(ParseSCInterfaceDescription(smcDoc, referableHash));
                }

            // ok
            return its;
        }
     }
}
