namespace MeetingEvents.Models;

public class UISettings
{
    public UISettings(int width, int height, string title, string id, string buttonTitle)
    {
        Width = width;
        Height = height;
        Title = title;
        Id = id;
        ButtonTitle = buttonTitle;
    }

    public int Height { get; set; }
    public int Width { get; set; }
    public string Title { get; set; }
    public string ButtonTitle { get; set; }
    public string Id { get; set; }
}