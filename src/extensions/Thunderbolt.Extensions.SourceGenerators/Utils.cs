using System.Diagnostics;

namespace Thunderbolt.Extensions.SourceGenerators;

internal static class Utils
{
    internal static string RunExecutable(string exePath, string args)
    {
        ProcessStartInfo psi = new(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = args
        };
        Process exe = Process.Start(psi);
        string output = exe.StandardOutput.ReadToEnd();
        exe.WaitForExit();
        if (!exe.HasExited)
        { //just an overkill
            exe.Kill();
        }
        return output;
    }
    internal static TextReader RunExecutableReader(string exePath, string args)
    {
        ProcessStartInfo psi = new(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = args
        };
        Process exe = Process.Start(psi);
        try
        {
            return exe.StandardOutput;
        }
        finally
        {
            exe.WaitForExit();
            if (!exe.HasExited)
            { //just an overkill
                exe.Kill();
            }
        }
    }
}
