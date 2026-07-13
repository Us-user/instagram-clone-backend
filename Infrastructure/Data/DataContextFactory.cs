using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
/// Design-time фабрика контекста для инструментов EF (`dotnet ef migrations add …`).
/// Позволяет создавать миграции без сборки хоста WebApi. Строка подключения здесь
/// используется только для определения провайдера (Npgsql) — реального соединения
/// при генерации миграции не происходит. Рантайм берёт строку из appsettings.json.
/// </summary>
public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=instagram_clone;Username=postgres;Password=postgres")
            .Options;

        return new DataContext(options);
    }
}
