using System;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace KS.Foundation
{
	public static class ProcessHelpers
	{
		public static int RunCommand(this string executablePath, string args)
		{
			Process process = null;

			try {
				process = new Process();
				process.StartInfo.FileName = executablePath;
				process.StartInfo.Arguments = args;
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.RedirectStandardOutput = false;
				process.StartInfo.RedirectStandardError = false;
				process.Start();
				process.WaitForExit();
				return process.ExitCode;
			} catch (Exception ex) {			
				ex.LogError ("RunCommand");
				return -1;
			}
			finally  {
				if (process != null)
					process.Dispose ();
			}
		}

		public static string RunConsoleOutput(this string executablePath, string args)
		{			
			Process process = null;

			try {
				process = new Process();
				process.StartInfo.FileName = executablePath;
				process.StartInfo.Arguments = args;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.Start();
				//* Read the output (or the error)
				string output = process.StandardOutput.ReadToEnd();
				Console.WriteLine(output);
				string err = process.StandardError.ReadToEnd();
				Console.WriteLine(err);
				process.WaitForExit();
				return output;
			} catch (Exception ex) {			
				ex.LogError ("RunConsoleOutput");
				return string.Empty;
			}
			finally  {
				if (process != null)
					process.Dispose ();
			}
		}

		static string RunConsoleOutputAsync(this string executablePath, string args) {

			Process process = null;
			StringBuilder sbData = new StringBuilder();

			try {
				process = new Process();
				process.StartInfo.FileName = executablePath;
				process.StartInfo.Arguments = args;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				//* Set your output and error (asynchronous) handlers
				//process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
				process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => sbData.Append(e.Data);

				//process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
				process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => e.LogError (e.Data);
				//* Start process and handlers
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();	
				return sbData.ToString ();
			} catch (Exception ex) {
				ex.LogError ("RunConsoleOutputAsync");
				return String.Empty;
			}
			finally  {
				if (process != null)
					process.Dispose ();
			}
		}
	}
}

