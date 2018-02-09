using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NodeReuseWaitForExit
{
    class Program
    {
        private const string _classlibPath = @"..\..\..\..\classlib";

        static void Main(string[] args)
        {
            // Kill all msbuild persistent nodes to start from a clean state
            RunProcess("taskkill.exe", "/f /im msbuild.exe");
            RunProcess("taskkill.exe", "/f /fi \"modules eq microsoft.build.dll\"");

            // WaitForExit() unblocks after msbuild.exe parent process exits
            RunProcess("msbuild.exe", "/m /nr:true", workingDirectory:_classlibPath);

            // WaitForExit() blocks even after dotnet.exe parent process exits.  
            RunProcess("dotnet.exe", "build /m /nr:true", workingDirectory: _classlibPath);
        }

        private static void RunProcess(string filename, string arguments, string workingDirectory = null)
        {
            var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                },
            };

            process.OutputDataReceived += (_, e) =>
            {
                Console.WriteLine($"  {e.Data}");
            };

            Console.WriteLine($"Starting process '{filename} {arguments}' in '{workingDirectory}'");

            process.Start();
            process.BeginOutputReadLine();

            Console.WriteLine("Waiting for process exit...");

            process.WaitForExit();

            // Workaround issue where WaitForExit() blocks until child processes are killed, which is problematic
            // for the dotnet.exe NodeReuse child processes.  I'm not sure why this is problematic for dotnet.exe child processes
            // but not for MSBuild.exe child processes.  The workaround is to specify a large timeout.
            // https://stackoverflow.com/a/37983587/102052
            // process.WaitForExit(int.MaxValue);

            Console.WriteLine("Process has exited");
            Console.WriteLine();
        }
    }
}
