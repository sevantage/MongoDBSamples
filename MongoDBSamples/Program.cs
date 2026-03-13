using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDBSamples;
using MongoDBSamples.Components;
using MongoDBSamples.Services;
using OpenAI.Chat;
using OpenAI.Embeddings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

Configuration cfg = builder.Configuration.Get<Configuration>()!;

// Set up MongoDB connection instances
builder.Services.AddSingleton<IMongoClient>(prov =>
{
    MongoClientSettings settings = MongoClientSettings.FromConnectionString(
        cfg.MongoDB.ConnectionString
    );
    return new MongoClient(settings);
});
builder.Services.AddSingleton(prov =>
{
    IMongoClient client = prov.GetRequiredService<IMongoClient>();
    return client.GetDatabase(cfg.MongoDB.Database);
});
builder.Services.AddSingleton(prov =>
{
    IMongoDatabase db = prov.GetRequiredService<IMongoDatabase>();
    IMongoCollection<Movie> coll = db.GetCollection<Movie>("embedded_movies");
    // Create search index if it doesn't exist.
    // In production, you would typically manage indexes separately and not create them at runtime like this.
    if (!coll.SearchIndexes.List("default").Any())
    {
        CreateVectorSearchIndexModel<Movie> model = new(
            x => x.PlotEmbedding,
            "default",
            VectorSimilarity.Euclidean,
            1536
        );
        coll.SearchIndexes.CreateOne(model);
        prov.GetRequiredService<ILogger<Program>>()
            .LogInformation(
                "Created MongoDB vector search index on collection {CollectionName}",
                coll.CollectionNamespace.CollectionName
            );
    }
    return coll;
});
builder.Services.AddSingleton<MovieRepository>();

// Set up Azure OpenAI client and related services
builder.Services.AddSingleton<AzureOpenAIClient>(prov =>
    new(new Uri(cfg.Endpoint), new DefaultAzureCredential())
);
builder.Services.AddSingleton<ChatClient>(prov =>
    prov.GetRequiredService<AzureOpenAIClient>().GetChatClient(cfg.ChatDeployment)
);
builder.Services.AddSingleton<EmbeddingClient>(prov =>
    prov.GetRequiredService<AzureOpenAIClient>().GetEmbeddingClient(cfg.EmbeddingDeployment)
);

// Register keyed implementations of Movie Recommendation Services
builder.Services.AddKeyedScoped<
    IMovieRecommendationService,
    MongoDBVectorSearchMovieRecommendationService
>("mongodb-vector-search-connector");
builder.Services.AddKeyedScoped<
    IMovieRecommendationService,
    MongoDBVectorStoreMovieRecommendationService
>("mongodb-vector-store-connector");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
