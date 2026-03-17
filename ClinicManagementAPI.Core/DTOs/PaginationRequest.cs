using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Core.DTOs;

public class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [Range(1, 50)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : value > 50 ? 50 : value;
    }

    public string? SearchTerm { get; set; }
}
