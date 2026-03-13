using MongoDB.Driver;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace MongoDBSamples.Services;

public class MongoDBVectorSearchMovieRecommendationService
    : MovieRecommendationServiceBase<MongoDBVectorSearchMovieRecommendationService>
{
    private readonly IMongoCollection<Movie> _moviesCollection;
    private readonly EmbeddingClient _embeddingClient;

    public MongoDBVectorSearchMovieRecommendationService(
        IMongoCollection<Movie> moviesCollection,
        ChatClient chatClient,
        EmbeddingClient embeddingClient,
        ILogger<MongoDBVectorSearchMovieRecommendationService> logger
    )
        : base(chatClient, logger)
    {
        _moviesCollection = moviesCollection;
        _embeddingClient = embeddingClient;
    }

    public override string Name => "MongoDB Vector Search";

    protected override async Task<List<Movie>> RetrieveMoviesAsync(
        string query,
        CancellationToken ct
    )
    {
        OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(query);
        QueryVector queryVector = new(embedding.ToFloats());

        VectorSearchOptions<Movie>? vectorSearchOptions = new()
        {
            IndexName = "default",
            NumberOfCandidates = 100,
        };

        return await _moviesCollection
            .Aggregate()
            .VectorSearch(x => x.PlotEmbedding, queryVector, 10, vectorSearchOptions)
            .ToListAsync(cancellationToken: ct);
    }
}
