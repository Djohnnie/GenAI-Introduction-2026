using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace AgentFramework.Workflows.Agents;

internal static class MijnThuisHeatingAgent
{
    private const string NAME = nameof(MijnThuisHeatingAgent);
    private const string DESCRIPTION = nameof(MijnThuisHeatingAgent);
    private const string INSTRUCTIONS = @"
        You should answer questions and receive commands regarding my heating installation:
        - Current temperature in my home.
        - Current temperature outside.
        - Current setpoint temperature and next setpoint temperature.
        - Next setpoint change time.
        Adhere to the following rules:
        - Just use plain text, no markdown or any other formatting.
        - Only answer questions and execute commands that are related to my heating installation.
        - Every request should result in a function/tool-call.
        - If you don't know the answer, say you don't know or can't help with that.
        - Never ask follow-up questions. If you can only answer part of the question, do so.
        ";
    private const string MCP_NAME = "MijnThuisMcpClient";
    private const string MCP_TOOL_PREFIX = "mijnthuis_heating";

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