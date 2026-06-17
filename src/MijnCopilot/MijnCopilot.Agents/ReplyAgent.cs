using Microsoft.Extensions.Configuration;
using MijnCopilot.Agents.Base;

namespace MijnCopilot.Agents;

internal class ReplyAgentFactory : AgentFactoryBase
{
    private string _description = "An agent that summarizes all replies to a question into a single reply";
    private string _instructions = @"
You are an agent that replies to one or more questions from the user and are fed the replies to these questions coming from other agents into the converstation history.
You should summarize a single reply from a chat history and combine all the replies in a single sentence or paragraph to capture all information.
You should not ask follow-up question, unless you need additional information from the user. Direct these requests for more information to the user directly.
";

    protected override string AgentName => "ReplyAgent";
    public override string AgentDescription => _description;
    protected override string AgentInstruction => _instructions;

    public ReplyAgentFactory(IConfiguration configuration) : base(configuration) { }
}