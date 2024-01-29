using MongoDB.Driver;

namespace CdpDotnetNetworkChecker.Data;

public interface IMongoDbClientFactory
{
    protected IMongoClient CreateClient();

    IMongoCollection<T> GetCollection<T>(string collection);
}