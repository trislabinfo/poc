# Feature Module - Application Layer

**Status**: ✅ Complete  
**Last Updated**: 2026-02-11  
**Module**: Feature  
**Layer**: Application  

---

## Overview

CQRS commands and queries for feature management and feature flag evaluation.

---

## Commands

### 1. CreateFeatureCommand

**Purpose**: Create a new global feature definition

```csharp
namespace Datarizen.Feature.Application.Commands.CreateFeature;

public sealed record CreateFeatureCommand(
    string Code,
    string Name,
    string Description,
    string Category,
    bool IsGloballyEnabled = false
) : ICommand<Result<Guid>>;

public sealed class CreateFeatureCommandHandler 
    : ICommandHandler<CreateFeatureCommand, Result<Guid>>
{
    private readonly IFeatureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(
        CreateFeatureCommand command,
        CancellationToken cancellationToken)
    {
        // Check if code already exists
        var existing = await _repository.GetByCodeAsync(command.Code, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure(Error.Conflict(
                "Feature.Code.AlreadyExists",
                $"Feature with code '{command.Code}' already exists"));

        var featureResult = Entities.Feature.Create(
            command.Code,
            command.Name,
            command.Description,
            command.Category,
            command.IsGloballyEnabled,
            _dateTimeProvider);

        if (featureResult.IsFailure)
            return Result<Guid>.Failure(featureResult.Error);

        await _repository.AddAsync(featureResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(featureResult.Value.Id);
    }
}
```

### 2. UpdateFeatureCommand

```csharp
namespace Datarizen.Feature.Application.Commands.UpdateFeature;

public sealed record UpdateFeatureCommand(
    Guid Id,
    string Name,
    string Description,
    string Category
) : ICommand<Result>;

public sealed class UpdateFeatureCommandHandler 
    : ICommandHandler<UpdateFeatureCommand, Result>
{
    private readonly IFeatureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result> Handle(
        UpdateFeatureCommand command,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (feature is null)
            return Result.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        var updateResult = feature.Update(
            command.Name,
            command.Description,
            command.Category,
            _dateTimeProvider);

        if (updateResult.IsFailure)
            return updateResult;

        await _repository.UpdateAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 3. EnableFeatureCommand

```csharp
namespace Datarizen.Feature.Application.Commands.EnableFeature;

public sealed record EnableFeatureCommand(Guid Id) : ICommand<Result>;

public sealed class EnableFeatureCommandHandler 
    : ICommandHandler<EnableFeatureCommand, Result>
{
    private readonly IFeatureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result> Handle(
        EnableFeatureCommand command,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (feature is null)
            return Result.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        var enableResult = feature.Enable(_dateTimeProvider);
        if (enableResult.IsFailure)
            return enableResult;

        await _repository.UpdateAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 4. DisableFeatureCommand

```csharp
namespace Datarizen.Feature.Application.Commands.DisableFeature;

public sealed record DisableFeatureCommand(Guid Id) : ICommand<Result>;

public sealed class DisableFeatureCommandHandler 
    : ICommandHandler<DisableFeatureCommand, Result>
{
    private readonly IFeatureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result> Handle(
        DisableFeatureCommand command,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (feature is null)
            return Result.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        var disableResult = feature.Disable(_dateTimeProvider);
        if (disableResult.IsFailure)
            return disableResult;

        await _repository.UpdateAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 5. DeleteFeatureCommand

```csharp
namespace Datarizen.Feature.Application.Commands.DeleteFeature;

public sealed record DeleteFeatureCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteFeatureCommandHandler 
    : ICommandHandler<DeleteFeatureCommand, Result>
{
    private readonly IFeatureRepository _repository;
    private readonly IFeatureFlagRepository _flagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        DeleteFeatureCommand command,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (feature is null)
            return Result.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        // Check if feature has any flags
        // This would require a new repository method or query
        // For now, we'll allow deletion (cascade will handle flags)

        await _repository.DeleteAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 6. CreateFeatureFlagCommand

```csharp
namespace Datarizen.Feature.Application.Commands.CreateFeatureFlag;

public sealed record CreateFeatureFlagCommand(
    Guid FeatureId,
    Guid? TenantId,
    Guid? UserId,
    bool IsEnabled,
    string? Configuration
) : ICommand<Result<Guid>>;

public sealed class CreateFeatureFlagCommandHandler 
    : ICommandHandler<CreateFeatureFlagCommand, Result<Guid>>
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result<Guid>> Handle(
        CreateFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        // Validate feature exists
        var feature = await _featureRepository.GetByIdAsync(
            command.FeatureId,
            cancellationToken);

        if (feature is null)
            return Result<Guid>.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        // Check if flag already exists for this scope
        if (command.TenantId.HasValue)
        {
            var existing = await _repository.GetByFeatureAndTenantAsync(
                command.FeatureId,
                command.TenantId.Value,
                cancellationToken);

            if (existing is not null)
                return Result<Guid>.Failure(Error.Conflict(
                    "FeatureFlag.AlreadyExists",
                    "Feature flag already exists for this tenant"));
        }
        else if (command.UserId.HasValue)
        {
            var existing = await _repository.GetByFeatureAndUserAsync(
                command.FeatureId,
                command.UserId.Value,
                cancellationToken);

            if (existing is not null)
                return Result<Guid>.Failure(Error.Conflict(
                    "FeatureFlag.AlreadyExists",
                    "Feature flag already exists for this user"));
        }

        var flagResult = FeatureFlag.Create(
            command.FeatureId,
            command.TenantId,
            command.UserId,
            command.IsEnabled,
            command.Configuration,
            _dateTimeProvider);

        if (flagResult.IsFailure)
            return Result<Guid>.Failure(flagResult.Error);

        await _repository.AddAsync(flagResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(flagResult.Value.Id);
    }
}
```

### 7. ToggleFeatureFlagCommand

```csharp
namespace Datarizen.Feature.Application.Commands.ToggleFeatureFlag;

public sealed record ToggleFeatureFlagCommand(Guid Id) : ICommand<Result>;

public sealed class ToggleFeatureFlagCommandHandler 
    : ICommandHandler<ToggleFeatureFlagCommand, Result>
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result> Handle(
        ToggleFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        var flag = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound(
                "FeatureFlag.NotFound",
                "Feature flag not found"));

        var toggleResult = flag.Toggle(_dateTimeProvider);
        if (toggleResult.IsFailure)
            return toggleResult;

        await _repository.UpdateAsync(flag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 8. UpdateFeatureFlagConfigurationCommand

```csharp
namespace Datarizen.Feature.Application.Commands.UpdateFeatureFlagConfiguration;

public sealed record UpdateFeatureFlagConfigurationCommand(
    Guid Id,
    string? Configuration
) : ICommand<Result>;

public sealed class UpdateFeatureFlagConfigurationCommandHandler 
    : ICommandHandler<UpdateFeatureFlagConfigurationCommand, Result>
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<Result> Handle(
        UpdateFeatureFlagConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        var flag = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound(
                "FeatureFlag.NotFound",
                "Feature flag not found"));

        var updateResult = flag.UpdateConfiguration(
            command.Configuration,
            _dateTimeProvider);

        if (updateResult.IsFailure)
            return updateResult;

        await _repository.UpdateAsync(flag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 9. DeleteFeatureFlagCommand

```csharp
namespace Datarizen.Feature.Application.Commands.DeleteFeatureFlag;

public sealed record DeleteFeatureFlagCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteFeatureFlagCommandHandler 
    : ICommandHandler<DeleteFeatureFlagCommand, Result>
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        DeleteFeatureFlagCommand command,
        CancellationToken cancellationToken)
    {
        var flag = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound(
                "FeatureFlag.NotFound",
                "Feature flag not found"));

        await _repository.DeleteAsync(flag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

---

## Queries

### 1. GetFeatureByIdQuery

```csharp
namespace Datarizen.Feature.Application.Queries.GetFeatureById;

public sealed record GetFeatureByIdQuery(Guid Id) : IQuery<Result<FeatureDto>>;

public sealed class GetFeatureByIdQueryHandler 
    : IQueryHandler<GetFeatureByIdQuery, Result<FeatureDto>>
{
    private readonly IFeatureRepository _repository;

    public async Task<Result<FeatureDto>> Handle(
        GetFeatureByIdQuery query,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (feature is null)
            return Result<FeatureDto>.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        var dto = FeatureDto.FromDomain(feature);
        return Result<FeatureDto>.Success(dto);
    }
}
```

### 2. GetFeatureByCodeQuery

```csharp
namespace Datarizen.Feature.Application.Queries.GetFeatureByCode;

public sealed record GetFeatureByCodeQuery(string Code) : IQuery<Result<FeatureDto>>;

public sealed class GetFeatureByCodeQueryHandler 
    : IQueryHandler<GetFeatureByCodeQuery, Result<FeatureDto>>
{
    private readonly IFeatureRepository _repository;

    public async Task<Result<FeatureDto>> Handle(
        GetFeatureByCodeQuery query,
        CancellationToken cancellationToken)
    {
        var feature = await _repository.GetByCodeAsync(query.Code, cancellationToken);
        if (feature is null)
            return Result<FeatureDto>.Failure(Error.NotFound(
                "Feature.NotFound",
                "Feature not found"));

        var dto = FeatureDto.FromDomain(feature);
        return Result<FeatureDto>.Success(dto);
    }
}
```

### 3. GetAllFeaturesQuery

```csharp
namespace Datarizen.Feature.Application.Queries.GetAllFeatures;

public sealed record GetAllFeaturesQuery : IQuery<Result<List<FeatureDto>>>;

public sealed class GetAllFeaturesQueryHandler 
    : IQueryHandler<GetAllFeaturesQuery, Result<List<FeatureDto>>>
{
    private readonly IFeatureRepository _repository;

    public async Task<Result<List<FeatureDto>>> Handle(
        GetAllFeaturesQuery query,
        CancellationToken cancellationToken)
    {
        var features = await _repository.GetAllAsync(cancellationToken);
        var dtos = features.Select(FeatureDto.FromDomain).ToList();
        return Result<List<FeatureDto>>.Success(dtos);
    }
}
```

### 4. GetFeaturesByCategoryQuery

```csharp
namespace Datarizen.Feature.Application.Queries.GetFeaturesByCategory;

public sealed record GetFeaturesByCategoryQuery(string Category) 
    : IQuery<Result<List<FeatureDto>>>;

public sealed class GetFeaturesByCategoryQueryHandler 
    : IQueryHandler<GetFeaturesByCategoryQuery, Result<List<FeatureDto>>>
{
    private readonly IFeatureRepository _repository;

    public async Task<Result<List<FeatureDto>>> Handle(
        GetFeaturesByCategoryQuery query,
        CancellationToken cancellationToken)
    {
        var features = await _repository.GetByCategoryAsync(
            query.Category,
            cancellationToken);

        var dtos = features.Select(FeatureDto.FromDomain).ToList();
        return Result<List<FeatureDto>>.Success(dtos);
    }
}
```

### 5. GetFeatureFlagsForTenantQuery

```csharp
namespace Datarizen.Feature.Application.Queries.GetFeatureFlagsForTenant;

public sealed record GetFeatureFlagsForTenantQuery(Guid TenantId) 
    : IQuery<Result<List<FeatureFlagDto>>>;

public sealed class GetFeatureFlagsForTenantQueryHandler 
    : IQueryHandler<GetFeatureFlagsForTenantQuery, Result<List<FeatureFlagDto>>>
{
    private readonly IFeatureFlagRepository _repository;

    public async Task<Result<List<FeatureFlagDto>>> Handle(
        GetFeatureFlagsForTenantQuery query,
        CancellationToken cancellationToken)
    {
        var flags = await _repository.GetByTenantIdAsync(
            query.TenantId,
            cancellationToken);

        var dtos = flags.Select(FeatureFlagDto.FromDomain).ToList();
        return Result<List<FeatureFlagDto>>.Success(dtos);
    }
}
```

### 6. IsFeatureEnabledQuery

**Purpose**: Evaluate if a feature is enabled for a specific context (hierarchical)

```csharp
namespace Datarizen.Feature.Application.Queries.IsFeatureEnabled;

public sealed record IsFeatureEnabledQuery(
    string FeatureCode,
    Guid? TenantId = null,
    Guid? UserId = null
) : IQuery<Result<FeatureEvaluationDto>>;

public sealed class IsFeatureEnabledQueryHandler 
    : IQueryHandler<IsFeatureEnabledQuery, Result<FeatureEvaluationDto>>
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureFlagRepository _flagRepository;

    public async Task<Result<FeatureEvaluationDto>> Handle(
        IsFeatureEnabledQuery query,
        CancellationToken cancellationToken)
    {
        // Get feature definition
        var feature = await _featureRepository.GetByCodeAsync(
            query.FeatureCode,
            cancellationToken);

        if (feature is null)
            return Result<FeatureEvaluationDto>.Failure(Error.NotFound(
                "Feature.NotFound",
                $"Feature '{query.FeatureCode}' not found"));

        // Hierarchical evaluation: User > Tenant > Global
        FeatureFlag? effectiveFlag = null;
        string? configuration = null;

        // 1. Check user-specific flag
        if (query.UserId.HasValue)
        {
            effectiveFlag = await _flagRepository.GetByFeatureAndUserAsync(
                feature.Id,
                query.UserId.Value,
                cancellationToken);
        }

        // 2. Check tenant-specific flag
        if (effectiveFlag is null && query.TenantId.HasValue)
        {
            effectiveFlag = await _flagRepository.GetByFeatureAndTenantAsync(
                feature.Id,
                query.TenantId.Value,
                cancellationToken);
        }

        // 3. Use global default
        bool isEnabled = effectiveFlag?.IsEnabled ?? feature.IsGloballyEnabled;
        configuration = effectiveFlag?.Configuration;

        var dto = new FeatureEvaluationDto(
            query.FeatureCode,
            isEnabled,
            configuration);

        return Result<FeatureEvaluationDto>.Success(dto);
    }
}
```

---

## DTOs

### FeatureDto

```csharp
namespace Datarizen.Feature.Application.DTOs;

public sealed record FeatureDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsGloballyEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static FeatureDto FromDomain(Entities.Feature feature)
    {
        return new FeatureDto
        {
            Id = feature.Id,
            Code = feature.Code,
            Name = feature.Name,
            Description = feature.Description,
            Category = feature.Category,
            IsGloballyEnabled = feature.IsGloballyEnabled,
            CreatedAt = feature.CreatedAt,
            UpdatedAt = feature.UpdatedAt
        };
    }
}
```

### FeatureFlagDto

```csharp
namespace Datarizen.Feature.Application.DTOs;

public sealed record FeatureFlagDto
{
    public Guid Id { get; init; }
    public Guid FeatureId { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
    public bool IsEnabled { get; init; }
    public string? Configuration { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static FeatureFlagDto FromDomain(FeatureFlag flag)
    {
        return new FeatureFlagDto
        {
            Id = flag.Id,
            FeatureId = flag.FeatureId,
            TenantId = flag.TenantId,
            UserId = flag.UserId,
            IsEnabled = flag.IsEnabled,
            Configuration = flag.Configuration,
            CreatedAt = flag.CreatedAt,
            UpdatedAt = flag.UpdatedAt
        };
    }
}
```

### FeatureEvaluationDto

```csharp
namespace Datarizen.Feature.Application.DTOs;

public sealed record FeatureEvaluationDto(
    string Code,
    bool IsEnabled,
    string? Configuration);
```

---

## Validators

### CreateFeatureCommandValidator

```csharp
namespace Datarizen.Feature.Application.Commands.CreateFeature;

public sealed class CreateFeatureCommandValidator 
    : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Feature code is required")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Feature code must be lowercase-kebab-case");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Feature name is required")
            .MaximumLength(200)
            .WithMessage("Feature name cannot exceed 200 characters");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Feature category is required")
            .MaximumLength(100)
            .WithMessage("Feature category cannot exceed 100 characters");
    }
}
```

### CreateFeatureFlagCommandValidator

```csharp
namespace Datarizen.Feature.Application.Commands.CreateFeatureFlag;

public sealed class CreateFeatureFlagCommandValidator 
    : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.FeatureId)
            .NotEmpty()
            .WithMessage("Feature ID is required");

        RuleFor(x => x)
            .Must(x => x.TenantId.HasValue || x.UserId.HasValue)
            .WithMessage("Either TenantId or UserId must be provided")
            .Must(x => !(x.TenantId.HasValue && x.UserId.HasValue))
            .WithMessage("Cannot specify both TenantId and UserId");

        When(x => !string.IsNullOrWhiteSpace(x.Configuration), () =>
        {
            RuleFor(x => x.Configuration)
                .Must(BeValidJson)
                .WithMessage("Configuration must be valid JSON");
        });
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

---

## Service Registration

**File**: `Feature.Application/DependencyInjection.cs`

```csharp
namespace Datarizen.Feature.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFeatureApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

---

## Success Criteria

- ✅ All CRUD commands for Features
- ✅ All CRUD commands for FeatureFlags
- ✅ Hierarchical feature evaluation (user > tenant > global)
- ✅ Configuration validation (JSON)
- ✅ Scope validation (tenant XOR user)
- ✅ DTOs with FromDomain mappers
- ✅ FluentValidation validators
- ✅ Service registration complete

**Estimated Time**: 4 hours


