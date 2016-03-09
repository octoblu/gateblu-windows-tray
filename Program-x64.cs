using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data;
using log4net;

namespace GatebluServiceTray
{
	public class Program
	{
		internal static Properties.Settings Settings { get { return Properties.Settings.Default; } }
		public const string AppName = "GatebluService";
        public string gatebluDir = @"C:\Program Files\Octoblu\GatebluService";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Process process;
		protected NotifyIcon icon;

        // from http://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
        private string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }
        void RunExternalExe(string filename, string arguments = null)
        {
            process = new Process();

            process.StartInfo.FileName = filename;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.StartInfo.WorkingDirectory = gatebluDir;
            string path = System.Environment.GetEnvironmentVariable("PATH");
            process.StartInfo.EnvironmentVariables["PATH"] = gatebluDir + @";" + path;
            process.OutputDataReceived += (sender, args) =>
                log.Debug(args.Data);
            process.ErrorDataReceived += (sender, args) =>
                log.Error(args.Data);

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                log.Fatal("OS error while executing " + Format(filename, arguments) + ": " + e.Message);
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }
        }

		[STAThread]
		static void Main(string[] args)
		{
            log4net.Config.XmlConfigurator.Configure();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            var p = new Program();
            Application.Run();
		}

		public Program()
		{
			InitTrayIcon();
            RunExternalExe(gatebluDir + @".\npm.cmd", "start");
		}

		private void InitTrayIcon()
		{
			ContextMenuStrip context = new ContextMenuStrip();
			context.Items.AddRange(new ToolStripItem[]
			{
				new ToolStripMenuItem("Exit", null, (q, w) => Application.Exit())
			});

			icon = new NotifyIcon()
			{
				Icon = Properties.Resources.synchronize,
				Text = AppName,
				ContextMenuStrip = context,
				Visible = true
			};

            icon.MouseClick += (obj, args) =>
			{
				//if (args.Button == MouseButtons.Left)
				//	Process.Start();
			};

			Application.ApplicationExit += (obj, args) =>
			{
                if (!process.HasExited)
                {
                    process.Kill();
                }
				icon.Dispose();
				context.Dispose();
			};
		}
	}
}
