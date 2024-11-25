using Newtonsoft.Json;

namespace MeetingEvents.Models;
public class AdaptiveCardTaskFetchValue<T>
{
    [JsonProperty("msteams")]
    public object Type { get; set; } = JsonConvert.DeserializeObject("{\"type\": \"task/fetch\" }");

    [JsonProperty("data")]
    public T Data { get; set; }
}
