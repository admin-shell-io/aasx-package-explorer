/*
Copyright (c) 2020 SICK AG <info@sick.de>

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AasxDictionaryImport.Model;
using ExcelDataReader;

namespace AasxDictionaryImport.Cdd
{
    /// <summary>
    /// A parser for IEC CDD exports.  An IEC CDD export is a set of XLS files storing the attributes of the CDD
    /// elements.  This parser reads the class and property tables, creates Class and Property elements (defined in
    /// Data.cs) and provides them with a dictionary storing their attributes.
    /// </summary>
    internal class Parser
    {
        /// <summary>
        /// Parses the IEC CDD export files and creates a new data context from the given data source.
        /// </summary>
        /// <param name="dataSource">The data source to read the export files from</param>
        /// <returns>The data context read from the given data source</returns>
        /// <exception cref="Model.ImportException">If an unexpected error occurs during the import</exception>
        /// <exception cref="MissingExportFilesException">If the data source does not contain all required export
        /// files</exception>
        /// <exception cref="MultipleExportFilesException">If the data source contains more than one set of export
        /// files</exception>
        public Context Parse(DataSource dataSource)
        {
            try
            {
                var files = FindFiles(dataSource.Path);
                return new Context(dataSource,
                    ParseFile<Class>(files, "class"),
                    ParseFile<Property>(files, "property"));
            }
            catch (System.IO.IOException e)
            {
                throw new Model.ImportException(
                    $"Could not parse data in directory '{dataSource.Path}'", e);
            }
        }

        /// <exception cref="MultipleExportFilesException"/>
        private Dictionary<string, string> FindFiles(string directory)
        {
            var fileRegex = new Regex(@"\\export_(.*)_TSTM-.*\.xls$");
            var dict = new Dictionary<string, string>();
            var files = System.IO.Directory.GetFiles(directory, "export_*.xls");
            foreach (var f in files)
            {
                var match = fileRegex.Match(f);
                if (match.Success)
                {
                    var type = match.Groups[1].Value.ToLowerInvariant();
                    if (dict.ContainsKey(type))
                    {
                        throw new MultipleExportFilesException(type);
                    }
                    dict.Add(match.Groups[1].Value.ToLowerInvariant(), f);
                }
            }
            return dict;
        }

        /// <exception cref="MissingExportFilesException"/>
        private List<T> ParseFile<T>(Dictionary<string, string> files, string name)
            where T : Element
        {
            if (!files.ContainsKey(name))
                throw new MissingExportFilesException(name);
            return ParseFile<T>(files[name]);
        }

        private List<T> ParseFile<T>(string filename) where T : Element
        {

            using var stream = File.Open(filename, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            if (reader.FieldCount == 0)
            {
                // no columns
                return new List<T>();
            }

            return ParseFile<T>(reader);
        }

        private List<T> ParseFile<T>(IExcelDataReader reader) where T : Element
        {
            var data = new List<T>();
            var columns = new List<string>();

            while (reader.Read())
            {
                // The row type is determined by the value of the first column:
                //    #PROPERTY_ID --> header row
                //    empty        --> data row
                //    other        --> comment

                var value = GetString(reader, 0);
                if (value == "#PROPERTY_ID")
                    columns = ParseHeaders(reader);
                else if (value.Length == 0)
                    data.Add(ParseElement<T>(reader, columns));
            }

            return data;
        }

        private List<string> ParseHeaders(IExcelDataReader reader)
        {
            var columns = new List<string>(reader.FieldCount - 1);
            for (int i = 1; i < reader.FieldCount; i++)
            {
                var column = GetString(reader, i);
                columns.Add(column);
            }
            return columns;
        }

        private T ParseElement<T>(IExcelDataReader reader, List<string> columns) where T : Element
        {
            var elementDict = new Dictionary<string, string>();
            for (int i = 0; i < columns.Count; i++)
            {
                elementDict.Add(columns[i], GetString(reader, i + 1));
            }
            return (T)Activator.CreateInstance(typeof(T), elementDict);
        }

        private String GetString(IExcelDataReader reader, int index)
        {
            var value = reader.GetValue(index);
            return (value == null) ? String.Empty : value.ToString();
        }

        /// <summary>
        /// Checks whether the given directory contains a valid IEC CDD data set.
        /// </summary>
        /// <param name="directory">The path of the directory th check</param>
        /// <returns>true if the directory contains a valid IEC CDD data set</returns>
        public static bool IsValidDirectory(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
                return false;
            try
            {
                var files = new Parser().FindFiles(directory);
                return files.ContainsKey("class") && files.ContainsKey("property");
            }
            catch (ImportException)
            {
                return false;
            }
        }
    }

    public class MissingExportFilesException : Model.ImportException
    {
        public MissingExportFilesException(string fileType)
            : base("Could not find the export file of type " + fileType)
        {
        }
    }

    public class MultipleExportFilesException : Model.ImportException
    {
        public MultipleExportFilesException(string fileType)
            : base("Found more than one export file of type " + fileType)
        {
        }
    }
}
