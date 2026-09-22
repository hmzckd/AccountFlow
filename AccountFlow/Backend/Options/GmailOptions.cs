namespace AccountFlow.Backend.Options
{
    public class GmailOptions
    {
        public const string GmailOptionsKey = "GmailOptions";
        public string Host { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
