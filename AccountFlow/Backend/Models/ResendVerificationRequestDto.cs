namespace AccountFlow.Backend.Models;

public class ResendVerificationRequestDto
{
    public required string Email { get; set; } = string.Empty;
}
