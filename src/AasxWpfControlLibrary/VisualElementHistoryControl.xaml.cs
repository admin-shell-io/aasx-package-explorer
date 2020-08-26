using AdminShellNS;
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

namespace AasxPackageExplorer
{
    public class VisualElementHistoryItem
    {
        public VisualElementGeneric VisualElement = null;
        public string ReferableFilename = null;
        public string ReferableReference = null;

        public VisualElementHistoryItem(VisualElementGeneric VisualElement, 
            string ReferableFilename = null, string ReferableReference = null)
        {
            this.VisualElement = VisualElement;
            this.ReferableFilename = ReferableFilename;
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

        public void Push(VisualElementGeneric ve, AdminShellPackageEnv[] packages = null)
        {
            // check, if ve identifies a Referable, to which a symbolic link can be done ..
            string fn = null;
            string refstr = null;
            var mdo = ve?.GetDereferencedMainDataObject();
            if (packages?.Length > 0 && mdo != null && mdo is AdminShell.Referable && mdo is AdminShell.IGetReference)
            {
                // TODO (MIHO, 2020-08-26): this is not elegant; access to the package could be by ve itself!

                // in which package is the main data object
                AdminShellPackageEnv pFound = null;
                foreach (var p in packages)
                    if (p?.AasEnv != null)
                        foreach (var foundRf in p.AasEnv.FindAllReferable((rf) => { return rf == mdo; }))
                        {
                            // found something!
                            pFound = p;
                            break;
                        }

                if (pFound != null)
                {
                    fn = pFound.Filename;
                    refstr = (mdo as AdminShell.IGetReference).GetReference()?.ToString();
                }
            }

            // add, only if not already there
            if (history.Count < 1 || history[history.Count - 1].VisualElement != ve)
                history.Add(new VisualElementHistoryItem(ve, fn, refstr));

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
