using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework.Workflows.Agents;

internal static class ReplyAgent
{
    private const string NAME = nameof(ReplyAgent);
    private const string DESCRIPTION = nameof(ReplyAgent);
    private const string INSTRUCTIONS = "You should combine multiple answers into a single reply.";
    private const string MODEL = "gpt-4o";

    public static AIAgent Create(AzureOpenAIClient client)
    {
        var chatClient = client.GetChatClient(MODEL).AsIChatClient();
        var agentClient = new ChatClientAgent(chatClient, NAME, DESCRIPTION, INSTRUCTIONS);
        return agentClient;
    }
}