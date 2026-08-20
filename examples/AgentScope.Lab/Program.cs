using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;
using AgentScope.Lab;
using DotNetEnv;
  
{
    Console.WriteLine($"HELLO!");
    await Executor();
}

async Task Executor()
{
    var demo = new ToolDemo();
    await  demo.Tool_chat_streaming();  
}
