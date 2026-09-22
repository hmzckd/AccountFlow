using AccountFlow.Backend.Models;
using FluentValidation;

namespace AccountFlow.Backend.Validators
{
    // Validates user registration data, enforcing rules for name format, email domain validity, and password complexity
    public class RegisterValidator : AbstractValidator<RegisterDto>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(50);

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Surname is required.")
                .MaximumLength(50);

            RuleFor(x => x.Email)
                .ValidEmail();

            RuleFor(x => x.Password)
                .StrongPassword();
        }
    }
}
