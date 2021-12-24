using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AasxPluginKnownSubmodels
{
    public class KnownSubmodelViewItem
    {
        public string DisplayHeader { get; set; } = "";
        public string DisplayContent { get; set; } = "";
        public string DisplayFurtherUrl { get; set; } = "";

        public string ImagePath = "";

        private BitmapImage _imageData = null;
        public BitmapImage DisplayImageData
        {
            get { TryLoadImageData(); return _imageData; }
            set { _imageData = value; }
        }

        public KnownSubmodelViewItem() { }

        public KnownSubmodelViewItem(KnownSubmodelsOptionsRecord rec, string basePath = "")
        {
            if (rec != null)
            {
                DisplayHeader = rec.Header;
                DisplayContent = rec.Content;
                DisplayFurtherUrl = rec.FurtherUrl;
                ImagePath = Path.Combine(basePath, rec.ImageLink);
            }
        }

        public void TryLoadImageData()
        {
            // only once
            if (_imageData != null)
                return;

            try
            {
                _imageData = new BitmapImage(new Uri(ImagePath, UriKind.RelativeOrAbsolute));
            }
            catch {; }
        }
    }

    public class KnownSubmodelViewModel : ObservableCollection<KnownSubmodelViewItem>
    {

    }
}
