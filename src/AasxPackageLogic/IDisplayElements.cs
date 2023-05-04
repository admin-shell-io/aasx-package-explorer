/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using AasxPackageLogic;

namespace AasxPackageExplorer
{
    /// <summary>
    /// UI abstracting interface to displayed AAS tree elements (middle section)
    /// </summary>
    public interface IDisplayElements
    {
        /// <summary>
        /// If it boils down to one item, which is the selected item.
        /// </summary>
        VisualElementGeneric GetSelectedItem();

        /// <summary>
        /// Clears only selection.
        /// </summary>
		void ClearSelection();

        /// <summary>
        /// Tries to expand all items, which aren't currently yet, e.g. because of lazy loading.
        /// Is found to be a valid pre-requisite in case of lazy loading for 
        /// <c>SearchVisualElementOnMainDataObject</c>.
        /// Potentially a expensive operation.
        /// </summary>
        void ExpandAllItems();

        /// <summary>
        /// Perform a general re-display of the elements.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Within the visual element, search a specific one associated with the main data object.
        /// Via <c>sri</c>, search also plugins and more.
        /// </summary>
        VisualElementGeneric SearchVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            ListOfVisualElement.SupplementaryReferenceInformation sri = null);

        /// <summary>
        /// For a given visual element, try to select it for the user view.
        /// </summary>
        bool TrySelectVisualElement(VisualElementGeneric ve, bool? wishExpanded);

        /// <summary>
        /// Carefully checks and tries to select a tree item which is identified
        /// by the main data object (e.g. an AAS, SME, ..)
        /// </summary>
        bool TrySelectMainDataObject(object dataObject, bool? wishExpanded);

    }

}