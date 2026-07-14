using System.Text.Json;

namespace Infrastructure.Common;

/// <summary>
/// Генерация «волны» голосового сообщения (§8). Полноценный анализ аудио (NAudio/FFmpeg) — вне
/// объёма этой фазы, поэтому строим правдоподобный детерминированный плейсхолдер-массив
/// нормализованных амплитуд по длительности; поле <c>Waveform</c> заполняется всегда, а генерацию
/// из реального аудио можно подключить позже без изменения контракта.
/// </summary>
public static class WaveformGenerator
{
    private const int MinBars = 16;
    private const int MaxBars = 64;

    /// <summary>JSON-массив амплитуд [0.05; 1.0] длиной, пропорциональной длительности.</summary>
    public static string Placeholder(int durationSeconds)
    {
        var bars = Math.Clamp(durationSeconds <= 0 ? 24 : durationSeconds * 4, MinBars, MaxBars);
        var values = new double[bars];
        for (var i = 0; i < bars; i++)
        {
            // Сглаженная синусоида с лёгкой модуляцией — визуально похоже на речь.
            var v = 0.5 + 0.4 * Math.Sin(i * 0.6) * Math.Cos(i * 0.15);
            values[i] = Math.Round(Math.Clamp(Math.Abs(v), 0.05, 1.0), 2);
        }

        // System.Text.Json форматирует числа инвариантно — разделитель всегда точка.
        return JsonSerializer.Serialize(values);
    }
}
