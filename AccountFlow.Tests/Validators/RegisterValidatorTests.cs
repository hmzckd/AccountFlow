using AccountFlow.Backend.Models;
using AccountFlow.Backend.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace AccountFlow.Tests.Validators
{
    public class RegisterValidatorTests
    {
        private readonly RegisterValidator _validator = new();

        private static RegisterDto Valid() => new()
        {
            Name = "Ada",
            Surname = "Lovelace",
            Email = "ada@example.com",
            Password = "Str0ng!Pass"
        };

        [Fact]
        public void Passes_For_A_Valid_Registration()
        {
            _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("user@localhost")]     // no dot in domain
        [InlineData("user@example.")]      // trailing dot
        [InlineData("user@example.c")]     // TLD too short
        public void Fails_For_Invalid_Emails(string email)
        {
            var dto = Valid();
            dto.Email = email;
            _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("short1!A", false)]    // valid (8 chars, all classes)
        [InlineData("alllower1!", true)]   // no uppercase
        [InlineData("ALLUPPER1!", true)]   // no lowercase
        [InlineData("NoDigits!!", true)]   // no number
        [InlineData("NoSpecial1", true)]   // no special char
        [InlineData("Ab1!", true)]         // too short
        public void Enforces_Password_Complexity(string password, bool shouldFail)
        {
            var dto = Valid();
            dto.Password = password;
            var result = _validator.TestValidate(dto);

            if (shouldFail)
                result.ShouldHaveValidationErrorFor(x => x.Password);
            else
                result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Requires_Name_And_Surname()
        {
            var dto = Valid();
            dto.Name = "";
            dto.Surname = "";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Surname);
        }
    }
}
