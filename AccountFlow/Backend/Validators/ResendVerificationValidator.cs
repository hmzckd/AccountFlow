using AccountFlow.Backend.Models;
using FluentValidation;

namespace AccountFlow.Backend.Validators;

public class ResendVerificationValidator : AbstractValidator<ResendVerificationRequestDto>
{
    public ResendVerificationValidator()
    {
        RuleFor(request => request.Email).ValidEmail();
    }
}
