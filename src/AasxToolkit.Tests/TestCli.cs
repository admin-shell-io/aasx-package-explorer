using System;
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

            var parsing = Cli.ParseInstructions(cmdLine, new string[] { });
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }
    }

    public class TestWithACommand
    {
        private Cli.CommandLine setUpCommandLine()
        {
            // We use a subset of commands to make the testing simpler, in particular when it comes to testing
            // the help message.
            var cmdLoad = new Cli.Command(
                "load",
                "loads the AASX package into RAM.",
                new[]
                {
                    new Cli.Arg(
                        "package-file",
                        "Path to the AASX package."),
                },
                (cmdArgs) =>
                {
                    var pth = cmdArgs[0];
                    return new Cli.Parsing(new Instruction.Load(pth));
                }
            );

            var cmdLine = Cli.DeclareCommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command> { cmdLoad }
            );

            return cmdLine;
        }

        [Test]
        public void TestWithNoArguments()
        {
            var cmdLine = setUpCommandLine();

            var parsing = Cli.ParseInstructions(cmdLine, new string[] { });
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
                $"  load [package-file]{nl}" +
                $"    loads the AASX package into RAM.{nl}" +
                $"{nl}" +
                $"    package-file:{nl}" +
                $"      Path to the AASX package.{nl}",
                help);
        }

        [Test]
        public void TestParsingChainOfCommands()
        {
            var cmdLine = setUpCommandLine();

            var parsing = Cli.ParseInstructions(cmdLine, new[] { "load", "one", "load", "two" });
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(2, parsing.Instructions.Count);

            Assert.IsInstanceOf<Instruction.Load>(parsing.Instructions[0]);
            Assert.IsInstanceOf<Instruction.Load>(parsing.Instructions[1]);

            var first = (Instruction.Load)parsing.Instructions[0];
            Assert.AreEqual("one", first.Path);

            var second = (Instruction.Load)parsing.Instructions[1];
            Assert.AreEqual("two", second.Path);
        }

        [Test]
        public void TestParsingNoArgumentsGivesEmptyInstructions()
        {
            var cmdLine = setUpCommandLine();

            var args = new string[] { };
            var parsing = Cli.ParseInstructions(cmdLine, args);
            Assert.IsEmpty(parsing.Instructions);
            Assert.AreEqual(0, parsing.AcceptedArgs);
            Assert.IsNull(parsing.Errors);
        }

        [Test]
        public void TestParsingFailedDueToTooFewArguments()
        {
            var cmdLine = setUpCommandLine();

            var args = new[] { "load" };
            var parsing = Cli.ParseInstructions(cmdLine, args);
            Assert.IsNull(parsing.Instructions);
            Assert.AreEqual(0, parsing.AcceptedArgs);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);

            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Arguments (vertically ordered):{nl}" +
                $"load <<< PROBLEM <<<{nl}" +
                nl +
                "Too few arguments specified for the command load. It requires at least one argument.",
                errorMsg);
        }

        [Test]
        public void TestParsingFailedDueToAnUnknownCommand()
        {
            var cmdLine = setUpCommandLine();

            var args = new[] { "load", "one", "unknown-command", "foobar" };
            var parsing = Cli.ParseInstructions(cmdLine, args);
            Assert.IsNull(parsing.Instructions);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);

            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Arguments (vertically ordered):{nl}" +
                $"load{nl}" +
                $"one{nl}" +
                $"unknown-command <<< PROBLEM <<<{nl}" +
                $"foobar{nl}" +
                $"{nl}" +
                "Command unknown: unknown-command",
                errorMsg);
        }
    }
}
