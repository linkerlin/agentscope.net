using AgentScope.Core; 
using AgentScope.Lab;
using DotNetEnv;
  
{
    Console.WriteLine($"HELLO!");
    await Executor();
}

async Task Executor()
{
    var demo = new WorkspaceDemo();
    await demo.ChatStream();
    Console.ReadKey();
}
