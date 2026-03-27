using AppBuilder.Web.Chat;

namespace AppBuilder.Web;

public interface IAppBuilderChatService
{
    /// <summary>Runs a chat turn. The returned value is the assistant's final message (after any tool calls).</summary>
    Task<string> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, CancellationToken cancellationToken = default);
}

