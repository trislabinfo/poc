using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for CreateEntityRequest (AppBuilder and TenantApplication).</summary>
public sealed class CreateEntityRequestValidator : AbstractValidator<CreateEntityRequest>
{
    public CreateEntityRequestValidator()
    {
        RuleFor(x => x.AppDefinitionId).NotEmpty().WithMessage("Application definition ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("Display name is required.").MaximumLength(200).WithMessage("Display name cannot exceed 200 characters.");
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
        RuleFor(x => x.PrimaryKey).MaximumLength(100).When(x => x.PrimaryKey != null);
    }
}
