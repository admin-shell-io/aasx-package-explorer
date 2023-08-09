/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable EventNeverSubscribedTo.Global

namespace AasxIntegrationBase
{
    public class WpfViewModelBase : INotifyPropertyChanged
    {
        #region Properties, Events
        //========================

        public event Action<WpfViewModelBase> ViewModelChanged = null;

        protected void RaiseViewModelChanged()
        {
            if (ViewModelChanged != null)
                ViewModelChanged.Invoke(this);
        }

        #endregion

        #region Magic for DEP PROPERTIES
        //==============================

        /// <summary>
        /// Raises the PropertyChange event for the property specified
        /// </summary>
        /// <param name="propertyName">Property name to update. Is case-sensitive.</param>
        public virtual void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion
    }
}
