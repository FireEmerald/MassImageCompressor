using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using NeoFotonCommon;
using System.Threading;
using System.Diagnostics;

namespace NeoFoton
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "INSTALLER") { Process.Start(Application.ExecutablePath); return; }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainUI());
            CleanUp();
        }

        private static void CleanUp()
        {
            string tempDirPath = Helper.AddDirectorySeparatorAtEnd(Path.GetTempPath())
						+ "ImageCompressor300889D794E649e896387D28A2EB5836";

			try
			{
				if (Directory.Exists(tempDirPath))
					Directory.Delete(tempDirPath, true);
			}
			catch
			{
				//eat exception if temp directory is in use or cannot delete
			}
        }

    }
}