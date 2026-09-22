namespace AccountFlow.Backend.Models
{
    public sealed class AdminStatsDto
    {
        public long Registrations { get; init; }
        public long Unverified { get; init; }
    }
}
