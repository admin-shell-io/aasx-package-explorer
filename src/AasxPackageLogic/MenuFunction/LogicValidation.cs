/*
Copyright (c) 2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using Extensions;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Webpki.JsonCanonicalizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This is a record for validations based on business logic descriptions.
    /// </summary>
    public class LogicValidationRecBase
    {
        /// <summary>
        /// Translates the record to a (character-width) formatted text line
        /// </summary>
        public virtual string ToTextLine()
        {
            return "";
        }

        /// <summary>
        /// Translates the record to a table row with dedicated columns
        /// </summary>
        public virtual AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow();
        }
    }

    /// <summary>
    /// Textual comment when validating some business logic
    /// </summary>
    public class LogicValidationRecComment : LogicValidationRecBase
    {
        /// <summary>
        /// Any comment
        /// </summary>
        public string Text = "";

        public override string ToTextLine()
        {
            return "# " + Text;
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow("# " + Text).Set(wrap: false);
        }
    }

    /// <summary>
    /// Conclusive or summary statement when validating some business logic
    /// </summary>
    public class LogicValidationRecStatement : LogicValidationRecBase
    {
        /// <summary>
        /// Id (<6 chars) to correlate with report, form, visual
        /// </summary>
        public string Id = "";

        /// <summary>
        /// Textual flag of the outcome (<10 chars)
        /// </summary>
        public string OutcomeText = "";

        /// <summary>
        /// True, if outcome means a failing of the report
        /// </summary>
        public bool OutcomeFail = false;

        /// <summary>
        /// Verbal report of the statement
        /// </summary>
        public string Text = "";

        public override string ToTextLine()
        {
            return String.Format("{0,-6} {1,-10} {2}", Id, OutcomeText, Text);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow(Id, OutcomeText, Text).Set(bold: true);
        }
    }

    /// <summary>
    /// statement when validating some business logic, which is based on a specific
    /// AAS element
    /// </summary>
    public class LogicValidationRecElementDetail : LogicValidationRecStatement
    {
        public Aas.IReference Reference = null;

        public override string ToTextLine()
        {
            var refText = "-";
            if (Reference != null)
                refText = Reference.ToStringExtended(2);
            return String.Format("{0,-6} {1,-10} {2} Reference={3}", Id, OutcomeText, Text, refText);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            var refText = "-";
            if (Reference != null)
                refText = Reference.ToStringExtended(2);
            return new AasxPluginExportTableInterop.InteropRow(Id, OutcomeText, Text, refText)
                .Set(bold: true);
        }
    }

    /// <summary>
    /// Holds a list of logic validation records
    /// </summary>
    public class LogicValidationRecordList : List<LogicValidationRecBase>
    {
        public void AddComment(string text)
        {
            this.Add(new LogicValidationRecComment() { Text = text });
        }

        public void AddStatement(string id, string outcome, bool isFail, string text)
        {
            this.Add(new LogicValidationRecStatement() {
                Id = id,
                OutcomeText = outcome,
                OutcomeFail = isFail,
                Text = text 
            });
        }

        public void AddElemDetail(string id, string outcome, bool isFail, string text, Aas.IReference rf)
        {
            this.Add(new LogicValidationRecElementDetail()
            {
                Id = id,
                OutcomeText = outcome,
                OutcomeFail = isFail,
                Text = text,
                Reference = rf
            });
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            foreach (var rec in this)
                sb.AppendLine(rec.ToTextLine());
            return sb.ToString();
        }

        public AasxPluginExportTableInterop.InteropTable ToTable()
        {
            var res = new AasxPluginExportTableInterop.InteropTable();
            foreach (var rec in this)
                res.Rows.Add(rec.ToTableRow());
            return res;
        }

        public bool IsAnyFailForId(string id)
        {
            var allFail = false;
            foreach (var r in this)
                if (r is LogicValidationRecStatement state
                    && state.Id.Trim().Equals(id.Trim(), StringComparison.InvariantCultureIgnoreCase)
                    && state.OutcomeFail)
                    allFail = true;
            return allFail;
        }

        public void AddAnyFailStatement(string id, string text)
        {
            var anyFail = IsAnyFailForId(id);
            if (anyFail)
                AddStatement(id, "FAIL", true, text);
            else
                AddStatement(id, "PASS", false, text);
        }
    }
}
