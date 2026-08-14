namespace AgentScope.Core.Agent;

/// <summary>任务提醒中间件：确保模型不会忘记关键上下文，对应 Java TaskReminderMiddleware</summary>
public sealed class TaskReminderMiddleware : MiddlewareBase
{
    private readonly string _reminder;

    public TaskReminderMiddleware(string reminder = "请记住当前任务目标")
    {
        _reminder = reminder;
    }

    public override Task<string> OnSystemPromptAsync(IAgent agent, RuntimeContext ctx, string prompt)
    {
        return Task.FromResult($"{prompt}\n\n[系统提醒]: {_reminder}");
    }
}
