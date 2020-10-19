using Console = System.Console;
using IDisposable = System.IDisposable;
using StringWriter = System.IO.StringWriter;
using TextWriter = System.IO.TextWriter;

namespace AasxToolkit.Test
{
    public class ConsoleCapture : IDisposable
    {
        private readonly StringWriter _writerOut;
        private readonly StringWriter _writerError;
        private readonly TextWriter _originalOutput;
        private readonly TextWriter _originalError;

        public ConsoleCapture()
        {
            _writerOut = new StringWriter();
            _writerError = new StringWriter();

            _originalOutput = Console.Out;
            _originalError = Console.Error;

            Console.SetOut(_writerOut);
            Console.SetError(_writerError);
        }

        public string Output()
        {
            return _writerOut.ToString();
        }

        public string Error()
        {
            return _writerError.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            Console.SetError(_originalError);
            _writerOut.Dispose();
            _writerOut.Dispose();
        }
    }
}
