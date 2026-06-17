using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace AgentFramework.Workflows.Agents;

internal static class MijnThuisPowerAgent
{
    private const string NAME = nameof(MijnThuisPowerAgent);
    private const string DESCRIPTION = nameof(MijnThuisPowerAgent);
    private const string INSTRUCTIONS = @"
        You should answer questions and receive commands regarding my power usage:
        - Power usage and peak power this month.
        - Energy usage today and this month.
        - Energy cost today and this month.
        - Current energy consumption and injection price.
        Adhere to the following rules:
        - Just use plain text, no markdown or any other formatting.
        - Separate every sentence with a [BR] as custom newline.
        - Only answer questions and execute commands that are related to my power usage.
        - Every request should result in a tool-call.
        - If you don't know the answer, say you don't know or can't help with that.
        - Never ask follow-up questions. If you can only answer part of the question, do so.
        ";
    private const string MCP_NAME = "MijnThuisMcpClient";
    private const string MCP_TOOL_PREFIX = "mijnthuis_power";

    private const string MODEL = "gpt-4o";

    public static async Task<AIAgent> Create(AzureOpenAIClient client, string mcpEndpoint)
    {
        var _mcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(new()
            {
                Name = MCP_NAME,
                Endpoint = new Uri(mcpEndpoint)
            }));

        var tools = await _mcpClient.ListToolsAsync();
        tools = tools.Where(x => string.IsNullOrEmpty(MCP_TOOL_PREFIX) || x.Name.StartsWith(MCP_TOOL_PREFIX)).ToList();

        var chatClient = client.GetChatClient(MODEL).AsIChatClient();
        var agentClient = new ChatClientAgent(chatClient, NAME, DESCRIPTION, INSTRUCTIONS, tools.Cast<AITool>().ToList());
        return agentClient;
    }
}