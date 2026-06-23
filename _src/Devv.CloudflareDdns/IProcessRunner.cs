namespace Devv.CloudflareDdns;

public interface IProcessRunner
{
    Task RunAsync(string command, CancellationToken cancellationToken);
}
