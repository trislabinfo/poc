using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for CreateNavigationRequest (AppBuilder and TenantApplication).</summary>
public sealed class CreateNavigationRequestValidator : AbstractValidator<CreateNavigationRequest>
{
    public CreateNavigationRequestValidator()
    {
        RuleFor(x => x.AppDefinitionId).NotEmpty().WithMessage("Application definition ID is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
    }
}
