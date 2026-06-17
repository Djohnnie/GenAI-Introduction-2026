using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentFramework.Workflows.Agents;

internal static class TimeAgent
{
    private const string NAME = nameof(TimeAgent);
    private const string DESCRIPTION = nameof(TimeAgent);
    private const string INSTRUCTIONS = @"
        You should reply to questions related to the current date and time.
        NEVER answer any other questions!
        You should handoff back to the OrchestratorAgent if the user wants anything else,
        if not, present a summary to the user.";
    private const string MODEL = "gpt-4o";

    public static AIAgent Create(AzureOpenAIClient client)
    {
        var chatClient = client.GetChatClient(MODEL).AsIChatClient();
        var agentClient = new ChatClientAgent(chatClient, NAME, DESCRIPTION, INSTRUCTIONS, GetTools());
        return agentClient;
    }

    private static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetTime),
            AIFunctionFactory.Create(GetDate),
        ];
    }

    [Description("Gets the current time.")]
    private static TimeSpan GetTime()
    {
        return TimeProvider.System.GetLocalNow().TimeOfDay;
    }

    [Description("Gets the current date.")]
    private static DateTime GetDate()
    {
        return TimeProvider.System.GetLocalNow().Date;
    }
}