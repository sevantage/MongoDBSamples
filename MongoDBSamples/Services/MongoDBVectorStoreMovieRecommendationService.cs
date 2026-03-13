using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using MongoDB.Driver;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace MongoDBSamples.Services;

public class MongoDBVectorStoreMovieRecommendationService
    : MovieRecommendationServiceBase<MongoDBVectorStoreMovieRecommendationService>
{
    private readonly VectorStoreCollection<string, Movie> _coll;
    private readonly EmbeddingClient _embeddingClient;

    public MongoDBVectorStoreMovieRecommendationService(
        IMongoDatabase database,
        ChatClient chatClient,
        EmbeddingClient embeddingClient,
        ILogger<MongoDBVectorStoreMovieRecommendationService> logger
    )
        : base(chatClient, logger)
    {
        VectorStore vectorStore = new MongoVectorStore(database);
        _coll = vectorStore.GetCollection<string, Movie>("embedded_movies");
        _embeddingClient = embeddingClient;
    }

    public override string Name => "MongoDB Vector Store Connector";

    protected override async Task<List<Movie>> RetrieveMoviesAsync(
        string query,
        CancellationToken ct
    )
    {
        OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(query);
        return await _coll
            .SearchAsync(new ReadOnlyMemory<float>(embedding.ToFloats().ToArray()), 10)
            .Select(m => m.Record)
            .ToListAsync();
    }
}
