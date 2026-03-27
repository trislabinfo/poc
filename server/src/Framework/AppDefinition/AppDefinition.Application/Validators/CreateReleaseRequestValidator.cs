using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for CreateReleaseRequest (AppBuilder and TenantApplication).</summary>
public sealed class CreateReleaseRequestValidator : AbstractValidator<CreateReleaseRequest>
{
    public CreateReleaseRequestValidator()
    {
        RuleFor(x => x.AppDefinitionId).NotEmpty().WithMessage("Application definition ID is required.");
        RuleFor(x => x.Version).NotEmpty().WithMessage("Version is required.");
        RuleFor(x => x.Major).GreaterThanOrEqualTo(0).WithMessage("Major must be non-negative.");
        RuleFor(x => x.Minor).GreaterThanOrEqualTo(0).WithMessage("Minor must be non-negative.");
        RuleFor(x => x.Patch).GreaterThanOrEqualTo(0).WithMessage("Patch must be non-negative.");
        RuleFor(x => x.ReleasedBy).NotEmpty().WithMessage("Released by is required.");
    }
}
