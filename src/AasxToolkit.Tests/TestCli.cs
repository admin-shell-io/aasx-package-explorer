﻿using System.Collections.Generic;
using NUnit.Framework;

namespace AasxToolkit.Tests
{
    public class TestWithNoCommands
    {
        [Test]
        public void TestWithNoArguments()
        {
            var cmdLine = Cli.DeclareCommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command>()
            );

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program" });
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }

        [Test]
        public void TestHelp()
        {
            var cmdLine = Cli.DeclareCommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command>()
            );

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program", "help" });
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(1, parsing.Instructions.Count);

            var instruction = parsing.Instructions[0];
            switch (instruction)
            {
                case Cli.HelpInstruction helpInstruction:
                    var writer = new System.IO.StringWriter();
                    helpInstruction.Out = writer;

                    Cli.ReturnCode code = helpInstruction.Execute();
                    Assert.AreEqual(0, code.Value);

                    var nl = System.Environment.NewLine;
                    Assert.AreEqual(
                        $"test-program:{nl}" +
                        $"  Tests something.{nl}" +
                        $"{nl}" +
                        $"Usage:{nl}" +
                        $"  test-program [list of commands]{nl}" +
                        $"{nl}" +
                        $"Commands:{nl}" +
                        $"  help{nl}" +
                        $"    Display the usage of the program and exit immediately{nl}" +
                        $"{nl}",
                        writer.ToString());
                    break;

                default:
                    throw new AssertionException(
                        $"Unexpected type of the instruction: {instruction.GetType()}");
            }
        }
    }

    public class TestWithACommand
    {
        /// <summary>
        /// Defines a global context for the execution of the dummy instructions.
        /// </summary>
        class Context
        {
            public List<string> Messages = new List<string>();
        }

        class DummyInstruction : Cli.Instruction
        {
            public Context Context;
            public readonly string Message;

            public DummyInstruction(Context context, string message)
            {
                Context = context;
                Message = message;
            }

            public override Cli.ReturnCode Execute()
            {
                Context.Messages.Add(Message);
                return null;
            }
        }

        private Cli.CommandLine setUpCommandLineWithContext(Context context)
        {
            var cmdLine = Cli.DeclareCommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command>
                {
                    new Cli.Command(
                        "say",
                        "Add a message to the dummy context.",
                        new List<Cli.Arg>
                        {
                            new Cli.Arg("message", "message to be added to the dummy context")
                        },
                        (args) => new Cli.Parsing(
                            new DummyInstruction(context, args[0])))
                }
            );

            return cmdLine;
        }

        [Test]
        public void TestWithNoArguments()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program" });
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }

        [Test]
        public void TestHelp()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program", "help" });
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(1, parsing.Instructions.Count);

            var instruction = parsing.Instructions[0];
            switch (instruction)
            {
                case Cli.HelpInstruction helpInstruction:
                    var writer = new System.IO.StringWriter();
                    helpInstruction.Out = writer;

                    Cli.ReturnCode code = helpInstruction.Execute();
                    Assert.AreEqual(0, code.Value);

                    var nl = System.Environment.NewLine;
                    Assert.AreEqual(
                        $"test-program:{nl}" +
                        $"  Tests something.{nl}" +
                        $"{nl}" +
                        $"Usage:{nl}" +
                        $"  test-program [list of commands]{nl}" +
                        $"{nl}" +
                        $"Commands:{nl}" +
                        $"  say [message]{nl}" +
                        $"    Add a message to the dummy context.{nl}" +
                        $"{nl}" +
                        $"    message:{nl}" +
                        $"      message to be added to the dummy context{nl}" +
                        $"{nl}" +
                        $"  help{nl}" +
                        $"    Display the usage of the program and exit immediately{nl}" +
                        $"{nl}",
                        writer.ToString());
                    break;

                default:
                    throw new AssertionException(
                        $"Unexpected type of the instruction: {instruction.GetType()}");
            }
        }

        [Test]
        public void TestParsingChainOfCommands()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program", "say", "one", "say", "two" });
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(2, parsing.Instructions.Count);

            Assert.IsInstanceOf<DummyInstruction>(parsing.Instructions[0]);
            Assert.IsInstanceOf<DummyInstruction>(parsing.Instructions[1]);

            var first = (DummyInstruction)parsing.Instructions[0];
            Assert.AreEqual("one", first.Message);

            var second = (DummyInstruction)parsing.Instructions[1];
            Assert.AreEqual("two", second.Message);
        }

        [Test]
        public void TestExecution()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program", "say", "one", "say", "two" });
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(2, parsing.Instructions.Count);

            Cli.Execute(parsing.Instructions);

            Assert.That(context.Messages, Is.EquivalentTo(new List<string> { "one", "two" }));
        }

        [Test]
        public void TestParsingFailedDueToTooFewArguments()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var args = new[] { "test-program", "say" };
            var parsing = Cli.ParseInstructions(cmdLine, args);
            Assert.IsNull(parsing.Instructions);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);

            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Arguments (vertically ordered):{nl}" +
                $"test-program{nl}" +
                $"say <<< PROBLEM <<<{nl}" +
                nl +
                "Too few arguments specified for the command say. It requires at least one argument.",
                errorMsg);
        }

        [Test]
        public void TestParsingFailedDueToAnUnknownCommand()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var args = new[] { "test-program", "say", "one", "unknown-command", "foobar" };
            var parsing = Cli.ParseInstructions(cmdLine, args);
            Assert.IsNull(parsing.Instructions);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);

            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Arguments (vertically ordered):{nl}" +
                $"test-program{nl}" +
                $"say{nl}" +
                $"one{nl}" +
                $"unknown-command <<< PROBLEM <<<{nl}" +
                $"foobar{nl}" +
                $"{nl}" +
                "Command unknown: unknown-command",
                errorMsg);
        }
    }
}