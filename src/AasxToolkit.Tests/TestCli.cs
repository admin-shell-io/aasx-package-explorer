using System.Collections.Generic;
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
    }

    public class TestWithACommand
    {
        class DummyInstruction : Cli.IInstruction
        {
            public readonly string Message;

            public DummyInstruction(string message)
            {
                Message = message;
            }
        }

        private Cli.CommandLine setUpCommandLine()
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
                        (args) => new Cli.Parsing(new DummyInstruction(args[0])))
                }
            );

            return cmdLine;
        }

        [Test]
        public void TestWithNoArguments()
        {
            var cmdLine = setUpCommandLine();

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "test-program" });
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }

        [Test]
        public void TestHelp()
        {
            var cmdLine = setUpCommandLine();

            string help = Cli.GenerateUsageMessage(cmdLine);

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
                $"      message to be added to the dummy context{nl}",
                help);
        }

        [Test]
        public void TestParsingChainOfCommands()
        {
            var cmdLine = setUpCommandLine();

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
        public void TestParsingFailedDueToTooFewArguments()
        {
            var cmdLine = setUpCommandLine();

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
            var cmdLine = setUpCommandLine();

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