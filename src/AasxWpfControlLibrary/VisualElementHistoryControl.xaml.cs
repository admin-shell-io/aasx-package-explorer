using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdminShellNS;

namespace AasxPackageExplorer
{
    public class VisualElementHistoryItem
    {
        public VisualElementGeneric VisualElement = null;
        public AdminShell.Identification ReferableAasId = null;
        public AdminShell.Reference ReferableReference = null;

        public VisualElementHistoryItem(VisualElementGeneric VisualElement,
            AdminShell.Identification ReferableAasId = null, AdminShell.Reference ReferableReference = null)
        {
            this.VisualElement = VisualElement;
            this.ReferableAasId = ReferableAasId;
            this.ReferableReference = ReferableReference;
        }
    }

    /// <summary>
    /// Interaktionslogik für VisualElementHistoryControl.xaml
    /// </summary>
    public partial class VisualElementHistoryControl : UserControl
    {
        // members

        public event EventHandler<VisualElementHistoryItem> VisualElementRequested = null;

        private List<VisualElementHistoryItem> history = new List<VisualElementHistoryItem>();

        // init

        public VisualElementHistoryControl()
        {
            InitializeComponent();

            // initial state: without content
            buttonBack.IsEnabled = false;
        }

        // functions

        /// <summary>
        /// Clears the history
        /// </summary>
        public void Clear()
        {
            // clear
            history.Clear();

            // not enabled
            buttonBack.IsEnabled = false;
        }

        public void Push(VisualElementGeneric ve)
        {
            // access
            if (ve == null)
                return;

            // for ve, try to find the AAS (in the parent hierarchy)
            var veAas = ve.FindAllParents((v) => { return v is VisualElementAdminShell; }).FirstOrDefault();

            // for ve, find the Referable to be ve or superordinate ..
            var veRef = ve.FindAllParents((v) =>
            {
                var derefdo = v?.GetDereferencedMainDataObject();
                return derefdo is AdminShell.Referable && derefdo is AdminShell.IGetReference;
            }).FirstOrDefault();

            // check, if ve can identify a Referable, to which a symbolic link can be done ..
            AdminShell.Identification aasid = null;
            AdminShell.Reference refref = null;

            if (veAas != null && veRef != null)
            {
                aasid = (veAas as VisualElementAdminShell)?.theAas?.identification;

                var derefdo = veRef.GetDereferencedMainDataObject();
                refref = (derefdo as AdminShell.IGetReference)?.GetReference();
            }

            // add, only if not already there
            if (history.Count < 1 || history[history.Count - 1].VisualElement != ve)
                history.Add(new VisualElementHistoryItem(ve, aasid, refref));

            // is enabled
            buttonBack.IsEnabled = true;
        }

        private void buttonBack_Click(object sender, RoutedEventArgs e)
        {
            // anything
            if (history == null || history.Count < 1)
                return;

            // pop last (as this the already displayed one)
            history.RemoveAt(history.Count - 1);

            // may be disable ..
            buttonBack.IsEnabled = history.Count > 0;

            // give back the one prior to it
            if (history.Count < 1)
                return;
            var ve = history[history.Count - 1];

            // trigger event
            this.VisualElementRequested?.Invoke(this, ve);
        }
    }
}
