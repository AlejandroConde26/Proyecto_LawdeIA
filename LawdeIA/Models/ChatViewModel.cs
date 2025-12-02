using LawdeIA.Models;

public class ChatViewModel
{
    public int CurrentConversationId { get; set; }
    public List<Conversation> UserConversations { get; set; } = new();
    public List<Message> CurrentMessages { get; set; } = new();
    public string NewMessage { get; set; }
    public string AIProvider { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; }
    public int? ConversationId { get; set; }
}

public class ChatResponse
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public MessageData Message { get; set; }
    public int ConversationId { get; set; }
    public string ConversationTitle { get; set; }
}

public class MessageData
{
    public int MessageId { get; set; }
    public string Content { get; set; }
    public string SenderType { get; set; }
    public string Timestamp { get; set; }
}