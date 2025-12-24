using System.Collections.Generic;
using Newtonsoft.Json;

namespace TourAlert.Models;

public class RoleMention
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public string IdString => Id;
}

public class DiscordMessage
{
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    [JsonProperty("author_id")]
    public object AuthorId { get; set; } = new();

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonProperty("attachments")]
    public List<string> Attachments { get; set; } = new();

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    [JsonProperty("channel_name")]
    public string ChannelName { get; set; } = string.Empty;

    [JsonProperty("channel_id")]
    public object ChannelId { get; set; } = new();

    [JsonProperty("role_mentions")]
    public List<RoleMention> RoleMentions { get; set; } = new();
}
