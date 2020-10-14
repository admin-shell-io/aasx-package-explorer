using System;
using ArgumentException = System.ArgumentException;
using System.Collections.Generic;
using System.Linq;

namespace AasxToolkit
{
    /// <summary>
    /// Defines command-line interface.
    ///
    /// The command-line interface consists of a command chain where each command can have none, one or more arguments.
    /// </summary>
    public static class Cli
    {
        public class ReturnCode
        {
            public readonly int Value;

            public ReturnCode(int value)
            {
                Value = value;
            }
        }
        
        public abstract class Instruction
        {
            /// <summary>
            /// Executes the instruction.
            /// </summary>
            /// <returns>Error code, if the instruction should exit, or null otherwise</returns>
            public abstract ReturnCode Execute();

            public List<string> Validate()
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Implements a pre-defined instruction to show command usage.
        /// </summary>
        public class HelpInstruction : Instruction
        {
            private CommandLine _commandLine;

            /// <summary>
            /// Specifies the writer to write the usage to.
            /// </summary>
            /// <remarks>
            /// This member was introduced so that we can test the instruction.
            /// </remarks>
            public System.IO.TextWriter Out = System.Console.Out;

            public override ReturnCode Execute()
            {
                Out.WriteLine(GenerateUsageMessage(_commandLine));
                return new ReturnCode(0);
            }

            public HelpInstruction(CommandLine commandLine)
            {
                _commandLine = commandLine;
            }
        }

        public class Parsing
        {
            public readonly Instruction Instruction;
            public readonly IReadOnlyList<string> Errors;

            public Parsing(Instruction instruction)
            {
                Instruction = instruction;
                Errors = null;
            }

            public Parsing(IReadOnlyList<string> errors)
            {
                Instruction = null;
                Errors = errors;
            }
        }

        /// <summary>
        /// Declares an argument of a command.
        /// </summary>
        public class Arg
        {
            public readonly string Name;
            public readonly string Description;

            public Arg(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        public delegate Parsing ParseFunction(IReadOnlyList<string> correspondingArguments);

        /// <summary>
        /// Declares a command of the application.
        /// </summary>
        public class Command
        {
            public readonly string Name;
            public readonly string Description;
            public readonly IReadOnlyList<Arg> Arguments;

            public readonly ParseFunction Parse;

            public Command(string name, string description, IReadOnlyList<Arg> arguments, ParseFunction parse)
            {
                Name = name;
                Description = description;
                Arguments = arguments;
                Parse = parse;
            }
        }

        /// <summary>
        /// Declares how the command-line arguments should be handled.
        /// </summary>
        public class CommandLine
        {
            /// <summary>
            /// Specify the name of the command.
            /// </summary>
            /// <remarks>
            /// This name will be shown in usage.
            /// </remarks>
            public readonly string Name;

            public readonly string Description;
            public readonly IReadOnlyList<Command> Commands;

            public CommandLine(string name, string description, IReadOnlyList<Command> commands)
            {
                // Pre-conditions
                var reserved = new HashSet<string> {"help"};
                if (commands.Any(cmd => reserved.Contains(cmd.Name)))
                {
                    throw new ArgumentException(
                        $"The following command names are reserved: {string.Join(", ", reserved)}");
                }

                var seen = new HashSet<string>();
                foreach (var cmd in commands)
                {
                    if (seen.Contains(cmd.Name))
                    {
                        throw new ArgumentException($"Duplicate command names: {cmd.Name}");
                    }

                    seen.Add(cmd.Name);
                }

                Name = name;
                Description = description;
                
                // # Join given commands and built-in commands

                var cmds = new List<Command>(commands);
                cmds.Add(
                    new Command(
                        "help",
                        "Display the usage of the program and exit immediately",
                        new List<Arg>(),
                        arguments => new Parsing(
                            new HelpInstruction(this))));

                Commands = cmds;
            }
        }

        /// <summary>
        /// Represents a parsing of the command-line arguments.
        /// </summary>
        public class CommandLineParsing
        {
            /// <summary>
            /// Specifies how many command-line arguments were accepted before the parsing stopped.
            /// </summary>
            /// <remarks>
            /// Either instructions or errors are specified, but not both.
            /// </remarks>
            public readonly int AcceptedArgs;

            public readonly IReadOnlyList<Instruction> Instructions;
            public readonly IReadOnlyList<string> Errors;

            public CommandLineParsing(int acceptedArgs, IReadOnlyList<Instruction> instructions)
            {
                AcceptedArgs = acceptedArgs;
                Instructions = instructions;
                Errors = null;
            }

            public CommandLineParsing(int acceptedArgs, IReadOnlyList<string> errors)
            {
                AcceptedArgs = acceptedArgs;
                Instructions = null;
                Errors = errors;
            }
        }

        public static class Indentation
        {
            private static string[] newLines = new[] {"\r\n", "\r", "\n"};

            // DONT-CHECK-IN add doctests
            /// <summary>
            /// Indent text by prepending indentation to each line.
            /// </summary>
            /// <param name="text">to be indented</param>
            /// <param name="indentation">to be prepended at each line</param>
            /// <returns>indented text</returns>
            /// <code doctest="true">
            /// </code>
            public static string Indent(string text, string indentation)
            {
                return string.Join(
                    System.Environment.NewLine,
                    text.Split(newLines, System.StringSplitOptions.None)
                        .Select(line => indentation + line));
            }
        }

        /// <summary>
        /// Generates the help message based on the declaration of the command line.
        /// </summary>
        /// <param name="commandLine">Corresponding command-line declaration</param>
        /// <returns>string to be readily output</returns>
        public static string GenerateUsageMessage(CommandLine commandLine)
        {
            var blocks = new List<string>();

            // # Description

            if (commandLine.Description.Length > 0)
            {
                var writer = new System.IO.StringWriter();
                writer.WriteLine($"{commandLine.Name}:");
                writer.WriteLine(Indentation.Indent(commandLine.Description, "  "));
                blocks.Add(writer.ToString());
            }
            else
            {
                blocks.Add("{commandLine.Name}:");
            }

            // # Usage

            if (commandLine.Commands.Count == 0)
            {
                blocks.Add($"Usage: {commandLine.Name}");
            }
            else
            {
                var writer = new System.IO.StringWriter();
                writer.WriteLine("Usage:");
                writer.WriteLine($"  {commandLine.Name} [list of commands]");
                blocks.Add(writer.ToString());
            }

            // # Commands

            if (commandLine.Commands.Count > 0)
            {
                var writer = new System.IO.StringWriter();
                writer.WriteLine("Commands:");

                var cmdBlocks = new List<string>();
                foreach (var cmd in commandLine.Commands)
                {
                    var cmdWriter = new System.IO.StringWriter();

                    string args =
                        ((cmd.Arguments.Count > 0) // Prefix with the space only if there are any arguments
                            ? " "
                            : "") +
                        string.Join(
                            " ",
                            cmd.Arguments.Select(arg => $"[{arg.Name}]")
                        );

                    cmdWriter.WriteLine($"  {cmd.Name}{args}");
                    cmdWriter.WriteLine(Indentation.Indent(cmd.Description, "    "));

                    foreach (var arg in cmd.Arguments)
                    {
                        cmdWriter.WriteLine();
                        cmdWriter.WriteLine($"    {arg.Name}:");
                        cmdWriter.WriteLine(Indentation.Indent(arg.Description, "      "));
                    }

                    cmdBlocks.Add(cmdWriter.ToString());
                }

                writer.Write(string.Join(System.Environment.NewLine, cmdBlocks));

                blocks.Add(writer.ToString());
            }

            // # Join the blocks

            string result = string.Join(System.Environment.NewLine, blocks);

            return result;
        }

        /// <summary>
        /// Parses the given command-line arguments.
        /// </summary>
        /// <param name="commandLine">declaration of the command line</param>
        /// <param name="args">arguments received in the Main()</param>
        /// <returns>parsing result</returns>
        public static CommandLineParsing Parse(CommandLine commandLine, IReadOnlyList<string> args)
        {
            if (args.Count == 0)
            {
                throw new ArgumentException(
                    "Unexpected zero command-line arguments. " +
                    "The command-line arguments should contain at least one element, namely the program.");
            }
            
            var instructions = new List<Instruction>();

            // Skip the first argument assuming it denotes the program and start with the second argument
            int cursor = 1;
            
            while (cursor < args.Count)
            {
                bool found = false;
                foreach (var cmd in commandLine.Commands)
                {
                    if (args[cursor] == cmd.Name)
                    {
                        if (cursor + cmd.Arguments.Count >= args.Count)
                        {
                            var itRequires = (cmd.Arguments.Count > 1)
                                ? $"It requires at least {cmd.Arguments.Count} arguments."
                                : "It requires at least one argument.";
                            
                            return new CommandLineParsing(
                                cursor,
                                new List<string>
                                {
                                    $"Too few arguments specified for the command {cmd.Name}. " + itRequires
                                });
                        }

                        var correspondingArguments = new List<string>(cmd.Arguments.Count);
                        for (int i = 0; i < cmd.Arguments.Count; i++)
                        {
                            correspondingArguments.Add(args[cursor + 1 + i]);
                        }

                        var parsing = cmd.Parse(correspondingArguments);
                        if (parsing.Instruction == null)
                        {
                            return new CommandLineParsing(cursor, parsing.Errors);
                        }

                        instructions.Add(parsing.Instruction);
                        cursor += cmd.Arguments.Count + 1;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return new CommandLineParsing(cursor, new List<string> {$"Command unknown: {args[cursor]}"});
                }
            }

            return new CommandLineParsing(0, instructions);
        }

        /// <summary>
        /// Executes the chain of instructions and stop if any of the instructions says so.
        /// </summary>
        /// <param name="instructions">Chain of instructions</param>
        /// <returns>return code after the execution, or null if the application can proceed</returns>
        public static ReturnCode Execute(IReadOnlyList<Instruction> instructions)
        {
            foreach (var instr in instructions)
            {
                ReturnCode code = instr.Execute();
                if (code != null)
                {
                    return code;
                }
            }

            return null;
        }

        public static string FormatParsingErrors(
            IReadOnlyList<string> args, int acceptedArgs, IReadOnlyList<string> errors)
        {
            // # Pre-conditions
            if (errors.Count == 0)
            {
                throw new ArgumentException("Expected at least an error, but there were no errors specified.");
            }

            if (acceptedArgs < 0)
            {
                throw new ArgumentException($"Unexpected negative {nameof(acceptedArgs)}: {acceptedArgs}");
            }

            if (args.Count <= acceptedArgs)
            {
                throw new ArgumentException(
                    "Unexpectedly too few arguments provided. " +
                    $"args.Count is {args.Count}, while {nameof(acceptedArgs)} is {acceptedArgs}.");
            }
            
            // # Implementation
            
            var writer = new System.IO.StringWriter();

            writer.WriteLine("The command-line arguments could not be parsed.");
            if (acceptedArgs == 0)
            {
                for (var i = 0; i < errors.Count; i++)
                {
                    writer.Write(errors[i]);
                    
                    if (i < errors.Count - 1)
                    {
                        writer.WriteLine();
                    }
                }
            }
            else
            {
                writer.WriteLine("Arguments (vertically ordered):");
                for (var i = 0; i < args.Count; i++)
                {
                    if (i != acceptedArgs )
                    {
                        writer.WriteLine(args[i]);
                    }
                    else
                    {
                        writer.WriteLine($"{args[i]} <<< PROBLEM <<<");
                    }
                }
                writer.WriteLine();
                for (var i = 0; i < errors.Count; i++)
                {
                    writer.Write(errors[i]);

                    if (i < errors.Count - 1)
                    {
                        writer.WriteLine();
                    }
                }
            }

            return writer.ToString();
        }
    }
}