using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for UpdatePageRequest (AppBuilder and TenantApplication).</summary>
public sealed class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
        RuleFor(x => x.Route).NotEmpty().WithMessage("Route is required.").MaximumLength(500).WithMessage("Route cannot exceed 500 characters.");
    }
}
