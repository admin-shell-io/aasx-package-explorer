using System;
using System.Collections.Generic;
using System.Security.Claims;
using NUnit.Framework;

namespace AasxToolkit.Tests
{
    public class TestWithNoCommands
    {
        [Test]
        public void TestWithNoArguments()
        {
            var cmdLine = new Cli.CommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command>()
            );

            var parsing = Cli.Parse(cmdLine, new string[] { });
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }

        [Test]
        public void TestHelp()
        {
            var cmdLine = new Cli.CommandLine(
                "test-program",
                "Tests something.",
                new List<Cli.Command>()
            );

            var parsing = Cli.Parse(cmdLine, new string[] {"help"});
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(1, parsing.Instructions.Count);

            var instruction = parsing.Instructions[0];
            switch (instruction)
            {
                case Cli.HelpInstruction helpInstruction:
                    var writer = new System.IO.StringWriter();
                    helpInstruction.Out = writer;

                    bool shouldContinue = helpInstruction.Execute();
                    Assert.IsFalse(shouldContinue);

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

            public override bool Execute()
            {
                Context.Messages.Add(Message);
                return true;  // should continue execution
            }
        }

        private Cli.CommandLine setUpCommandLineWithContext(Context context)
        {
            var cmdLine = new Cli.CommandLine(
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
                        (args) => new Cli.InstructionParsing(
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

            var parsing = Cli.Parse(cmdLine, new string[]{});
            Assert.IsNull(parsing.Errors);
            Assert.IsEmpty(parsing.Instructions);
        }

        [Test]
        public void TestHelp()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.Parse(cmdLine, new string[]{"help"});
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(1, parsing.Instructions.Count);

            var instruction = parsing.Instructions[0];
            switch (instruction)
            {
                case Cli.HelpInstruction helpInstruction:
                    var writer = new System.IO.StringWriter();
                    helpInstruction.Out = writer;

                    bool shouldContinue = helpInstruction.Execute();
                    Assert.IsFalse(shouldContinue);

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

            var parsing = Cli.Parse(cmdLine, new string[]{"say", "one", "say", "two"});
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(2, parsing.Instructions.Count);

            Assert.IsInstanceOf<DummyInstruction>(parsing.Instructions[0]);
            Assert.IsInstanceOf<DummyInstruction>(parsing.Instructions[1]);

            var first = (DummyInstruction) parsing.Instructions[0];
            Assert.AreEqual("one", first.Message);
            
            var second = (DummyInstruction) parsing.Instructions[1];
            Assert.AreEqual("two", second.Message);
        }
        
        [Test]
        public void TestExecution()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var parsing = Cli.Parse(cmdLine, new string[]{"say", "one", "say", "two"});
            Assert.IsNull(parsing.Errors);
            Assert.AreEqual(2, parsing.Instructions.Count);

            Cli.Execute(parsing.Instructions);

            Assert.That(context.Messages, Is.EquivalentTo(new List<string> {"one", "two"}));
        }
        
        [Test]
        public void TestParsingFailedDueToTooFewArguments()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var args = new[] {"say"};
            var parsing = Cli.Parse(cmdLine, args);
            Assert.IsNull(parsing.Instructions);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);

            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Too few arguments specified for the command say. It requires at least one argument.",
                errorMsg);
        }
        
        [Test]
        public void TestParsingFailedDueToAnUnknownCommand()
        {
            var context = new Context();

            var cmdLine = setUpCommandLineWithContext(context);

            var args = new[] {"say", "one", "unknown-command", "foobar"};
            var parsing = Cli.Parse(cmdLine, args);
            Assert.IsNull(parsing.Instructions);

            var errorMsg = Cli.FormatParsingErrors(args, parsing.AcceptedArgs, parsing.Errors);
            
            var nl = System.Environment.NewLine;
            Assert.AreEqual(
                $"The command-line arguments could not be parsed.{nl}" +
                $"Arguments (vertically ordered):{nl}" +
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