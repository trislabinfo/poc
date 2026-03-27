using Microsoft.Extensions.Options;
using AppBuilder.Web.Chat;

namespace AppBuilder.Web;

public sealed class AppBuilderChatService : IAppBuilderChatService
{
    private readonly AppBuilderWebOptions _options;
    private readonly OpenAiChatWithToolsService _openAiChat;
    private readonly AnthropicChatWithToolsService _anthropicChat;

    public AppBuilderChatService(
        IOptions<AppBuilderWebOptions> options,
        OpenAiChatWithToolsService openAiChat,
        AnthropicChatWithToolsService anthropicChat)
    {
        _options = options.Value;
        _openAiChat = openAiChat;
        _anthropicChat = anthropicChat;
    }

    public Task<string> SendMessageAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (history is null) throw new ArgumentNullException(nameof(history));
        if (userMessage is null) throw new ArgumentNullException(nameof(userMessage));

        var provider = _options.Llm.Provider?.Trim();
        if (string.IsNullOrWhiteSpace(provider))
            provider = "OpenAI";

        var conversation = history.Concat(new[] { new ChatMessage { Role = "user", Content = userMessage } }).ToList();

        return provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
            ? _openAiChat.ChatAsync(conversation, cancellationToken)
            : provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase)
                ? _anthropicChat.ChatAsync(conversation, cancellationToken)
                : throw new InvalidOperationException($"Unsupported LLM provider: {provider}");
    }
}

