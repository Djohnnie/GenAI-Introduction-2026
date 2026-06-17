using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelDemo.Planners;
using System.Text.Json;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4o", endpoint, key);

builder.Plugins.AddFromType<MathFunctions>();

var kernel = builder.Build();

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

ChatHistory history = [];

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var serializedPlan = string.Empty;

if (!File.Exists("plan.json"))
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    Console.ForegroundColor = ConsoleColor.White;
    var request = Console.ReadLine();
    history.AddUserMessage(request!);

    // Build the plan.
    var result = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings, kernel);

    serializedPlan = JsonSerializer.Serialize(history);
}
else
{
    serializedPlan = File.ReadAllText("plan.json");
}

File.WriteAllText("plan.json", serializedPlan);

var deserializedPlan = JsonSerializer.Deserialize<ChatHistory>(serializedPlan);

var response = await chatCompletionService.GetChatMessageContentAsync(deserializedPlan, executionSettings, kernel);

string fullMessage = "";
Console.ForegroundColor = ConsoleColor.Cyan;

Console.Write(response.Content);