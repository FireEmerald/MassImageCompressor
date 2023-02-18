using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;
using System.Reflection;

namespace NeoFotonCommon
{
    public static class Helper
    {
		private static List<string> extensionsAllowd
				= new List<string>(new string[] { ".JPG", ".JPEG", ".PNG", ".BMP", ".RLE", ".DIB", ".GIF", ".TIF", ".TIFF", ".WMF",
                    ".3FR" , ".ARI" , ".ARW" , ".BAY" , ".CRW" , ".CR2" , ".CAP" , ".DCS" , ".DCR" , ".DNG" , 
                    ".DRF" , ".EIP" , ".ERF" , ".FFF" , ".IIQ" , ".K25" , ".KDC" , ".MDC" , ".MEF" , ".MOS" , 
                    ".MRW" , ".NEF" , ".NRW" , ".OBM" , ".ORF" , ".PEF" , ".PTX" , ".PXN" , ".R3D" , ".RAF" , 
                    ".RAW" , ".RWL" , ".RW2" , ".RWZ" , ".SR2" , ".SRF" , ".SRW" , ".X3F"
                    });
        private static List<string> rawExtensionsAllowd = new List<string>(new string[] { 
                        ".3FR" , ".ARI" , ".ARW" , ".BAY" , ".CRW" , ".CR2" , ".CAP" , ".DCS" , ".DCR" , ".DNG" , 
                        ".DRF" , ".EIP" , ".ERF" , ".FFF" , ".IIQ" , ".K25" , ".KDC" , ".MDC" , ".MEF" , ".MOS" , 
                        ".MRW" , ".NEF" , ".NRW" , ".OBM" , ".ORF" , ".PEF" , ".PTX" , ".PXN" , ".R3D" , ".RAF" , 
                        ".RAW" , ".RWL" , ".RW2" , ".RWZ" , ".SR2" , ".SRF" , ".SRW" , ".X3F"
                    });

		public static string strJpegExtension = ".jpeg";
		public static string strPngExtension = ".png";
		
		public static bool IsSupportedImage(string filePath)
		{
			if (Helper.extensionsAllowd.Contains(Path.GetExtension(filePath).ToUpper()))
				return true;
			return false;
		}

        public static bool IsRaw(string filePath)
        {
            if (Helper.rawExtensionsAllowd.Contains(Path.GetExtension(filePath).ToUpper()))
                return true;
            return false;
        }

        public static System.Drawing.Bitmap GetRawImageData(string filePath, string execDcrawExe)
        {
            var f = new FileInfo(execDcrawExe);
            var args = "-c -T";

            var startInfo = new System.Diagnostics.ProcessStartInfo(f.FullName)
            {
                Arguments = args + " \"" + filePath + "\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = System.Diagnostics.Process.Start(startInfo);

            if (process == null) return null;

            try
            {
                return (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(process.StandardOutput.BaseStream, true, true);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }
               
        public static void CompressPNG(string filePath, int qualityLevel, bool waitForProcessToEnd)
        {
            var exeFilePath = GetFullPath(@"Exec\pngquant.exe");
            var args = " --quality=0-" + qualityLevel + " -f --ext .png ";

            RunExternalOperationOnImage(filePath, exeFilePath, args, waitForProcessToEnd);
        }


        public static void OptimizePNG(string filePath, bool waitForProcessToEnd)
        {

            var exeFilePath = GetFullPath(@"Exec\optipng.exe");
            var args = "-o2 -strip all";
            RunExternalOperationOnImage(filePath, exeFilePath, args, waitForProcessToEnd);
        }

        public static long GetFileSize(string filePath)
        {
            return new FileInfo(filePath).Length;
        }

        public static bool SavingAsSameMimeType(string filePath, SupportedMimeType type)
        {
            string ext = Path.GetExtension(filePath);

            if (type == SupportedMimeType.ORIGINAL)
                return true;

            if ((ext.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase))
                && (type == SupportedMimeType.JPEG))
                return true;
            if (ext.Equals(".png", StringComparison.InvariantCultureIgnoreCase) && (type == SupportedMimeType.PNG))
                return true;
            return false;
        }

        public static void MakeJpegProgressive(string filePath, bool waitForProcessToEnd)
        {

            var exeFilePath = GetFullPath(@"Exec\jpegtran.exe");
            var args = "-copy all -optimize -progressive -outfile " + " \"" + filePath + "\"";
            RunExternalOperationOnImage(filePath, exeFilePath, args, waitForProcessToEnd);
        }

        private static void RunExternalOperationOnImage(string filePath, string exeFilePath, string args, bool waitForProcessToEnd)
        {
            var f = new FileInfo(exeFilePath);
            var startInfo = new System.Diagnostics.ProcessStartInfo(f.FullName)
            {
                Arguments = args + " \"" + filePath + "\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = System.Diagnostics.Process.Start(startInfo);

            if (waitForProcessToEnd)
                process.WaitForExit();
        }


        private static string GetFullPath(string relativePath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativePath);
        }

        public static System.Drawing.Bitmap GetBitmap(string filepath)
        {
            if (IsRaw(filepath))
                return GetRawImageData(filepath, GetFullPath(@"Exec\dcraw.exe"));
            else
                return new System.Drawing.Bitmap(filepath);
        }

        public static bool isPathWithFile(string filePath)
        {
            return File.Exists(filePath);
        }

		public static void SetExtension(string filePath)
		{
			string fileExtension = Path.GetExtension(filePath);

			if (fileExtension.ToUpper().Contains("JP"))
				strJpegExtension = fileExtension;
			else if (fileExtension.ToUpper().Contains("PNG"))
				strPngExtension = fileExtension;
		}

		public static string AddDirectorySeparatorAtEnd(string path)
		{
			if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
				return path + Path.DirectorySeparatorChar.ToString();
			return path;
		}

		public static string ChangeExensionToMimeType(string fullFilePath, SupportedMimeType type)
		{
			string dirPath = RemoveFileName(fullFilePath);

            if (type == SupportedMimeType.JPEG || Helper.IsRaw(fullFilePath))
                return
                    Helper.AddDirectorySeparatorAtEnd(dirPath)
                    + Path.GetFileNameWithoutExtension(fullFilePath)
                    + Helper.strJpegExtension;
            else if (type == SupportedMimeType.PNG)
                return
                    Helper.AddDirectorySeparatorAtEnd(dirPath)
                    + Path.GetFileNameWithoutExtension(fullFilePath)
                    + Helper.strPngExtension;
            else
                return fullFilePath;
		}

        public static string RemoveFileName(string pathWithFileName)
        {
            string dirPath = "";

            if (string.IsNullOrEmpty(Path.GetFileName(pathWithFileName)))
                return "";

            string[] DirsAndFile = pathWithFileName.Split(Path.DirectorySeparatorChar);
            if (DirsAndFile == null) return "";

            for (int i = 0; i < DirsAndFile.Length - 1; i++)
				dirPath += DirsAndFile[i] + Path.DirectorySeparatorChar;
            return dirPath;
        }

        public static string GetReadableBytes(long bytesCount)
        {
            switch(bytesCount.ToString().Length)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    //Bytes
                    return (bytesCount.ToString() + " Bytes");
                case 5:
                case 6:
                    //KBS
                    return ((bytesCount/1024).ToString() + " KB");
                default:
					return ((bytesCount / 1048576.00).ToString(".00") + " MB");
                    //MBs
            }
        }

        public static ImageCodecInfo GetEncoderInfoFromOriginalFile(String filePath)
        {
            string inputfileExt = "*" + Path.GetExtension(filePath).ToUpper();

            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].FilenameExtension.Contains(inputfileExt))
                    return encoders[j];
            }

            return null;
        }

        public static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }

    /// <summary>Event arguments representing the completion of a parallel action.</summary>
    public class ParallelEventArgs : EventArgs
    {
        private readonly object state;
        private readonly Exception exception;
        internal ParallelEventArgs(object state, Exception exception)
        {
            this.state = state;
            this.exception = exception;
        }

        /// <summary>The opaque state object that identifies the action (null otherwise).</summary>
        public object State { get { return state; } }

        /// <summary>The exception thrown by the parallel action, or null if it completed without exception.</summary>
        public Exception Exception { get { return exception; } }
    }

    /// <summary>Provides a caller-friendly wrapper around parallel actions.</summary>
    public sealed class Forker
    {
        int running;
        private readonly object joinLock = new object(), eventLock = new object();

        /// <summary>Raised when all operations have completed.</summary>
        public event EventHandler AllComplete
        {
            add { lock (eventLock) { allComplete += value; } }
            remove { lock (eventLock) { allComplete -= value; } }
        }
        private EventHandler allComplete;
        /// <summary>Raised when each operation completes.</summary>
        public event EventHandler<ParallelEventArgs> ItemComplete
        {
            add { lock (eventLock) { itemComplete += value; } }
            remove { lock (eventLock) { itemComplete -= value; } }
        }
        private EventHandler<ParallelEventArgs> itemComplete;

        private void OnItemComplete(object state, Exception exception)
        {
            EventHandler<ParallelEventArgs> itemHandler = itemComplete; // don't need to lock
            if (itemHandler != null) itemHandler(this, new ParallelEventArgs(state, exception));
            if (Interlocked.Decrement(ref running) == 0)
            {
                EventHandler allHandler = allComplete; // don't need to lock
                if (allHandler != null) allHandler(this, EventArgs.Empty);
                lock (joinLock)
                {
                    Monitor.PulseAll(joinLock);
                }
            }
        }

        /// <summary>Adds a callback to invoke when each operation completes.</summary>
        /// <returns>Current instance (for fluent API).</returns>
        public Forker OnItemComplete(EventHandler<ParallelEventArgs> handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            ItemComplete += handler;
            return this;
        }

        /// <summary>Adds a callback to invoke when all operations are complete.</summary>
        /// <returns>Current instance (for fluent API).</returns>
        public Forker OnAllComplete(EventHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            AllComplete += handler;
            return this;
        }

        /// <summary>Waits for all operations to complete.</summary>
        public void Join()
        {
            Join(-1);
        }

        /// <summary>Waits (with timeout) for all operations to complete.</summary>
        /// <returns>Whether all operations had completed before the timeout.</returns>
        public bool Join(int millisecondsTimeout)
        {
            lock (joinLock)
            {
                if (CountRunning() == 0) return true;
                Thread.SpinWait(1); // try our luck...
                return (CountRunning() == 0) ||
                    Monitor.Wait(joinLock, millisecondsTimeout);
            }
        }

        /// <summary>Indicates the number of incomplete operations.</summary>
        /// <returns>The number of incomplete operations.</returns>
        public int CountRunning()
        {
            return Interlocked.CompareExchange(ref running, 0, 0);
        }

        /// <summary>Enqueues an operation.</summary>
        /// <param name="action">The operation to perform.</param>
        /// <returns>The current instance (for fluent API).</returns>
        public Forker Fork(ThreadStart action) { return Fork(action, null); }

        /// <summary>Enqueues an operation.</summary>
        /// <param name="action">The operation to perform.</param>
        /// <param name="state">An opaque object, allowing the caller to identify operations.</param>
        /// <returns>The current instance (for fluent API).</returns>
        public Forker Fork(ThreadStart action, object state)
        {
            if (action == null) throw new ArgumentNullException("action");
            Interlocked.Increment(ref running);
            ThreadPool.QueueUserWorkItem(delegate
            {
                Exception exception = null;
                try { action(); }
                catch (Exception ex) { exception = ex; }
                OnItemComplete(state, exception);
            });
            return this;
        }
    }
}
