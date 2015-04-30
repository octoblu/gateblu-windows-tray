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
            process.StartInfo.WorkingDirectory = "C:\\Program Files (x86)\\Octoblu\\GatebluService";
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
			Application.ThreadException += (o, e) => p.Log(e.Exception);
			Application.Run();
		}

		public Program()
		{
			InitTrayIcon();
            RunExternalExe(ToolFileName, "start");
            //Log(String.Format("Sync started. Local path: {0}, Remote path: {1}", Settings.LocalPath, Settings.RemotePath));
		}

		private void InitTrayIcon()
		{
			ContextMenuStrip context = new ContextMenuStrip();
			context.Items.AddRange(new ToolStripItem[] 
			{
				new ToolStripMenuItem("Show log", null, (q, w) => Process.Start(logFile)),
				new ToolStripMenuItem("Clear log", null, (q, w) => File.Delete(logFile)),
				new ToolStripMenuItem("Open app location", null, (q, w) => Process.Start(Path.GetDirectoryName(Application.ExecutablePath))),
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
					//Process.Start(logFile);
			};

			Application.ApplicationExit += (obj, args) =>
			{
                process.Kill();
				icon.Dispose();
				context.Dispose();
			};
		}

		public void Log(string message)
		{
			File.AppendAllText(logFile, String.Format("[{0}] {1}\r\n", DateTime.Now, message));
		}
		public void Log(Exception e)
		{
			Log(String.Format("Exception: {0}\r\n{1}", e.Message, e.StackTrace));
			if (icon != null)
				icon.ShowBalloonTip(1000, "Error", e.Message, ToolTipIcon.Error);
		}
	}
}
