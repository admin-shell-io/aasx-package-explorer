using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase.AasForms
{
    public enum IFormListControlPropertyType { ShowContentItems, FormSmaller };

    public interface IFormListControl
    {
        /// <summary>
        /// Set the property, given by enum <c>IFormListControlPropertyType</c> to the
        /// value
        /// </summary>
        void SetProperty(IFormListControlPropertyType property, object value);

        /// <summary>
        /// Called from outside allows to show / collapse the detailed list view
        /// </summary>
        void ShowContentItems(bool completelyVisible);

        /// <summary>
        /// Tries to set the keyboard focus (cursor) to the first content field
        /// </summary>
        void ContentFocus();
    }
}
