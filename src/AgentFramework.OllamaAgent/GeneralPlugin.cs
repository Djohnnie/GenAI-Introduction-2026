using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace AgentFramework.OllamaAgent;

public class GeneralPlugin
{
    public static IList<AITool> GetTools()
    {
        return [
            AIFunctionFactory.Create(GetTime),
            AIFunctionFactory.Create(GetDate),
        ];
    }

    [Description("Gets the current time.")]
    public static TimeSpan GetTime()
    {
        return TimeProvider.System.GetLocalNow().TimeOfDay;
    }

    [Description("Gets the current date.")]
    public static DateTime GetDate()
    {
        return TimeProvider.System.GetLocalNow().Date;
    }
}