using Domain.DTOs.Location;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;
using Infrastructure.Common;
using Infrastructure.Data;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Справочник локаций: фильтр с пагинацией, чтение по Id и CRUD. Локации не имеют владельца —
/// операции доступны любому авторизованному юзеру. Валидация тел запросов — через FluentValidation.
/// </summary>
public class LocationService : ILocationService
{
    private readonly DataContext _context;
    private readonly IValidator<AddLocationDto> _addValidator;
    private readonly IValidator<UpdateLocationDto> _updateValidator;

    public LocationService(
        DataContext context,
        IValidator<AddLocationDto> addValidator,
        IValidator<UpdateLocationDto> updateValidator)
    {
        _context = context;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResponse<List<GetLocationDto>>> GetLocationsAsync(
        string? city, string? state, string? zipCode, string? country, int? pageNumber, int? pageSize)
    {
        var (page, size) = Pagination.Normalize(pageNumber, pageSize);

        var query = _context.Locations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var term = city.Trim().ToLower();
            query = query.Where(l => l.City.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            var term = state.Trim().ToLower();
            query = query.Where(l => l.State.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(zipCode))
        {
            var term = zipCode.Trim().ToLower();
            query = query.Where(l => l.ZipCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var term = country.Trim().ToLower();
            query = query.Where(l => l.Country.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var locations = await query
            .OrderBy(l => l.Country).ThenBy(l => l.City)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(l => new GetLocationDto
            {
                Id = l.Id,
                City = l.City,
                State = l.State,
                ZipCode = l.ZipCode,
                Country = l.Country
            })
            .ToListAsync();

        return new PagedResponse<List<GetLocationDto>>(locations, total, page, size);
    }

    public async Task<Response<GetLocationDto>> GetLocationByIdAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id локации.");

        var dto = await _context.Locations.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new GetLocationDto
            {
                Id = l.Id,
                City = l.City,
                State = l.State,
                ZipCode = l.ZipCode,
                Country = l.Country
            })
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Локация не найдена.");

        return new Response<GetLocationDto>(dto);
    }

    public async Task<Response<GetLocationDto>> AddLocationAsync(AddLocationDto dto)
    {
        await _addValidator.ValidateAndThrowAsync(dto);

        var location = new Location
        {
            City = dto.City.Trim(),
            State = dto.State.Trim(),
            ZipCode = dto.ZipCode.Trim(),
            Country = dto.Country.Trim()
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        return new Response<GetLocationDto>(new GetLocationDto
        {
            Id = location.Id,
            City = location.City,
            State = location.State,
            ZipCode = location.ZipCode,
            Country = location.Country
        });
    }

    public async Task<Response<GetLocationDto>> UpdateLocationAsync(UpdateLocationDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == dto.LocationId)
            ?? throw new NotFoundException("Локация не найдена.");

        location.City = dto.City.Trim();
        location.State = dto.State.Trim();
        location.ZipCode = dto.ZipCode.Trim();
        location.Country = dto.Country.Trim();

        await _context.SaveChangesAsync();

        return new Response<GetLocationDto>(new GetLocationDto
        {
            Id = location.Id,
            City = location.City,
            State = location.State,
            ZipCode = location.ZipCode,
            Country = location.Country
        });
    }

    public async Task<Response<bool>> DeleteLocationAsync(int? id)
    {
        if (id is null or <= 0)
            throw new BadRequestException("Некорректный Id локации.");

        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Локация не найдена.");

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();

        return new Response<bool>(true);
    }
}
