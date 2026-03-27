using AppDefinition.Contracts.Requests;
using FluentValidation;

namespace AppDefinition.Application.Validators;

/// <summary>Shared validation rules for UpdateDataSourceRequest (AppBuilder and TenantApplication).</summary>
public sealed class UpdateDataSourceRequestValidator : AbstractValidator<UpdateDataSourceRequest>
{
    public UpdateDataSourceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
    }
}
