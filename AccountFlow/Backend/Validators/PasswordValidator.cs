using AccountFlow.Backend.Models;
using FluentValidation;

namespace AccountFlow.Backend.Validators
{
    // Enforces security complexity requirements for new passwords, including length, casing, and special characters
    public class PasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        public PasswordValidator()
        {
            RuleFor(x => x.NewPassword)
                .StrongPassword();
        }
    }
}
