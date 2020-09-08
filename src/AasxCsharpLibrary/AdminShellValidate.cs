using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AdminShellNS
{
    public enum AasValidationSeverity
    {
        Hint, Warning, SpecViolation, SchemaViolation, Serialization
    }

    public enum AasValidationAction
    {
        No, ToBeDeleted
    }

    public class AasValidationRecord
    {
        public AasValidationSeverity Severity = AasValidationSeverity.Hint;
        public AdminShell.Referable Source = null;
        public string Message = "";

        public Action Fix = null;

        public AasValidationRecord(AasValidationSeverity Severity, AdminShell.Referable Source,
            string Message, Action Fix = null)
        {
            this.Severity = Severity;
            this.Source = Source;
            this.Message = Message;
            this.Fix = Fix;
        }

        public override string ToString()
        {
            return $"[{Severity.ToString()}] in {"" + Source?.ToString()}: {"" + Message}";
        }

        public string DisplaySeverity { get { return "" + Severity.ToString(); } }
        public string DisplaySource
        {
            get
            {
                return "" + ((Source != null) ? Source.ToString() : "(whole content)");
            }
        }
        public string DisplayMessage { get { return "" + Message?.ToString(); } }
    }

    public class AasValidationRecordList : List<AasValidationRecord>
    {
    }

    public static class AasSchemaValidation
    {
        public enum SerializationFormat { XML, JSON }

        public static string[] GetSchemaResources(SerializationFormat fmt)
        {
            if (fmt == SerializationFormat.XML)
            {
                return new[]
                {
                    "AdminShellNS.Resources.schemaV201.AAS.xsd",
                    "AdminShellNS.Resources.schemaV201.AAS_ABAC.xsd",
                    "AdminShellNS.Resources.schemaV201.IEC61360.xsd"
                };
            }
            if (fmt == SerializationFormat.JSON)
            {
                return new[]
                {
                    "AdminShellNS.Resources.schemaV201.aas.json"
                };
            }
            return null;
        }

        public static int ValidateXML(AasValidationRecordList recs, Stream xmlContent)
        {
            // see: AasxCsharpLibrary.Tests/TestLoadSave.cs
            var newRecs = new AasValidationRecordList();

            // access
            if (recs == null || xmlContent == null)
                return -1;

            // Load the schema files
            var files = GetSchemaResources(SerializationFormat.XML);
            if (files == null)
                return -1;

            var xmlSchemaSet = new System.Xml.Schema.XmlSchemaSet();
            xmlSchemaSet.XmlResolver = new System.Xml.XmlUrlResolver();

            try
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                foreach (var schemaFn in files)
                {
                    using (Stream schemaStream = myAssembly.GetManifestResourceStream(schemaFn))
                    {
                        using (XmlReader schemaReader = XmlReader.Create(schemaStream))
                        {
                            xmlSchemaSet.Add(null, schemaReader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException("ValidateXML: Error accessing embedded resource schema files: " +
                    ex.Message);
            }

            // set up messages
            xmlSchemaSet.ValidationEventHandler += (object sender, System.Xml.Schema.ValidationEventArgs e) =>
            {
                newRecs.Add(new AasValidationRecord(AasValidationSeverity.Serialization, null,
                    "" + e?.Exception?.LineNumber + ", " + e?.Exception?.LinePosition + ": " + e?.Message));
            };

            // compile
            try
            {
                xmlSchemaSet.Compile();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ValidateXML: Error compiling schema files: " +
                    ex.Message);
            }

            if (newRecs.Count > 0)
            {
                var parts = new List<string> { $"Failed to compile the schema files:" };
                parts.AddRange(newRecs.Select<AasValidationRecord, string>((r) => r.Message));
                throw new InvalidOperationException(string.Join(Environment.NewLine, parts));
            }

            // load/ validate on same records
            var settings = new System.Xml.XmlReaderSettings();
            settings.ValidationType = System.Xml.ValidationType.Schema;
            settings.Schemas = xmlSchemaSet;

            settings.ValidationEventHandler +=
                (object sender, System.Xml.Schema.ValidationEventArgs e) =>
                {
                    newRecs.Add(new AasValidationRecord(AasValidationSeverity.Serialization, null,
                    "XML: " + e?.Exception?.LineNumber + ", " + e?.Exception?.LinePosition + ": " + e?.Message));
                };

            // use the xml stream
            using (var reader = System.Xml.XmlReader.Create(xmlContent, settings))
            {
                while (reader.Read())
                {
                    // Invoke callbacks
                };
            }

            // result
            recs.AddRange(newRecs);
            return newRecs.Count;
        }

        public static int ValidateJSONAlternative(AasValidationRecordList recs, Stream jsonContent)
        {
            // see: https://github.com/RicoSuter/NJsonSchema/wiki/JsonSchemaValidator
            var newRecs = new AasValidationRecordList();

            // access
            if (recs == null || jsonContent == null)
                return -1;

            // Load the schema files
            // right now: exactly ONE schema file
            var files = GetSchemaResources(SerializationFormat.JSON);
            if (files == null || files.Length != 1)
                return -1;

            NJsonSchema.JsonSchema schema = null;

            try
            {
                Assembly myAssembly = Assembly.GetExecutingAssembly();
                foreach (var schemaFn in files)
                {
                    using (Stream schemaStream = myAssembly.GetManifestResourceStream(schemaFn))
                    {
                        using (var streamReader = new StreamReader(schemaStream))
                        {
                            var allTxt = streamReader.ReadToEnd();
                            schema = NJsonSchema.JsonSchema.FromJsonAsync(allTxt).GetAwaiter().GetResult();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException("ValidateJSON: Error loading schema: " +
                    ex.Message);
            }

            if (schema == null)
            {
                throw new FileNotFoundException("ValidateJSON: Schema not found properly.");
            }

            // create validator
            var validator = new NJsonSchema.Validation.JsonSchemaValidator();

            // load the JSON content
            string jsonTxt = null;
            try
            {
                using (var streamReader = new StreamReader(jsonContent))
                {
                    jsonTxt = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ValidateJSON: Error loading JSON content: " +
                    ex.Message);
            }

            if (jsonTxt == null || jsonTxt == "")
                throw new InvalidOperationException("ValidateJSON: Error loading JSON content gave null.");

            // validate
            ICollection<NJsonSchema.Validation.ValidationError> errors;
            try
            {
                errors = validator.Validate(jsonTxt, schema);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ValidateJSON: Error when validating: " +
                    ex.Message);
            }

            // re-format messages
            if (errors != null)
                foreach (var ve in errors)
                {
                    var msg = ("" + ve.ToString());
                    msg = Regex.Replace(msg, @"\s+", " ");
                    newRecs.Add(new AasValidationRecord(AasValidationSeverity.Serialization, null,
                        $"JSON: {ve.LineNumber,5},{ve.LinePosition:3}: {msg}"));
                }

            // result
            recs.AddRange(newRecs);
            return newRecs.Count;
        }
    }
}
