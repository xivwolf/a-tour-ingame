# Tour Notify Server 接口規格文件 (API & WebSocket)

此服務提供一個 HTTP 接口用於發送廣播，以及一個 WebSocket 接口用於即時接收訊息。

## 1. 資料模型 (Data Model)

所有的通訊均使用 JSON 格式。以下是訊息的標準結構：

| 欄位名稱 | 型別 | 說明 |
| :--- | :--- | :--- |
| `content` | String | 訊息本文 |
| `author` | String | 傳送者名稱 (e.g., `User#1234`) |
| `author_id` | Any (String/Int) | 傳送者的唯一識別碼 |
| `timestamp` | String | 訊息產生的時間戳 (ISO 8601) |
| `attachments` | Array[String] | 附件的 URL 列表 |
| `category` | String | 頻道分類名稱 |
| `channel_name` | String | 頻道名稱 |
| `channel_id` | Any (String/Int) | 頻道的唯一識別碼 |
| `role_mentions` | Array[Any] | 被標記的角色 ID 或名稱列表 |

**JSON 範例：**
```json
{
  "content": "Hello World!",
  "author": "BoringWolf#1234",
  "author_id": "123456789012345678",
  "timestamp": "2025-12-24T23:22:55Z",
  "attachments": ["https://example.com/image.png"],
  "category": "General",
  "channel_name": "lobby",
  "channel_id": "987654321098765432",
  "role_mentions": ["Admin", "Moderator"]
}
```

---

## 2. HTTP API (廣播發送)

用於從外部系統（如 Discord Bot）將訊息送入 Server。

- **URL:** `http://localhost:8080/broadcast`
- **Method:** `POST`
- **Content-Type:** `application/json`
- **Payload:** 詳見上述 [資料模型](#1-資料模型-data-model)。
- **Response:**
    - `200 OK`: 訊息已成功進入廣播隊列。
    - `400 Bad Request`: JSON 格式錯誤或欄位缺失。

---

## 3. WebSocket API (即時接收)

客戶端應連線至此端點以監聽所有廣播訊息。

- **URL:** `ws://localhost:8080/ws`
- **協定行為:**
    - **連線 (Connection):** 連線成功後，客戶端即進入監聽狀態。
    - **接收 (Receive):** 當任何來源透過 HTTP POST 發送訊息時，Server 會將該 JSON 原始物件直接轉發給所有已連線的 WebSocket 客戶端。
    - **方向:** 單向 (Server -> Client)。目前客戶端傳送給 Server 的訊息會被忽略。

---

## 4. 客戶端實作建議 (C# 範例)

```csharp
public class DiscordMessage
{
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; }

    [JsonPropertyName("author_id")]
    public object AuthorId { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }

    [JsonPropertyName("attachments")]
    public List<string> Attachments { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("channel_name")]
    public string ChannelName { get; set; }

    [JsonPropertyName("channel_id")]
    public object ChannelId { get; set; }

    [JsonPropertyName("role_mentions")]
    public List<object> RoleMentions { get; set; }
}
```