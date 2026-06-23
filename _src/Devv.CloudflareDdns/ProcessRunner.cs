using System.Diagnostics;

namespace Devv.CloudflareDdns;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task RunAsync(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            ArgumentList = { "-c", command },
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start reload command");

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
            return;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        throw new InvalidOperationException(
            $"Reload command failed with exit code {process.ExitCode}. Output: {output}. Error: {error}");
    }
}
