using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data;

namespace GatebluServiceTray
{
	public class Program
	{
		internal static Properties.Settings Settings { get { return Properties.Settings.Default; } }
		public const string logFile = "log.txt";
		public const string AppName = "GatebluService";
        public Process process;
		protected NotifyIcon icon;

        // from http://stackoverflow.com/questions/206323/how-to-execute-command-line-in-c-get-std-out-results
        const string ToolFileName = "C:\\Program Files (x86)\\Octoblu\\GatebluService\\npm.cmd";
        private string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }
        string RunExternalExe(string filename, string arguments = null)
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
            string gatebluDir = @"C:\\Program Files\\Octoblu\\GatebluService";
            
            if (Environment.Is64BitProcess)
            {
               gatebluDir = @"C:\\Program Files (x86)\\Octoblu\\GatebluService";

            }

            process.StartInfo.WorkingDirectory = gatebluDir;
            string path = System.Environment.GetEnvironmentVariable("PATH");
            process.StartInfo.EnvironmentVariables["PATH"] = gatebluDir + @";" + path;
            var stdOutput = new StringBuilder();
            process.OutputDataReceived += (sender, args) => stdOutput.Append(args.Data);

            string stdError = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
            }

            if (process.ExitCode == 0)
            {
                return stdOutput.ToString();
            }
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }

                throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
        }

		[STAThread]
		static void Main(string[] args)
		{
			var p = new Program();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
            Application.Run();
		}

		public Program()
		{
			InitTrayIcon();
            RunExternalExe(ToolFileName, "start");
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

			Application.ApplicationExit += (obj, args) =>
			{
                process.Kill();
				icon.Dispose();
				context.Dispose();
			};
		}
	}
}
