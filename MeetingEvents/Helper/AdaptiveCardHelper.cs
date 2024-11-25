using AdaptiveCards;
using Microsoft.Bot.Schema;
using System;
using System.IO;

namespace MeetingEvents.Helper;

public static class AdaptiveCardHelper
{
    public static Attachment GetAdaptiveCard()
    {
        // Parse the JSON 
        AdaptiveCardParseResult result = AdaptiveCard.FromJson(GetAdaptiveCardJson());

        return new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = result.Card
        };
    }
    public static String GetAdaptiveCardJson()
    {
        var path = Path.Combine(".", "Resources", "AdaptiveCard_TaskModule.json");
        return File.ReadAllText(path);
    }

}