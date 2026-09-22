namespace AccountFlow.Backend.Models
{
    public class EmailDto
    {
        public required string To { get; set; } = string.Empty;
        public required string Subject { get; set; } = string.Empty;
        public required string Body { get; set; } = string.Empty;
    }
}
