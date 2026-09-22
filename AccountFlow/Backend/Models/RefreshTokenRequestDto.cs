using MongoDB.Bson;

namespace AccountFlow.Backend.Models
{
    public class RefreshTokenRequestDto
    {
        public required string RefreshToken { get; set; }
    }
}
