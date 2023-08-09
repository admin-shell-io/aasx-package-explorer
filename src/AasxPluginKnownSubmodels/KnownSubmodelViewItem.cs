/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.ObjectModel;
using System.IO;

// ReSharper disable EmptyNamespace

namespace AasxPluginKnownSubmodels
{
#if USE_WPF
    public class KnownSubmodelViewItem
    {
        public string DisplayHeader { get; set; } = "";
        public string DisplayContent { get; set; } = "";
        public string DisplayFurtherUrl { get; set; } = "";

        public string ImagePath = "";

#if USE_WPF
        private BitmapImage _imageData = null;
        public BitmapImage DisplayImageData
        {
            get { TryLoadImageData(); return _imageData; }
            set { _imageData = value; }
        }
#endif

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
#endif

}
