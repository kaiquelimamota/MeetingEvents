using Newtonsoft.Json;

namespace MeetingEvents.Models;

public class CardTaskFetchValue<T>
{
    [JsonProperty("type")]
    public object Type { get; set; } = "task/fetch";

    [JsonProperty("data")]
    public T Data { get; set; }
}
