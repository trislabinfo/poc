using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for CreatePropertyRequest (AppBuilder and TenantApplication).</summary>
public sealed class CreatePropertyRequestValidator : AbstractValidator<CreatePropertyRequest>
{
    public CreatePropertyRequestValidator()
    {
        RuleFor(x => x.EntityDefinitionId).NotEmpty().WithMessage("Entity definition ID is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative.");
        RuleFor(x => x.DefaultValue).MaximumLength(500).When(x => x.DefaultValue != null);
    }
}
