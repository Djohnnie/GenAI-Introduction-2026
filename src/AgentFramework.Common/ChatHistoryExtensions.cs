using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace AgentFramework.Common;

public static class ChatHistoryExtensions
{
    public static void Debug(this AgentSession chatThread)
    {
        if (chatThread.StateBag.TryGetValue<InMemoryChatHistoryProvider.State>(nameof(InMemoryChatHistoryProvider), out var inMemoryState))
        {
            foreach (var message in inMemoryState.Messages)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[{message.Role}] ");

                if (message.Role == ChatRole.Assistant && message.Contents[0] is FunctionCallContent functionCall)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{functionCall.CallId} --> {functionCall.Name}");
                }
                else if (message.Role == ChatRole.Tool && message.Contents[0] is FunctionResultContent functionResult)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{functionResult.CallId} --> {functionResult.Result.ToString()}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{message.Text.Trim()}");
                }
            }
        }

        //    if (chatThread.StateBag.TryGetValue<WorkflowChatHistoryProvider>(nameof(InMemoryChatHistoryProvider), out var inMemoryState))
        //    {
        //        foreach (var message in inMemoryState.Messages)
        //        {
        //            Console.ForegroundColor = ConsoleColor.Red;
        //            Console.Write($"[{message.Role}] ");

        //            if (message.Role == ChatRole.Tool && message.Contents[0] is FunctionResultContent functionResult)
        //            {
        //                Console.ForegroundColor = ConsoleColor.White;
        //                Console.WriteLine($"{functionResult.CallId} --> {functionResult.Result.ToString()}");
        //            }
        //            else
        //            {
        //                Console.ForegroundColor = ConsoleColor.White;
        //                Console.WriteLine($"{message.Text.Trim()}");
        //            }
        //        }
        //    }
    }
}