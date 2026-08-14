using AgentScope.Core.Agent;
namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>任务仓库接口，对�?Java TaskRepository</summary>
public interface ITaskRepository
{
    BackgroundTask? GetTask(RuntimeContext? rc, string sessionId, string taskId);
    BackgroundTask PutTask(RuntimeContext? rc, string taskId, string subAgentId,
        string sessionId, TaskRunSpec spec);
    ICollection<BackgroundTask> ListTasks(RuntimeContext? rc,
        string sessionId, TaskStatus? filter = null);
    bool CancelTask(RuntimeContext? rc, string sessionId, string taskId);
}

