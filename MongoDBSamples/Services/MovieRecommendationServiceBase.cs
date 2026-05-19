using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MongoDB.Driver;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace MongoDBSamples.Services;

public abstract class MovieRecommendationServiceBase<T> : IMovieRecommendationService
{
    protected readonly AIAgent _agent;
    protected AgentSession? _session;
    protected readonly ILogger<T> _logger;
    protected readonly List<Microsoft.Extensions.AI.ChatMessage> _chatHistory =
    [
        new Microsoft.Extensions.AI.ChatMessage(
            ChatRole.Assistant,
            "Hello! I'm your movie recommendation agent. What kind of movies do you like?"
        ),
    ];

    public abstract string Name { get; }

    public MovieRecommendationServiceBase(ChatClient chatClient, ILogger<T> logger)
    {
        _logger = logger;

        TextSearchProviderOptions textSearchOptions = new()
        {
            SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling,
        };

        _agent = chatClient.AsAIAgent(
            new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = """
                    You are a conversational and enthusiastic movie recommendation agent. Your goal is to help users find films based on their preferences.

                    **Core Guidelines:**
                    1. **Tool Usage:** You have access to the `TextSearch` tool. You must use this tool to verify plots, cast, genres, and release dates before answering. Do not rely on general training data for specific movie details.
                    2. **Citations:** When providing movie details, include a source link in your answer. Use ONLY the relative links found in the tool search results (e.g., [Link Text](/movies/123)). Do not invent or link to external URLs.
                    3. **Accuracy:** If the search tool returns no results for a specific query, do not hallucinate. State clearly that the movie or details are not found in your database.
                    4. **Conciseness & Tone:** Keep responses concise (maximum 2-3 sentences) but maintain an energetic, helpful tone. Use Markdown formatting (bolding, lists) to improve readability.
                    5. **Scope:** You specialize in movies. If a user asks about non-movie topics, politely redirect the conversation back to film recommendations.

                    **Response Format:**
                    - Use Markdown.
                    - Prioritize tool-verified information.
                    - Ensure all links are relative to the current application.
                    """,
                },
                AIContextProviders = [new TextSearchProvider(RetrieveContextAsync, textSearchOptions)]
            }
        );
    }

    public IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> GetChatHistory() =>
        _chatHistory.AsReadOnly();

    public async Task<AgentResponse> RecommendAsync(
        string userInput,
        CancellationToken cancellationToken
    )
    {
        _chatHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userInput));
        AgentSession session = await GetSessionAsync();
        Microsoft.Extensions.AI.ChatMessage message = new(ChatRole.User, userInput);
        AgentResponse response = await _agent.RunAsync(message, session, null, cancellationToken);
        _chatHistory.Add(
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, response.ToString())
        );
        return response;
    }

    protected async Task<AgentSession> GetSessionAsync()
    {
        _session ??= await _agent.CreateSessionAsync();
        return _session;
    }

    private async Task<IEnumerable<TextSearchProvider.TextSearchResult>> RetrieveContextAsync(
        string query,
        CancellationToken ct
    )
    {
        _logger.LogInformation(
            "Retrieving context using method {Method} with query: {Query}",
            Name,
            query
        );

        List<Movie> movies = await RetrieveMoviesAsync(query, ct);

        _logger.LogInformation(
            "Retrieved {Count} movies for query {Query} with method {Method}",
            movies.Count,
            query,
            Name
        );

        return movies.Select(m => MapToSearchResult(m));
    }

    private static TextSearchProvider.TextSearchResult MapToSearchResult(Movie m)
    {
        var cast = m.Cast ?? new List<string>();
        var genres = m.Genres ?? new List<string>();

        return new TextSearchProvider.TextSearchResult
        {
            SourceName = m.Title,
            SourceLink = $"/movies/{m.Id}",
            Text =
                $@"""
                Title: {m.Title}
                Plot: {m.Plot}
                Full plot: {m.FullPlot}
                Cast: {string.Join(", ", cast)}
                Genres: {string.Join(", ", genres)}
                """,
        };
    }

    protected abstract Task<List<Movie>> RetrieveMoviesAsync(string query, CancellationToken ct);
}
