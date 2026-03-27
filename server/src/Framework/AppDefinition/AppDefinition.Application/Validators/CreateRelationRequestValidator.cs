using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for CreateRelationRequest (AppBuilder and TenantApplication).</summary>
public sealed class CreateRelationRequestValidator : AbstractValidator<CreateRelationRequest>
{
    public CreateRelationRequestValidator()
    {
        RuleFor(x => x.SourceEntityId).NotEmpty().WithMessage("Source entity ID is required.");
        RuleFor(x => x.TargetEntityId).NotEmpty().WithMessage("Target entity ID is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
    }
}
