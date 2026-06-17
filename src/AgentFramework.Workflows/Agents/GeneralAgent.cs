using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework.Workflows.Agents;

internal static class GeneralAgent
{
    private const string NAME = nameof(GeneralAgent);
    private const string DESCRIPTION = nameof(GeneralAgent);
    private const string INSTRUCTIONS = @"
        You should answer all general questions.
        You should handoff the answer back to the OrchestratorAgent.";

    public static async Task<AIAgent> Create(PersistentAgentsClient client, string agentId)
    {
        var agentClient = await client.GetAIAgentAsync(agentId, new ChatClientAgentOptions
        {
            Name = NAME,
            Description = DESCRIPTION,
            ChatOptions = new ChatOptions
            {
                Instructions = INSTRUCTIONS
            }
        });

        return agentClient;
    }
}