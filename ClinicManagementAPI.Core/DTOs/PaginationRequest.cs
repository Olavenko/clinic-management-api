using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs;

public class PaginationRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    public string? SearchTerm { get; set; }
}
