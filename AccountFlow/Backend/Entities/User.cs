using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace AccountFlow.Backend.Entities
{
    public class User
    {
        [BsonId]
        [BsonElement("_id"), BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("user_name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("user_surname")]
        public string Surname { get; set; } = string.Empty;

        [BsonElement("user_email")]
        public string Email { get; set; } = string.Empty;

        // Sensitive fields are never serialized to API responses (defense-in-depth).
        [JsonIgnore]
        [BsonElement("user_hashed_password")]
        public string HashedPassword { get; set; } = string.Empty;

        // Password Reset Logic
        [JsonIgnore]
        public string? HashedResetCode { get; set; }
        public DateTime? ResetCodeExpiry { get; set; }

        // Email Verification Logic
        public bool IsVerified { get; set; } = false;
        [JsonIgnore]
        public string? HashedVerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }
        public DateTime? LastVerificationEmailSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Role-Based Access Control (Default: "User")
        public string Role { get; set; } = "User";

        // JWT Refresh Token Mechanism (stored hashed, never serialized)
        [JsonIgnore]
        public string? HashedRefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }

        // Incremented on password reset so previously issued access tokens are rejected.
        [JsonIgnore]
        public long SessionVersion { get; set; }
    }
}
