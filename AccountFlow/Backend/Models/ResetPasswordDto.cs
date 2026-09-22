using System.ComponentModel.DataAnnotations;


namespace AccountFlow.Backend.Models
{
    public class ResetPasswordDto

    {
        [Required]
        public required string NewPassword { get; set; } = string.Empty;

        public required string Token { get; set; } = string.Empty;

    }

}
