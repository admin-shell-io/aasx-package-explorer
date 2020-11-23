using Console = System.Console;
using Directory = System.IO.Directory;
using File = System.IO.File;
using InvalidOperationException = System.InvalidOperationException;
using Path = System.IO.Path;
using Regex = System.Text.RegularExpressions.Regex;
using StringComparison = System.StringComparison;
using StringSplitOptions = System.StringSplitOptions;
using XmlReaderSettings = System.Xml.XmlReaderSettings;
using XmlReader = System.Xml.XmlReader;
using XmlDocument = System.Xml.XmlDocument;
using XmlNode = System.Xml.XmlNode;

using System.Collections.Generic;
using System.CommandLine;



namespace CheckHeadersScript
{
    internal static class Program
    {
        private static readonly Regex OnlySpaceRe = new Regex(@"^\s+$");

        private static List<string> ExamineContent(string[] lines)
        {
            if (lines.Length < 7)
            {
                return new List<string>
                {
                    "Expected at least 7 lines " +
                    "(empty, copyright, empty, our license, empty, reference to licenses, empty), " +
                    $"but got only {lines.Length} line(s)"
                };
            }

            var result = new List<string>();

            if (lines[0] != "")
            {
                result.Add($"Expected the first line to be empty, but got: {Quote(lines[0])}");
            }

            if (lines[^1] != "")
            {
                result.Add($"Expected the last line to be empty, but got: {Quote(lines[^1])}");
            }

            if (!lines[1].StartsWith("Copyright"))
            {
                result.Add(
                    "Expected the second line to start with 'Copyright', " +
                    $"but got: {Quote(lines[1])}");
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (OnlySpaceRe.IsMatch(lines[i]))
                {
                    result.Add($"Unexpected trailing space on line {i + 1}: {Quote(lines[i])}");
                }
            }

            for (var i = 0; i < lines.Length - 1; i++)
            {
                if (lines[i] == "" && lines[i + 1] == "")
                {
                    result.Add($"Unexpected vertical space (more than one empty line) at line {i + 1}");
                }
            }

            const string ourLicense = "This source code is licensed under the Apache License 2.0 (see LICENSE.txt).";

            int licenseLine = lines.Length - 3;

            if (lines[licenseLine - 1] != ourLicense)
            {
                result.Add(
                    $"Expected the penultimate paragraph on line {licenseLine} to be a reference to our license " +
                    $"({Quote(ourLicense)}), " +
                    $"but got: {Quote(lines[licenseLine - 1])}");
            }

            const string referenceToLicenses =
                "This source code may use other Open Source software components (see LICENSE.txt).";

            var referenceLine = lines.Length - 1;
            if (lines[referenceLine - 1] != referenceToLicenses)
            {
                result.Add(
                    $"Expected the last paragraph on line {referenceLine} to be " +
                    $"{Quote(referenceToLicenses)}, " +
                    $"but got: {Quote(lines[referenceLine - 1])}");
            }

            return result;
        }

        class InvalidHeader
        {
            public readonly string? Comment;
            public readonly List<string> Errors;

            public InvalidHeader(string? comment, List<string> errors)
            {
                Comment = comment;
                Errors = errors;
            }
        }

        /// <summary>
        /// Adds quotes around the text and escapes a couple of common special characters.
        /// </summary>
        /// <remarks>Do not use <see cref="System.Text.Json.JsonSerializer"/> since it escapes so many common
        /// characters that the output is unreadable.
        /// See <a href="https://github.com/dotnet/runtime/issues/1564">this GitHub issue</a></remarks>
        private static string Quote(string text)
        {
            string escaped =
                text
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t")
                    .Replace("\a", "\\b")
                    .Replace("\b", "\\b")
                    .Replace("\v", "\\v")
                    .Replace("\f", "\\f");

            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Imitates the Python's
        /// <a href="https://docs.python.org/3/library/textwrap.html#textwrap.dedent"><c>textwrap.dedent</c></a>.
        /// </summary>
        /// <param name="text">Text to be dedented</param>
        /// <returns>array of dedented lines</returns>
        private static string[] Dedent(string text)
        {
            var lines = text.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None);

            // Search for the first non-empty line starting from the second line.
            // The first line is not expected to be indented.
            var firstNonemptyLine = -1;
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;

                firstNonemptyLine = i;
                break;
            }

            if (firstNonemptyLine < 0) return lines;

            // Search for the second non-empty line.
            // If there is no second non-empty line, we can return immediately as we
            // can not pin the indent.
            var secondNonemptyLine = -1;
            for (var i = firstNonemptyLine + 1; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;

                secondNonemptyLine = i;
                break;
            }

            if (secondNonemptyLine < 0) return lines;

            // Match the common prefix with at least two non-empty lines
            
            var firstNonemptyLineLength = lines[firstNonemptyLine].Length;
            var prefixLength = 0;
            
            for (int column = 0; column < firstNonemptyLineLength; column++)
            {
                char c = lines[firstNonemptyLine][column];
                if (c != ' ' && c != '\t') break;
                
                bool matched = true;
                for (int lineIdx = firstNonemptyLine + 1; lineIdx < lines.Length; lineIdx++)
                {
                    if (lines[lineIdx].Length == 0) continue;
                    
                    if (lines[lineIdx].Length < column + 1)
                    {
                        matched = false;
                        break;
                    }

                    if (lines[lineIdx][column] != c)
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched) break;
                
                prefixLength++;
            }

            if (prefixLength == 0) return lines;
            
            for (var i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length > 0) lines[i] = lines[i].Substring(prefixLength);
            }

            return lines;
        }
        
        private static InvalidHeader? Examine(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            switch (extension)
            {
                case ".cs":
                {
                    string text = File.ReadAllText(path);

                    if (text.Length <= 4)
                    {
                        return new InvalidHeader(
                            null, new List<string>
                            {
                                $"The file is too short (only {text.Length} character(s)). No header could be found."
                            });
                    }

                    if (!text.StartsWith("/*"))
                    {
                        return new InvalidHeader(
                            null, new List<string>
                            {
                                "The file does not start with '/*'. The first 4 characters are: " +
                                Quote(text.Substring(0, 4))
                            });
                    }

                    var commentEnds = text.IndexOf("*/", 2, StringComparison.InvariantCulture);
                    if (commentEnds == -1)
                    {
                        return new InvalidHeader(
                            null, new List<string> {"The closing '*/' could not be found."});
                    }

                    string comment = text.Substring(0, commentEnds + 2);
                    if (!comment.StartsWith("/*") || !comment.EndsWith("*/"))
                    {
                        throw new InvalidOperationException(
                            $"Invalid parsing of the comment: {Quote(comment)}");
                    }

                    string content = comment.Substring(2, comment.Length - 4);

                    string[] lines = content.Split(
                        new[] {"\r\n", "\r", "\n"},
                        StringSplitOptions.None);
                    
                    var errors = ExamineContent(lines);
                    return errors.Count == 0 ? null : new InvalidHeader(comment, errors);
                }
                case ".xaml":
                {
                    XmlReaderSettings readerSettings = new XmlReaderSettings {IgnoreComments = false};

                    using XmlReader reader = XmlReader.Create(path, readerSettings);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(reader);
                    XmlNode root = doc.DocumentElement;

                    if (root.ChildNodes.Count == 0)
                    {
                        return new InvalidHeader(
                            null, new List<string>
                            {
                                $"Expected the comment node just beneath the root node ({root.Name}), " +
                                "but found no children."
                            });
                    }
                    if (!(root.ChildNodes[0] is System.Xml.XmlComment))
                    {
                        return new InvalidHeader(
                            null, new List<string>
                            {
                                $"Expected the comment node just beneath the root node (<{root.Name}>), " +
                                $"but the first child was a node of type {root.ChildNodes[0].NodeType}."
                            });
                    }

                    string content = root.ChildNodes[0].InnerText;
                    string[] lines = Dedent(content);

                    var errors = ExamineContent(lines);
                    return errors.Count == 0 ? null : new InvalidHeader(content, errors);
                }
                default:
                    return new InvalidHeader(
                        null, new List<string>
                        {
                            "We do not know how to inspect the header of this file " +
                            "(only *.cs and *.xaml are supported)."
                        });
            }
        }

        private static int Scan(string[] inputs, string[]? excludes, bool verbose)
        {
            string cwd = Directory.GetCurrentDirectory();
            IEnumerable<string> paths = Input.MatchFiles(
                cwd,
                new List<string>(inputs),
                new List<string>(excludes ?? new string[0]));

            int exitCode = 0;

            foreach (string path in paths)
            {
                InvalidHeader? invalidHeader = Examine(path);

                if (invalidHeader == null)
                {
                    if (verbose)
                    {
                        Console.Out.WriteLine($"OK: {path}");
                    }
                }
                else
                {
                    exitCode = 1;
                    Console.Error.WriteLine($"FAIL: {path}: invalid header:");
                    if (invalidHeader.Comment != null)
                    {
                        var lines = invalidHeader.Comment.Split(
                            new[] {"\r\n", "\r", "\n"},
                            StringSplitOptions.None);
                        for (var i = 0; i < lines.Length; i++)
                        {
                            Console.Error.WriteLine($"{i + 1,2}: {lines[i]}");
                        }

                        Console.Error.WriteLine();
                    }

                    foreach (var error in invalidHeader.Errors)
                    {
                        Console.Error.WriteLine($" * {error}");
                    }
                }
            }

            return exitCode;
        }

        private static int MainWithCode(string[] args)
        {
            var rootCommand = new RootCommand(
                "Examines the headers of the source code files for copyrights and licenses.")
            {
                new Option<string[]>(
                        new[] {"--inputs", "-i"},
                        "Glob patterns of the files to be inspected")
                    {Required = true},

                new Option<string[]>(
                    new[] {"--excludes", "-e"},
                    "Glob patterns of the files to be excluded from inspection"),

                new Option<bool>(
                    new[] {"--verbose"},
                    "If set, makes the console output more verbose"
                )
            };

            rootCommand.Handler = System.CommandLine.Invocation.CommandHandler.Create(
                (string[] inputs, string[] excludes, bool verbose) => Scan(inputs, excludes, verbose));

            var exitCode = rootCommand.InvokeAsync(args).Result;
            return exitCode;
        }

        public static void Main(string[] args)
        {
            var exitCode = MainWithCode(args);
            System.Environment.ExitCode = exitCode;
        }
    }
}
