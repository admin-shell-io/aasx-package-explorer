﻿/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using AasxIntegrationBase;
using AasxWpfControlLibrary.PackageCentral;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxWpfControlLibrary.AasxFileRepo
{
    /// <summary>
    /// This class is a business logic for maintaining a list of repositories. This could be "informal"
    /// repos load from a JSON file (AasxFileRepo) or repos given by AAS(X) repositories and registries.
    /// This business logic object is intended to be paired with a view / view model.
    /// </summary>
    public class AasxRepoList : ObservableCollection<AasxFileRepository>, IRepoFind
    {

        //
        // Adding element at the top
        //

        public AasxFileRepository AddAtTop(AasxFileRepository el)
        {
            if (el != null)
                this.Insert(0, el);
            return el;
        }

        //
        // IRepoFind interface
        //

        public AasxFileRepositoryItem FindByAssetId(string aid)
        {
            foreach (var fr in this)
            {
                var fi = fr?.FindByAssetId(aid);
                if (fi != null)
                    return fi;
            }
            return null;
        }

        public AasxFileRepositoryItem FindByAasId(string aid)
        {
            foreach (var fr in this)
            {
                var fi = fr?.FindByAasId(aid);
                if (fi != null)
                    return fi;
            }
            return null;
        }

        public IEnumerable<AasxFileRepositoryItem> EnumerateItems()
        {
            foreach (var fr in this)
                foreach (var fi in fr.EnumerateItems())
                    yield return fi;
        }

        public bool Contains(AasxFileRepositoryItem fi)
        {
            foreach (var fr in this)
                if (true == fr?.Contains(fi))
                    return true;
            return false;
        }

        //
        // Further finds
        //

        public AasxFileRepository FindRepository (AasxFileRepositoryItem fi)
        {
            foreach (var fr in this)
                if (true == fr?.Contains(fi))
                    return fr;
            return null;
        }
    }
}
