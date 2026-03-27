namespace AppBuilder.Web.Chat;

public sealed class ChatMessage
{
    public required string Role { get; init; } // "user" | "assistant"
    public required string Content { get; init; }
}

