using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AccountFlow.Backend.Models
{
    public class RegisterDto
    {

        public required string Name { get; set; } = string.Empty;

        public required string Surname { get; set; } = string.Empty;

        public required string Email { get; set; } = string.Empty;

        public required string Password { get; set; } = string.Empty;
    }

}
