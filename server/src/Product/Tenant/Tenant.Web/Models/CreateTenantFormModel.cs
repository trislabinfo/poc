using System.ComponentModel.DataAnnotations;

namespace Tenant.Web.Models;

public sealed class CreateTenantFormModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Slug { get; set; } = string.Empty;
}

