using AccountFlow.Backend.Models;
using AccountFlow.Backend.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace AccountFlow.Tests.Validators
{
    public class LoginValidatorTests
    {
        private readonly LoginValidator _validator = new();

        [Fact]
        public void Passes_For_A_Valid_Email()
        {
            var dto = new LoginDto { Email = "user@example.com", Password = "whatever" };
            _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("")]
        [InlineData("bad")]
        [InlineData("user@example.")]
        public void Fails_For_Invalid_Email(string email)
        {
            var dto = new LoginDto { Email = email, Password = "whatever" };
            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
        }
    }

    public class PasswordValidatorTests
    {
        private readonly PasswordValidator _validator = new();

        [Fact]
        public void Passes_For_A_Strong_Password()
        {
            var dto = new ResetPasswordDto { NewPassword = "Str0ng!Pass", Token = "t" };
            _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        }

        [Theory]
        [InlineData("weak")]
        [InlineData("nouppercase1!")]
        [InlineData("NOLOWERCASE1!")]
        [InlineData("NoNumber!!")]
        [InlineData("NoSpecial11")]
        public void Fails_For_Weak_Passwords(string password)
        {
            var dto = new ResetPasswordDto { NewPassword = password, Token = "t" };
            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NewPassword);
        }
    }
}
