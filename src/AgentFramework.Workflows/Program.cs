using AgentFramework.Workflows.Agents;
using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Text;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;
var saunaMcpEndpoint = Environment.GetEnvironmentVariable("MIJNSAUNA_MCP") ?? string.Empty;
var thuisMcpEndpoint = Environment.GetEnvironmentVariable("MIJNTHUIS_MCP") ?? string.Empty;
var photoCarouselMcpEndpoint = Environment.GetEnvironmentVariable("PHOTOCAROUSEL_MCP") ?? string.Empty;
var tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? string.Empty;
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? string.Empty;
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? string.Empty;
var foundryUri = Environment.GetEnvironmentVariable("FOUNDRY_URI") ?? string.Empty;

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));

var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
string[] scopes = ["https://cognitiveservices.azure.com/.default"];
AccessToken token = credential.GetToken(new TokenRequestContext(scopes));
var persistentClient = new PersistentAgentsClient(foundryUri, credential);

var orchestratorAgent = OrchestratorAgent.Create(client);
var generalAgent = await GeneralAgent.Create(persistentClient, "asst_ycAatzTZRDE4NqetS6USQwsQ");
var timeAgent = TimeAgent.Create(client);
var saunaAgent = await MijnSaunaAgent.Create(client, saunaMcpEndpoint);
var carAgent = await MijnThuisCarAgent.Create(client, thuisMcpEndpoint);
var powerAgent = await MijnThuisPowerAgent.Create(client, thuisMcpEndpoint);
var solarAgent = await MijnThuisSolarAgent.Create(client, thuisMcpEndpoint);
var heatingAgent = await MijnThuisHeatingAgent.Create(client, thuisMcpEndpoint);
var photoCarouselAgent = await PhotoCarouselAgent.Create(client, photoCarouselMcpEndpoint);
var replyAgent = ReplyAgent.Create(client);

var workflow = AgentWorkflowBuilder
    .CreateHandoffBuilderWith(initialAgent: orchestratorAgent)
    .WithHandoffs(from: orchestratorAgent,
        to: [generalAgent, timeAgent, saunaAgent, carAgent, powerAgent, solarAgent, heatingAgent, photoCarouselAgent])
    .WithHandoff(from: generalAgent, to: orchestratorAgent)
    .WithHandoff(from: timeAgent, to: orchestratorAgent)
    .WithHandoff(from: saunaAgent, to: orchestratorAgent)
    .WithHandoff(from: carAgent, to: orchestratorAgent)
    .WithHandoff(from: powerAgent, to: orchestratorAgent)
    .WithHandoff(from: solarAgent, to: orchestratorAgent)
    .WithHandoff(from: heatingAgent, to: orchestratorAgent)
    .WithHandoff(from: photoCarouselAgent, to: orchestratorAgent)
    .Build();

var workflowAgent = workflow.AsAIAgent();

var chatSession = await workflowAgent.CreateSessionAsync();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    Console.ForegroundColor = ConsoleColor.White;
    var request = Console.ReadLine();

    var answerBuilder = new StringBuilder();

    //var answer = await workflowAgent.RunAsync(request!, chatSession);
    //Console.ForegroundColor = ConsoleColor.Cyan;
    //Console.Write("Assistant > ");
    //Console.ForegroundColor = ConsoleColor.White;
    //Console.WriteLine(answer.Text);
    //Console.ForegroundColor = ConsoleColor.Red;
    //Console.WriteLine("--------------------------------");

    //Console.WriteLine();
    //Console.WriteLine();
    //Console.WriteLine();

    await foreach (var response in workflowAgent.RunStreamingAsync(request!, chatSession))
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            answerBuilder.Append(response.Text);
        }
        else
        {
            foreach (var contents in response.Contents)
            {
                if (contents is UsageContent usageContent)
                {
                    // NOP
                }

                if (contents is FunctionCallContent functionCallContent)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{response.AuthorName}-FunctionCall > ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(functionCallContent.Name);
                    foreach (var argument in functionCallContent.Arguments)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"\t{argument.Key} > ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(argument.Value);
                    }
                }

                if (contents is FunctionResultContent functionResultContent)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"{response.AuthorName}-FunctionResult > ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(functionResultContent.Result);
                }
            }
        }
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Assistant > ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(answerBuilder);
}