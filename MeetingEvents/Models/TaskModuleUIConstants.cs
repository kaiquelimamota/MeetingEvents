using System.Runtime;

namespace MeetingEvents.Models;

public static class TaskModuleUIConstants
{
    public static UISettings CustomForm { get; set; } =
        new UISettings(510, 450, "Custom Form", TaskModuleIds.CustomForm, "Custom Form");
    public static UISettings AdaptiveCard { get; set; } =
        new UISettings(450, 450, "Adaptive Card: Inputs", TaskModuleIds.AdaptiveCard, "Identificar");
}
