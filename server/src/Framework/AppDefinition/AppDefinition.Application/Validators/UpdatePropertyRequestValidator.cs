using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for UpdatePropertyRequest (AppBuilder and TenantApplication).</summary>
public sealed class UpdatePropertyRequestValidator : AbstractValidator<UpdatePropertyRequest>
{
    public UpdatePropertyRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0).WithMessage("Order must be non-negative.");
        RuleFor(x => x.DefaultValue).MaximumLength(500).When(x => x.DefaultValue != null);
    }
}
