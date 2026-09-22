using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AccountFlow.Backend.Models
{
    public class ResetPasswordRequestDto
    {
        [Required]
        public required string Email { get; set; } = string.Empty;

    }

}
