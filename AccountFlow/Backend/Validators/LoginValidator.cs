using AccountFlow.Backend.Models;
using FluentValidation;

namespace AccountFlow.Backend.Validators
{
    // Validates user credentials during login, ensuring specific email domain format requirements are met
    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .ValidEmail();
        }
    }
}
