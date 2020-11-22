using IDisposable = System.IDisposable;

namespace AasxPackageExplorer.GuiTests
{
    class TemporaryDirectory : IDisposable
    {
        public readonly string Path;

        public TemporaryDirectory()
        {
            this.Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName());

            System.IO.Directory.CreateDirectory(this.Path);
        }

        public void Dispose()
        {
            System.IO.Directory.Delete(this.Path, true);
        }
    }
}
