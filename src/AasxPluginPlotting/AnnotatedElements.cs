/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxIntegrationBase
{
    /// <summary>
    /// This class holds annotation information for SubmodelElements, which is supposed
    /// to be rendered in various UI situations.
    /// The arguments are supposed to be with a Qualifier "Annotation.Args"
    /// </summary>
    public class AnnotatedElemArgs
    {
        // ReSharper disable UnassignedField.Global

        /// <summary>
        /// Free text position argument for the annotation. Can be used to cluster
        /// annotations at different places onthe UI.
        /// </summary>
        public string pos;

        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string text;

        /// <summary>
        /// If true, (multi-language) text shall be taken from the description of the SubmodelElement,
        /// which features the Qualifier "Annotation.Args".
        /// </summary>
        public bool desc;

        /// <summary>
        /// Causes bold text.
        /// </summary>
        public bool bold;

        /// <summary>
        /// Top and bottom margins
        /// </summary>
        public double top = 1.0, bottom = 1.0;

        /// <summary>
        /// Mirrored description from the SubmodelElement,
        /// which features the Qualifier "Annotation.Args".
        /// </summary>
        [JsonIgnore]
        public List<Aas.ILangStringTextType> Description;

        // ReSharper enable UnassignedField.Global

        public static AnnotatedElemArgs Parse(string json)
        {
            if (!json.HasContent())
                return null;

            try
            {
                var res = Newtonsoft.Json.JsonConvert.DeserializeObject<AnnotatedElemArgs>(json);
                return res;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }
    }

    public class AnnotatedElements
    {
        private List<AnnotatedElemArgs> _args = new List<AnnotatedElemArgs>();

        public AnnotatedElements() { }

        public AnnotatedElements(Aas.IReferable root)
        {
            Parse(root);
        }

        public void Parse(Aas.IReferable root)
        {
            root?.RecurseOnReferables(null,
                includeThis: true,
                lambda: (o, parents, rf) =>
                {
                    foreach (var ext in rf.FindAllExtensionName("Annotation.Args"))
                    {
                        var a = AnnotatedElemArgs.Parse(ext?.Value);
                        if (a == null)
                            continue;

                        if (a.desc && rf is Aas.Submodel sm)
                            a.Description = sm.Description;

                        if (a.desc && rf is Aas.ISubmodelElement sme)
                            a.Description = sme.Description;

                        _args.Add(a);
                    }
                    return true;
                });
        }

        public IEnumerable<AnnotatedElemArgs> FindAnnotations(string hasPosition)
        {
            foreach (var a in _args)
                if (true == a?.pos.HasContent()

                    && a.pos.ToLower().Contains(hasPosition?.ToLower()))
                    yield return a;
        }

        public StackPanel RenderElements(IEnumerable<AnnotatedElemArgs> args, string defaultLang = null)
        {
            // result
            var res = new StackPanel();
            res.Orientation = Orientation.Vertical;
            if (args == null)
                return res;

            // simply fill
            foreach (var a in args)
            {
                if (a == null)
                    continue;
                var tb = new TextBox();
                tb.IsReadOnly = true;
                tb.IsReadOnlyCaretVisible = false;
                tb.TextWrapping = TextWrapping.WrapWithOverflow;
                tb.Text = "" + a.text;
                if (a.desc)
                {
                    var d = a.Description?.GetDefaultString(defaultLang);
                    if (d.HasContent())
                        tb.Text = d;
                }
                tb.BorderThickness = new Thickness(0.0);
                tb.Margin = new Thickness(0.0, a.top, 0.0, a.bottom);
                if (a.bold)
                    tb.FontWeight = FontWeights.Bold;
                res.Children.Add(tb);
            }

            // return
            return res;
        }
    }
}
