using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FreeMarketOne.Extensions.Helpers
{
	public static class EnvironmentHelper
	{
		/// <summary>
		/// Executes a command with bash.
		/// https://stackoverflow.com/a/47918132/2061103
		/// </summary>
		/// <param name="cmd"></param>
		public static int ShellExec(string cmd, bool waitForExit = true)
		{
			var escapedArgs = cmd.Replace("\"", "\\\"");

            using (var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
            {
                if (waitForExit)
                {
                    process.WaitForExit();
                    return process.ExitCode;
                }
            }

            return 0;
		}
	}
}
