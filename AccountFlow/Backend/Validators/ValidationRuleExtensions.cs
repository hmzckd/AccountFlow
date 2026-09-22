using FluentValidation;

namespace AccountFlow.Backend.Validators
{
    internal static class ValidationRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> ValidEmail<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .Must(HasValidDomain)
                .WithMessage("Please enter a valid email domain (e.g. example.com).");
        }

        public static IRuleBuilderOptions<T, string> StrongPassword<T>(
            this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
                .Matches("[A-Z]").WithMessage("At least one uppercase letter required.")
                .Matches("[a-z]").WithMessage("At least one lowercase letter required.")
                .Matches("[0-9]").WithMessage("At least one number required.")
                .Matches("[^a-zA-Z0-9]").WithMessage("At least one special character required.");
        }

        private static bool HasValidDomain(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var parts = email.Split('@');
            if (parts.Length != 2) return false;

            var domain = parts[1];
            var lastDot = domain.LastIndexOf('.');
            return lastDot > 0 && lastDot < domain.Length - 2;
        }
    }
}
