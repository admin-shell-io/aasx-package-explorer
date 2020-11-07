using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AasxGlobalLogging;
using AdminShellNS;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    //
    // Modify Repo
    //

    public class ModifyRepo
    {
        // some types for LambdaAction
        public class LambdaAction { }
        public class LambdaActionNone : LambdaAction { }
        public class LambdaActionRedrawEntity : LambdaAction { }
        public class LambdaActionRedrawAllElements : LambdaAction
        {
            public object NextFocus = null;
            public bool? IsExpanded = null;
            public DispEditHighlight.HighlightFieldInfo HighlightField = null;
            public bool OnlyReFocus = false;

            public LambdaActionRedrawAllElements(
                object nextFocus, bool? isExpanded = true,
                DispEditHighlight.HighlightFieldInfo highlightField = null,
                bool onlyReFocus = false)
            {
                this.NextFocus = nextFocus;
                this.IsExpanded = isExpanded;
                this.HighlightField = highlightField;
                this.OnlyReFocus = onlyReFocus;
            }
        }
        public class LambdaActionContentsChanged : LambdaAction { }
        public class LambdaActionContentsTakeOver : LambdaAction { }
        public class LambdaActionNavigateTo : LambdaAction
        {
            public LambdaActionNavigateTo() { }
            public LambdaActionNavigateTo(AdminShell.Reference targetReference)
            {
                this.targetReference = targetReference;
            }
            public AdminShell.Reference targetReference;
        }

        // some flags for the main application
        public List<LambdaAction> WishForOutsideAction = new List<LambdaAction>();

        public class RepoItem
        {
            public Control control = null;
            public Func<object, LambdaAction> setValueLambda = null;
            public object originalValue = null;
            public LambdaAction takeOverLambda = null;
        }

        private Dictionary<Control, RepoItem> items = new Dictionary<Control, RepoItem>();

        public void AddWishForAction(LambdaAction la)
        {
            WishForOutsideAction.Add(la);
        }

        public Control RegisterControl(
            Control c, Func<object, LambdaAction> setValue, LambdaAction takeOverLambda = null)
        {
            // add item
            var it = new RepoItem();
            it.control = c;
            it.setValueLambda = setValue;
            it.takeOverLambda = takeOverLambda;
            items.Add(c, it);

            // put callbacks accordingly
            if (c is TextBox)
            {
                var tb = c as TextBox;
                it.originalValue = "" + tb.Text;
                tb.TextChanged += Tb_TextChanged;
                tb.KeyUp += Tb_KeyUp;
            }

            if (c is Button btn)
            {
                btn.Click += B_Click;
            }

            if (c is ComboBox cb)
            {
                it.originalValue = "" + cb.Text;
                cb.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                  new System.Windows.Controls.TextChangedEventHandler(Tb_TextChanged));
                if (!cb.IsEditable)
                    // we need this event
                    cb.SelectionChanged += Cb_SelectionChanged;
                if (cb.IsEditable)
                    // add this for comfort
                    cb.KeyUp += Tb_KeyUp;
            }

            if (c is CheckBox ch)
            {
                it.originalValue = ch.IsChecked;
                ch.Checked += Cb_Checked;
                ch.Unchecked += Cb_Checked;
            }

            if (c is MenuItem mi)
            {
                mi.Click += B_Click;
            }

            return c;
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && items.ContainsKey(sender as Control))
                {
                    var it = items[sender as Control];
                    if (it.control is ComboBox cb && it.setValueLambda != null)
                        it.setValueLambda((string)cb.SelectedItem);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());

                    if (it.takeOverLambda != null)
                        WishForOutsideAction.Add(it.takeOverLambda);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && items.ContainsKey(sender as Control))
                {
                    var it = items[sender as Control];
                    if (it.control is CheckBox cb && it.setValueLambda != null)
                        it.setValueLambda(cb.IsChecked == true);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());

                    if (it.takeOverLambda != null)
                        WishForOutsideAction.Add(it.takeOverLambda);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }

        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && items.ContainsKey(sender as Control))
                {
                    var it = items[sender as Control];
                    if (it.control is Button || it.control is MenuItem)
                    {
                        var action = it.setValueLambda(it.control);
                        if (action != null)
                            WishForOutsideAction.Add(action);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        private void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && items.ContainsKey(sender as Control))
                {
                    var it = items[sender as Control];
                    if (it.control is TextBox tb && it.setValueLambda != null)
                        it.setValueLambda(tb.Text);
                    if (it.control is ComboBox cb && it.setValueLambda != null)
                        it.setValueLambda(cb.Text);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsChanged());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        private void Tb_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    // send a take over
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());
                    // more?
                    if (sender is Control && items.ContainsKey(sender as Control))
                    {
                        var it = items[sender as Control];
                        if (it.takeOverLambda != null)
                            WishForOutsideAction.Add(it.takeOverLambda);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        public void Clear()
        {
            items.Clear();
        }

        public void CallUndoChanges()
        {
            try
            {
                foreach (var it in items.Values)
                {
                    if (it.control != null && it.originalValue != null)
                    {
                        if (it.control is TextBox tb)
                            tb.Text = it.originalValue as string;
                    }

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "While in user callback (modify repo lambda)");
            }
        }
    }
}
