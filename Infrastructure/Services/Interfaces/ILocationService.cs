using Domain.DTOs.Location;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Справочник локаций (независимая фича): фильтр с пагинацией, чтение по Id и CRUD.
/// Локации не привязаны к владельцу — редактировать/удалять может любой авторизованный юзер.
/// </summary>
public interface ILocationService
{
    /// <summary>Локации с фильтром по городу/региону/индексу/стране и пагинацией.</summary>
    Task<PagedResponse<List<GetLocationDto>>> GetLocationsAsync(
        string? city, string? state, string? zipCode, string? country, int? pageNumber, int? pageSize);

    /// <summary>Локация по Id.</summary>
    Task<Response<GetLocationDto>> GetLocationByIdAsync(int? id);

    /// <summary>Создать локацию (все поля обязательны).</summary>
    Task<Response<GetLocationDto>> AddLocationAsync(AddLocationDto dto);

    /// <summary>Обновить локацию по <c>LocationId</c> (все поля обязательны).</summary>
    Task<Response<GetLocationDto>> UpdateLocationAsync(UpdateLocationDto dto);

    /// <summary>Удалить локацию по Id.</summary>
    Task<Response<bool>> DeleteLocationAsync(int? id);
}
