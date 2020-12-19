/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdminShellNS;

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
            public AasCntlFrameworkElement aasCntl = null;
            public FrameworkElement fwElem = null;
            public Func<object, LambdaAction> setValueLambda = null;
            public object originalValue = null;
            public LambdaAction takeOverLambda = null;
        }

        private List<RepoItem> items = new List<RepoItem>();

        private Dictionary<FrameworkElement, RepoItem> fwElemToItem = 
                    new Dictionary<FrameworkElement, RepoItem>();
        private Dictionary<AasCntlFrameworkElement, RepoItem> aasCntlToItem = 
                    new Dictionary<AasCntlFrameworkElement, RepoItem>();

        public void AddWishForAction(LambdaAction la)
        {
            WishForOutsideAction.Add(la);
        }

        /// <summary>
        /// This function attaches lambdas accordingly to a give user control.
        /// It is to be used, when an abstract AasCntl... is being created and the according WPF element
        /// will be activated later.
        /// </summary>
        /// <param name="fe">User control</param>
        /// <param name="setValue">Lambda called, whenever the value is changed</param>
        /// <param name="takeOverLambda">Lamnda called at the end of a modification</param>
        /// <returns>Passes thru the user control</returns>
        public AasCntlFrameworkElement RegisterControl(
            AasCntlFrameworkElement cntl, Func<object, LambdaAction> setValue, LambdaAction takeOverLambda = null)
        {
            // store for LATER activation
            var it = new RepoItem();
            it.aasCntl = cntl;
            it.setValueLambda = setValue;
            it.takeOverLambda = takeOverLambda;

            items.Add(it);
            aasCntlToItem.Add(cntl, it);

            // pass through
            return (cntl);
        }

        /// <summary>
        /// This function attaches lambdas accordingly to a give user control.
        /// It is to be used, when a WPF dialogue is build up directly.
        /// </summary>
        /// <param name="fe">User control</param>
        /// <param name="setValue">Lambda called, whenever the value is changed</param>
        /// <param name="takeOverLambda">Lamnda called at the end of a modification</param>
        /// <returns>Passes thru the user control</returns>
        public FrameworkElement RegisterControl(
            FrameworkElement fe, Func<object, LambdaAction> setValue, LambdaAction takeOverLambda = null)
        {
            // add item
            var it = new RepoItem();
            it.fwElem = fe;
            it.setValueLambda = setValue;
            it.takeOverLambda = takeOverLambda;

            items.Add(it);
            fwElemToItem.Add(fe, it);

            // activate directly
            ActivateFwElem(it);

            // pass through
            return fe;
        }

        public void ActivateAasCntl(AasCntlFrameworkElement aasCntl, FrameworkElement fe)
        {
            // access and book keeping
            if (aasCntl == null || fe == null || !aasCntlToItem.ContainsKey(aasCntl))
                return;
            var it = aasCntlToItem[aasCntl];
            it.fwElem = fe;

            // add to framework elems index
            if (!fwElemToItem.ContainsKey(fe))
                fwElemToItem.Add(fe, it);

            if (aasCntl is AasCntlComboBox cb && it.takeOverLambda != null)
                ;

            // now, activate
            ActivateFwElem(it);
        }

        private void ActivateFwElem(RepoItem it)
        {
            // access
            var fe = it?.fwElem;
            if (fe == null)
                return;

            // put callbacks accordingly
            if (fe is TextBox)
            {
                var tb = fe as TextBox;
                it.originalValue = "" + tb.Text;
                tb.TextChanged += Tb_TextChanged;
                tb.KeyUp += Tb_KeyUp;
            }

            if (fe is Button btn)
            {
                btn.Click += B_Click;
            }

            if (fe is ComboBox cb)
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

            if (fe is CheckBox ch)
            {
                it.originalValue = ch.IsChecked;
                ch.Checked += Cb_Checked;
                ch.Unchecked += Cb_Checked;
            }

            if (fe is MenuItem mi)
            {
                mi.Click += B_Click;
            }

            if (fe is Border brd && brd.Tag is string tag && tag == "DropBox")
            {
                brd.AllowDrop = true;
                brd.DragEnter += (object sender2, DragEventArgs e2) =>
                {
                    e2.Effects = DragDropEffects.Copy;
                };
                brd.PreviewDragOver += (object sender3, DragEventArgs e3) =>
                {
                    e3.Handled = true;
                };
                brd.Drop += (object sender4, DragEventArgs e4) =>
                {
                    if (e4.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        // Note that you can have more than one file.
                        string[] files = (string[])e4.Data.GetData(DataFormats.FileDrop);

                        // Assuming you have one file that you care about, pass it off to whatever
                        // handling code you have defined.
                        if (files != null && files.Length > 0
                            && sender4 is FrameworkElement fe2 && fwElemToItem.ContainsKey(fe2))
                        {
                            var it2 = fwElemToItem[fe2];
                            if (it2.fwElem is Border brd2 && it2.setValueLambda != null)
                            {
                                // update UI
                                if (brd2.Child is TextBlock tb2)
                                    tb2.Text = "" + files[0];

                                // value changed
                                it2.setValueLambda(files[0]);

                                // contents changed
                                WishForOutsideAction.Add(new LambdaActionContentsChanged());
                            }

                        }
                    }

                    e4.Handled = true;
                };
            }
        }

        private void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && fwElemToItem.ContainsKey(sender as Control))
                {
                    var it = fwElemToItem[sender as Control];
                    if (it.fwElem is ComboBox cb && it.setValueLambda != null)
                        it.setValueLambda((string)cb.SelectedItem);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());

                    if (it.takeOverLambda != null && false)
                        WishForOutsideAction.Add(it.takeOverLambda);
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        private void Cb_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && fwElemToItem.ContainsKey(sender as Control))
                {
                    var it = fwElemToItem[sender as Control];
                    if (it.fwElem is CheckBox cb && it.setValueLambda != null)
                        it.setValueLambda(cb.IsChecked == true);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());

                    if (it.takeOverLambda != null)
                        WishForOutsideAction.Add(it.takeOverLambda);
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
            }

        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && fwElemToItem.ContainsKey(sender as Control))
                {
                    var it = fwElemToItem[sender as Control];
                    if (it.fwElem is Button || it.fwElem is MenuItem)
                    {
                        var action = it.setValueLambda(it.fwElem);
                        if (action != null)
                            WishForOutsideAction.Add(action);
                    }
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        private void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // sender shall be in dictionary
                if (sender is Control && fwElemToItem.ContainsKey(sender as Control))
                {
                    var it = fwElemToItem[sender as Control];
                    if (it.fwElem is TextBox tb && it.setValueLambda != null)
                        it.setValueLambda(tb.Text);
                    if (it.fwElem is ComboBox cb && it.setValueLambda != null)
                        it.setValueLambda(cb.Text);

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsChanged());
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
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
                    if (sender is Control && fwElemToItem.ContainsKey(sender as Control))
                    {
                        var it = fwElemToItem[sender as Control];
                        if (it.takeOverLambda != null)
                            WishForOutsideAction.Add(it.takeOverLambda);
                    }
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
            }
        }

        public void Clear()
        {
            items.Clear();
            fwElemToItem.Clear();
            aasCntlToItem.Clear();
        }

        public void CallUndoChanges()
        {
            try
            {
                foreach (var it in fwElemToItem.Values)
                {
                    if (it.fwElem != null && it.originalValue != null)
                    {
                        if (it.fwElem is TextBox tb)
                            tb.Text = it.originalValue as string;
                    }

                    // contents changed
                    WishForOutsideAction.Add(new LambdaActionContentsTakeOver());
                }
            }
            catch (Exception ex)
            {
                AasxPackageExplorer.Log.Singleton.Error(ex, "While in user callback (modify repo lambda)");
            }
        }
    }
}
