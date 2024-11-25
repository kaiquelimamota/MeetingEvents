using Microsoft.Bot.Schema.Teams;

namespace MeetingEvents.Models;

public static class TaskModuleResponseFactory
{
    public static TaskModuleResponse CreateResponse(string message)
    {
        return new TaskModuleResponse
        {
            Task = new TaskModuleMessageResponse()
            {
                Value = message,
            },
        };
    }

    public static TaskModuleResponse CreateResponse(TaskModuleTaskInfo taskInfo)
    {
        return new TaskModuleResponse
        {
            Task = new TaskModuleContinueResponse()
            {
                Value = taskInfo,
            },
        };
    }

    public static TaskModuleResponse ToTaskModuleResponse(this TaskModuleTaskInfo taskInfo)
    {
        return CreateResponse(taskInfo);
    }
}