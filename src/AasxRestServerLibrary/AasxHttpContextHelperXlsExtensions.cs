using Newtonsoft.Json;
using System;
using System.Text;
using System.Linq;
using Grapevine.Shared;
using Grapevine.Interfaces.Server;
using System.IO;
using Grapevine.Server;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using System.Data;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;

namespace AasxRestServerLibrary
{
    public static class AasxHttpContextHelperXlsExtensions
    {

        /**
         * This method is able to evaluate an XML fragment and return a suitable serialization.
         */
        public static void EvalGetXLSFragment(this AasxHttpContextHelper helper, IHttpContext context, Stream xlsFileStream, string xlsFragment)
        {
            try
            {

                XLWorkbook workbook = LoadXlsDocument(xlsFileStream);

                object fragmentObject = FindFragmentObject(workbook, xlsFragment);

                var content = context.Request.QueryString.Get("content") ?? "normal";
                var extent = context.Request.QueryString.Get("extent") ?? "withoutBlobValue";

                JsonConverter converter = new XlsJsonConverter(content, extent);
                string json = JsonConvert.SerializeObject(fragmentObject, Newtonsoft.Json.Formatting.Indented, converter);

                SendJsonResponse(context, json);

                return;
            }
            catch (XlsFragmentEvaluationException e)
            {
                context.Response.SendResponse(
                    Grapevine.Shared.HttpStatusCode.NotFound,
                    e.Message);
                return;
            }
        }

        private static XLWorkbook LoadXlsDocument(Stream xlsFileStream)
        {
            try
            {
                return new XLWorkbook(xlsFileStream);
            }
            catch
            {
                throw new XlsFragmentEvaluationException($"Unable to load XLS file from stream.");
            }
        }

        private static object FindFragmentObject(XLWorkbook workbook, string xlsFragment)
        {
            String xlsExpression = xlsFragment.Trim('/');

            try
            {
                if (xlsExpression.Length == 0)
                {
                    // provided expression references the complete workbook
                    return workbook;
                }
                else if (xlsExpression.StartsWith("="))
                {
                    // provided expression is a formula
                    return workbook.Evaluate(xlsExpression.Substring(1));
                }
                else if (xlsExpression.Contains(':'))
                {
                    // provided expression is reference to a cell range
                    return workbook.Cells(xlsExpression);
                }
                else if (xlsExpression.Contains('!'))
                {
                    // provided expression is a reference to a single cell
                    return workbook.Cell(xlsExpression);
                }
                else
                {
                    // provided expression is a reference to a worksheet
                    return workbook.Worksheet(xlsExpression);
                }

            }
            catch
            {
                throw new XlsFragmentEvaluationException("An error occurred while evaluating Excel expression '" + xlsExpression + "'!");
            }

        }

        static void SendJsonResponse(IHttpContext context, string json)
        {
            var buffer = context.Request.ContentEncoding.GetBytes(json);
            var length = buffer.Length;

            context.Response.ContentType = ContentType.JSON;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = length;
            context.Response.SendResponse(buffer);
        }

    }

    /**
     * A JsonConverter that converts an XLWorkbook, XLWorksheet, XLCell, XLCells or a raw value to a JSON representation. The converter refers to the parameters 'content' and 
     * 'extent' as defined by "Details of the AAS, part 2".
     */
    class XlsJsonConverter : JsonConverter
    {
        string Content;
        string Extent;

        public XlsJsonConverter(string content = "normal", string extent = "withoutBlobValue")
        {
            this.Content = content;
            this.Extent = extent;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(XLWorkbook).IsAssignableFrom(objectType) || typeof(IXLWorksheet).IsAssignableFrom(objectType) ||
                typeof(IXLCell).IsAssignableFrom(objectType) || typeof(IXLCells).IsAssignableFrom(objectType) || typeof(string).IsAssignableFrom(objectType);
        }
        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            JContainer result;

            bool valueOnly = (Content == "value");

            if (value is XLWorkbook)
            {
                result = CompileJson(value as XLWorkbook, valueOnly);
            }
            else if (value is IXLWorksheet)
            {
                result = CompileJson(value as IXLWorksheet, valueOnly);
            }
            else if (value is IXLCells)
            {
                result = CompileJson(value as IXLCells, valueOnly);
            }
            else if (value is IXLCell)
            {
                result = CompileJson(value as IXLCell, valueOnly);
            }
            else if (value is string)
            {
                result = new JObject
                {
                    ["value"] = value as string
                };

                if (!valueOnly)
                {
                    result["type"] = "formula";
                }
            }
            else
            {
                throw new XlsFragmentEvaluationException("Unable to convert object to a suitbale type: " + value);
            }

            result.WriteTo(writer);
            return;

        }

        private JContainer CompileJson(XLWorkbook workbook, bool valueOnly)
        {
            JObject result = new JObject();
            if (!valueOnly)
            {
                result["type"] = "workbook";
                result["author"] = workbook.Author;
                result["worksheets"] = new JArray();
            }

            foreach (var worksheet in workbook.Worksheets)
            {
                var worksheetJson = CompileJson(worksheet, valueOnly);
                if (valueOnly)
                {
                    result[worksheet.Name] = worksheetJson;
                }
                else
                {
                    (result["worksheets"] as JArray).Add(worksheetJson);
                }
            }

            return result;
        }

        private JContainer CompileJson(IXLWorksheet worksheet, bool valueOnly)
        {
            JContainer cells = CompileJson(worksheet.Cells(), valueOnly);

            if (valueOnly)
            {
                return cells;
            }

            JObject result = new JObject
            {
                ["type"] = "worksheet",
                ["name"] = worksheet.Name
            };

            result["cells"] = cells;

            return result;
        }

        private JContainer CompileJson(IXLCells cells, bool valueOnly)
        {
            JContainer result;
            if (valueOnly)
            {
                result = new JObject();

                foreach (var cell in cells)
                {
                    result[cell.Address.ToString()] = cell.Value.ToString();
                }

            }
            else
            {
                result = new JArray();

                foreach (var cell in cells)
                {
                    result.Add(CompileJson(cell, valueOnly));
                }
            }

            return result;
        }

        private JObject CompileJson(IXLCell cell, bool valueOnly)
        {
            if (valueOnly)
            {
                return new JObject
                {
                    ["value"] = cell.Value.ToString()
                };
            }

            return new JObject
            {
                ["type"] = "cell",
                ["address"] = cell.Address.ToString(),
                ["formula"] = cell.FormulaA1.ToString(),
                ["value"] = cell.Value.ToString(),
            };
        }

    }


    /**
     * An exception that indicates that something went wrong while evaluating an XLS fragment.
     */
    public class XlsFragmentEvaluationException : ArgumentException
    {

        public XlsFragmentEvaluationException(string message) : base(message)
        {
        }

        public XlsFragmentEvaluationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}