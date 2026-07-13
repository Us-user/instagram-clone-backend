using System.Text.Json;
using Domain.Exceptions;
using Domain.Responses;
using FluentValidation;

namespace WebApi.Middleware;

/// <summary>
/// Глобальный обработчик исключений: превращает любое исключение в <see cref="Response{T}"/>
/// с корректным statusCode и списком errors. 5xx логируются как ошибки, 4xx — как предупреждения.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errors) = exception switch
        {
            ValidationException ve => (400, ve.Errors.Select(e => e.ErrorMessage).Distinct().ToList()),
            BadRequestException => (400, new List<string> { exception.Message }),
            NotFoundException => (404, new List<string> { exception.Message }),
            ForbiddenException => (403, new List<string> { exception.Message }),
            UnauthorizedAccessException => (401, new List<string> { exception.Message }),
            _ => (500, new List<string> { "Внутренняя ошибка сервера." })
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Необработанное исключение");
        else
            _logger.LogWarning("Обработанное исключение ({StatusCode}): {Message}", statusCode, exception.Message);

        var response = new Response<string>(statusCode, errors);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
