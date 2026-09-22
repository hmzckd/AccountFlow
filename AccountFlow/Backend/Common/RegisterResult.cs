using AccountFlow.Backend.Entities;

namespace AccountFlow.Backend.Common
{
    // Distinguishes the reasons a registration can fail, so the controller no longer has to
    // guess (and mislabel every null as "email already exists").
    public enum RegisterStatus
    {
        Success,
        EmailAlreadyExists,
        EmailSendFailed
    }

    public record RegisterResult(RegisterStatus Status, User? User = null);
}
