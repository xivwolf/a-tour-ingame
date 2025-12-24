using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TourAlert.Models;

public class DiscordMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("author_id")]
    public object AuthorId { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; } = new();

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("channel_name")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonPropertyName("channel_id")]
    public object ChannelId { get; set; } = new();

    [JsonPropertyName("role_mentions")]
    public List<object> RoleMentions { get; set; } = new();
}
