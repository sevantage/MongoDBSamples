using Microsoft.Agents.AI;

namespace MongoDBSamples.Services;

public interface IMovieRecommendationService
{
    string Name { get; }
    IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> GetChatHistory();
    Task<AgentResponse> RecommendAsync(string userInput, CancellationToken cancellationToken);
}
