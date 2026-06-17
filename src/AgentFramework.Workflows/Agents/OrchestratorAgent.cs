using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework.Workflows.Agents;

internal static class OrchestratorAgent
{
    private const string NAME = nameof(OrchestratorAgent);
    private const string DESCRIPTION = nameof(OrchestratorAgent);
    private const string INSTRUCTIONS = @"
        You are a triage agent that redirects questions to the correct agent.
        Your ONLY job is to analyse the conversation and hand off to the right specialist agent.
        You do not answer questions or ask additional questions.
        Do NOT re-route to an agent that has already finished its work.

        Handoff rules:
        - Unaswered questions regarding current date and time -> handoff to TimeAgent.
        - Unanswered questions regarding my sauna -> handoff to MijnSaunaAgent.
        - Unanswered questions regarding my car -> handoff to MijnThuisCarAgent.
        - Unanswered questions regarding my solar installation -> handoff to MijnThuisSolarAgent.
        - Unanswered questions regarding my heating -> handoff to MijnThuisHeatingAgent.
        - Unanswered questions regarding photos that are currently showed -> handoff to PhotoCarouselAgent.
        - Any remaining general questions -> handoff to GeneralAgent.

        If there are no remaining pending questions, return a summary of the answers to the user.";
    private const string MODEL = "gpt-4o";

    public static AIAgent Create(AzureOpenAIClient client)
    {
        var chatClient = client.GetChatClient(MODEL).AsIChatClient();
        var agentClient = new ChatClientAgent(chatClient, NAME, DESCRIPTION, INSTRUCTIONS);
        return agentClient;
    }
}