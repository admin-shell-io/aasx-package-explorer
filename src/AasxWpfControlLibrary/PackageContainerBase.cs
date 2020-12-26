using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPackageExplorer;
using AdminShellNS;

namespace AasxWpfControlLibrary
{
    /// <summary>
    /// Excpetions thrown when handling PackageContainer or PackageCentral
    /// </summary>
    public class PackageContainerException : Exception {
        public PackageContainerException() { }
        public PackageContainerException(string message) : base(message) { }
    }

    /// <summary>
    /// The container wraps an AdminShellPackageEnv with the availability to upload, download, re-new the package env
    /// and to transport further information (future use).
    /// </summary>
    public class PackageContainerBase
    {
        public enum Format { Unknown = 0, AASX, XML, JSON }
        public static string[] FormatExt = { ".bin", ".aasx", ".xml", ".json" };

        public AdminShellPackageEnv Env;
        public Format IsFormat = Format.Unknown;

        /// <summary>
        /// If true, then PackageContainer will try to automatically load the contents of the package
        /// on application level.
        /// </summary>
        public bool LoadResident;

        //
        // Different capabilities are modelled as delegates, which can be present or not (null), depening
        // on dynamic protocoll capabilities
        //

        /// <summary>
        /// Can load an AASX from (already) given data source
        /// </summary>
        public delegate void CapabilityLoadFromSource();

        /// <summary>
        /// Can save the (edited) AASX to an already given or new dta source name
        /// </summary>
        /// <param name="saveAsNewFilename"></param>
        public delegate void CapabilitySaveAsToSource(string saveAsNewFilename = null);

        // the derived classes will selctively set the capabilities
        public CapabilityLoadFromSource LoadFromSource = null;
        public CapabilitySaveAsToSource SaveAsToSource = null;

        //
        // Base functions
        //

        public static Format EvalFormat(string fn)
        {
            Format res = Format.Unknown;
            var ext = Path.GetExtension(fn).ToLower();
            foreach (var en in (Format[])Enum.GetValues(typeof(Format)))
                if (ext == FormatExt[(int)en])
                    res = en;
            return res;
        }

        public bool IsOpen { get { return Env != null && Env.IsOpen; } }

        public void Close()
        {
            if (!IsOpen)
                return;
            Env.Close();
            Env = null;
        }
    }

    /// <summary>
    /// This container was taken over from AasxPackageEnv and lacks therefore further
    /// load/ store information
    /// </summary>
    public class PackageContainerTakenOver : PackageContainerBase
    {
    }

    /// <summary>
    /// This container add functionalities for "indirect load/ save" and backing up file contents
    /// </summary>
    public class PackageContainerBuffered : PackageContainerBase
    {
        public bool IndirectLoadSave = false;

        public string TempFn;

        public string CreateNewTempFn(string sourceFn, Format fmt)
        {
            // TODO (MIHO, 2020-12-25): think of creating a temp file which resemebles the source file
            // name (for ease of handling)
            var res = System.IO.Path.GetTempFileName().Replace(".tmp", FormatExt[(int)fmt]);
            return res;
        }
    }    

    /// <summary>
    /// This container represents a file, which is locally accessible via the computer's file system.
    /// </summary>
    public class PackageContainerLocalFile : PackageContainerBuffered
    {
        /// <summary>
        /// The file on the computer's file system, which is perceived by the user as the opened
        /// AASX file.
        /// </summary>
        public string SourceFn;

        public PackageContainerLocalFile() 
        {
            Init();
        }

        public PackageContainerLocalFile(string sourceFn, bool loadResident = false)
        {
            Init();
            SetNewSourceFn(sourceFn);
            LoadResident = loadResident;
            if (LoadResident)
                LoadFromSource();
        }

        private void Init()
        {
            this.LoadFromSource = this.InternalLoadFromSource;
            this.SaveAsToSource = this.InternalSaveToSource;
        }

        private void SetNewSourceFn(string sourceFn)
        {
            SourceFn = sourceFn;
            IsFormat = EvalFormat(SourceFn);
            IndirectLoadSave = Options.Curr.IndirectLoadSave && IsFormat == Format.AASX;
        }

        public override string ToString()
        {
            var s = "local file: " + SourceFn;
            if (IndirectLoadSave)
                s += "buffered to: " + TempFn;
            return s;
        }

        protected void InternalLoadFromSource()
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While loading aasx, unknown file format/ extension was encountered!");

            // buffer
            var fn = SourceFn;
            try
            {
                if (IndirectLoadSave)
                {
                    TempFn = CreateNewTempFn(SourceFn, IsFormat);
                    fn = TempFn;
                    System.IO.File.Copy(SourceFn, fn);
                }
                else
                {
                    TempFn = null;
                }
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While buffering aasx from {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            // open
            try
            {
                // TODO (MIHO, 2020-12-15): consider removing "indirectLoadSave" from AdminShellPackageEnv
                Env = new AdminShellPackageEnv(fn, indirectLoadSave: false);
            }
            catch (Exception ex)
            {
                throw new PackageContainerException(
                    $"While opening aasx {fn} from source {this.ToString()} " +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        protected void InternalSaveToSource(string saveAsNewFileName = null)
        {
            // check extension
            if (IsFormat == Format.Unknown)
                throw new PackageContainerException(
                    "While saving aasx, unknown file format/ extension was encountered!");

            // check open package
            if (Env == null || !Env.IsOpen)
            {
                Env = null;
                throw new PackageContainerException(
                    "While saving aasx, package was indeed not existng or not open!");
            }

            // divert on indirect load/ save, to have dedicated try&catch
            if (IndirectLoadSave)
            {
                // apply possible new source name directly
                if (saveAsNewFileName != null)
                    SetNewSourceFn(saveAsNewFileName);

                // do a close, execute and re-open cycle
                try
                {
                    Env.TemporarilyCloseAndReOpenPackage(() => {
                        System.IO.File.Copy(Env.Filename, SourceFn, overwrite: true);
                    });
                }
                catch (Exception ex)
                {
                    throw new PackageContainerException(
                        $"While indirect-saving aasx to source {this.ToString()} " +
                        $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }                
            }
            else
            {
                // new file?
                if (saveAsNewFileName != null)
                {
                    // save as
                    try
                    {
                        Env.SaveAs(saveAsNewFileName);
                        SetNewSourceFn(saveAsNewFileName);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While saving aasx to new source {saveAsNewFileName} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
                else
                { 
                    // just save
                    try
                    {
                        Env.SaveAs(SourceFn);
                    }
                    catch (Exception ex)
                    {
                        throw new PackageContainerException(
                            $"While direct-saving aasx to source {this.ToString()} " +
                            $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// This container represents a file, which is retrieved by an network/ HTTP commands and
    /// buffered locally.
    /// </summary>
    public class PackageContainerNetworkHttpFile : PackageContainerBuffered
    {
        /// <summary>
        /// Uri of an AASX retrieved by HTTP
        /// </summary>
        public Uri SourceUri;

        public override string ToString()
        {
            return "HTTP file: " + SourceUri;
        }

        protected void InternalLoadFromSource()
        {

        }
    }
}
