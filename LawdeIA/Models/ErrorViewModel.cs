public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? Message { get; set; } // ✅ Añade esta propiedad

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}