using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Representation of a VDI2770 Document or so ..
    /// </summary>
    public class DocumentEntity
    {
        public delegate void DocumentEntityEvent(DocumentEntity e);
        public event DocumentEntityEvent DoubleClick = null;

        public delegate void MenuClickDelegate(DocumentEntity e, string menuItemHeader);
        public event MenuClickDelegate MenuClick = null;

        public string Title = "";
        public string Organization = "";
        public string FurtherInfo = "";
        public string[] CountryCodes;
        public string DigitalFile = "";
        public System.Windows.Controls.Viewbox ImgContainer = null;
        public string ReferableHash = null;

        public AdminShell.SubmodelElementWrapperCollection SourceElementsDocument = null;
        public AdminShell.SubmodelElementWrapperCollection SourceElementsDocumentVersion = null;

        public string ImageReadyToBeLoaded = null; // adding Image to ImgContainer needs to be done by the GUI thread!!    
        public string[] DeleteFilesAfterLoading = null;

        public DocumentEntity() { }

        public DocumentEntity(string Title, string Organization, string FurtherInfo, string[] LangCodes = null)
        {
            this.Title = Title;
            this.Organization = Organization;
            this.FurtherInfo = FurtherInfo;
            this.CountryCodes = LangCodes;
        }

        public void RaiseDoubleClick()
        {
            if (DoubleClick != null)
                DoubleClick(this);
        }

        public void RaiseMenuClick(string menuItemHeader)
        {
            if (MenuClick != null)
                MenuClick(this, menuItemHeader);
        }
    }
}
