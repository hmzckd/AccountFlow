using AccountFlow.Backend.Models;
using FluentValidation;

namespace AccountFlow.Backend.Validators
{
    // Validates the email input for password reset requests
    public class EmailValidator : AbstractValidator<ResetPasswordRequestDto>
    {
        public EmailValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }
}