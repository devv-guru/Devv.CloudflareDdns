namespace Devv.CloudflareDdns;

public interface ICloudflareDnsChallengeService
{
    Task<string> CreateTxtRecordAsync(
        string zoneId,
        string name,
        string value,
        CancellationToken cancellationToken);

    Task DeleteTxtRecordAsync(
        string zoneId,
        string recordId,
        CancellationToken cancellationToken);
}
