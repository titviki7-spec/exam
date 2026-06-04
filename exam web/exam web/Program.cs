using Npgsql;
using System.Text.Json;

const string ConnectionString = "Host=localhost;Database=integrations;Username=postgres;Password=postgres";
const string ApiKey = "256c9be1d876fed8d5b8af8b23ef14a0";

// Создаём таблицу при старте (если не существует)
await InitializeDatabase();

// Пример использования: сохраняем погоду для Москвы
await FetchAndStoreWeather("Moscow");

// Просмотр последних записей
await ShowRecentRecords();

// ---------- Методы ----------

async Task InitializeDatabase()
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    var sql = @"
        CREATE TABLE IF NOT EXISTS external_data (
            id BIGSERIAL PRIMARY KEY,
            api_name VARCHAR(100) NOT NULL,
            response_json JSONB NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        CREATE INDEX IF NOT EXISTS idx_external_data_api_created 
            ON external_data (api_name, created_at DESC);";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

async Task FetchAndStoreWeather(string city)
{
    using var http = new HttpClient();
    var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={ApiKey}&units=metric&lang=ru";

    try
    {
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        // Убедимся, что это валидный JSON (для JSONB обязательно)
        using var doc = JsonDocument.Parse(json);

        await SaveResponse("OpenWeather", json);
        Console.WriteLine($"Погода для '{city}' сохранена.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

async Task SaveResponse(string apiName, string json)
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    var sql = "INSERT INTO external_data (api_name, response_json) VALUES (@name, @json::jsonb)";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("name", apiName);
    cmd.Parameters.AddWithValue("json", json); // передаём как текст, явно приводим к jsonb
    await cmd.ExecuteNonQueryAsync();
}

async Task ShowRecentRecords()
{
    await using var conn = new NpgsqlConnection(ConnectionString);
    await conn.OpenAsync();
    var sql = "SELECT id, api_name, created_at FROM external_data ORDER BY id DESC LIMIT 3";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"{reader.GetInt64(0)} | {reader.GetString(1)} | {reader.GetDateTime(2)}");
    }
}
