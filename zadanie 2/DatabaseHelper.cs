using MySqlConnector;

namespace zadanie_2;

public class DatabaseHelper
{
    private const string ConnectionString =
        "Server=85.236.189.123;Port=13306;Database=isp-44_kuralenkov_rg_zadanie2;Uid=isp-44_kuralenkov_rg;Pwd=isp-44_kuralenkov_rg;";

    private const string MasterConnectionString =
        "Server=85.236.189.123;Port=13306;Uid=isp-44_kuralenkov_rg;Pwd=isp-44_kuralenkov_rg;";

    public static string SchemaName => "isp-44_kuralenkov_rg_zadanie2";

    public MySqlConnection CreateConnection() => new MySqlConnection(ConnectionString);

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var conn = new MySqlConnection(MasterConnectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CreateSchemaIfNotExistsAsync()
    {
        try
        {
            using var conn = new MySqlConnection(MasterConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{SchemaName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
            await cmd.ExecuteNonQueryAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ServiceDto>> GetServicesAsync(string? searchText = null, double? minDiscount = null, double? maxDiscount = null, string sortBy = "ID", bool sortAsc = true)
    {
        var services = new List<ServiceDto>();
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            var conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("Title LIKE @search");
                cmd.Parameters.AddWithValue("@search", $"%{searchText}%");
            }

            if (minDiscount.HasValue)
            {
                conditions.Add("(Discount IS NOT NULL AND Discount >= @minDisc)");
                cmd.Parameters.AddWithValue("@minDisc", minDiscount.Value);
            }

            if (maxDiscount.HasValue)
            {
                conditions.Add("(Discount IS NOT NULL AND Discount <= @maxDisc)");
                cmd.Parameters.AddWithValue("@maxDisc", maxDiscount.Value);
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            var orderClause = sortBy switch
            {
                "Cost" => sortAsc ? "ORDER BY Cost ASC" : "ORDER BY Cost DESC",
                _ => sortAsc ? "ORDER BY ID ASC" : "ORDER BY ID DESC"
            };

            cmd.CommandText = $@"
                SELECT ID, Title, Cost, DurationInSeconds, Description, Discount, MainImagePath
                FROM `Service`
                {whereClause}
                {orderClause}";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                services.Add(new ServiceDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetDecimal(2),
                    reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)
                ));
            }
        }
        catch
        {
        }
        return services;
    }

    public async Task<bool> ServiceTitleExistsAsync(string title, int? excludeId = null)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            if (excludeId.HasValue)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM `Service` WHERE Title = @title AND ID != @id";
                cmd.Parameters.AddWithValue("@id", excludeId.Value);
            }
            else
            {
                cmd.CommandText = "SELECT COUNT(*) FROM `Service` WHERE Title = @title";
            }
            cmd.Parameters.AddWithValue("@title", title);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AddServiceAsync(ServiceDto service)
    {
        try
        {
            if (service.DurationInSeconds < 0 || service.DurationInSeconds > 14400)
                return false;

            if (await ServiceTitleExistsAsync(service.Title))
                return false;

            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `Service` (Title, Cost, DurationInSeconds, Description, Discount, MainImagePath)
                VALUES (@title, @cost, @duration, @description, @discount, @imagePath);
                SELECT LAST_INSERT_ID();";
            cmd.Parameters.AddWithValue("@title", service.Title);
            cmd.Parameters.AddWithValue("@cost", service.Cost);
            cmd.Parameters.AddWithValue("@duration", service.DurationInSeconds);
            cmd.Parameters.AddWithValue("@description", (object?)service.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@discount", (object?)service.Discount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@imagePath", (object?)service.MainImagePath ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteServiceAsync(int serviceId)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM `Service` WHERE ID = @id";
            cmd.Parameters.AddWithValue("@id", serviceId);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateServiceAsync(ServiceDto service)
    {
        try
        {
            if (service.DurationInSeconds < 0 || service.DurationInSeconds > 14400)
                return false;

            if (await ServiceTitleExistsAsync(service.Title, service.ID))
                return false;

            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE `Service` 
                SET Title = @title, Cost = @cost, DurationInSeconds = @duration, 
                    Description = @description, Discount = @discount, MainImagePath = @imagePath
                WHERE ID = @id";
            cmd.Parameters.AddWithValue("@title", service.Title);
            cmd.Parameters.AddWithValue("@cost", service.Cost);
            cmd.Parameters.AddWithValue("@duration", service.DurationInSeconds);
            cmd.Parameters.AddWithValue("@description", (object?)service.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@discount", (object?)service.Discount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@imagePath", (object?)service.MainImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", service.ID);
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ClientDto>> GetClientsAsync()
    {
        var clients = new List<ClientDto>();
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ID, FirstName, LastName, Patronymic, Phone
                FROM `Client`
                ORDER BY LastName, FirstName";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                clients.Add(new ClientDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.GetString(4)
                ));
            }
        }
        catch
        {
        }
        return clients;
    }

    public async Task<bool> CreateClientServiceAsync(int clientId, int serviceId, DateTime startTime, string? comment = null)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `ClientService` (ClientID, ServiceID, StartTime, Comment)
                VALUES (@clientId, @serviceId, @startTime, @comment)";
            cmd.Parameters.AddWithValue("@clientId", clientId);
            cmd.Parameters.AddWithValue("@serviceId", serviceId);
            cmd.Parameters.AddWithValue("@startTime", startTime);
            cmd.Parameters.AddWithValue("@comment", (object?)comment ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }
}

public record ServiceDto(
    int ID,
    string Title,
    decimal Cost,
    int DurationInSeconds,
    string? Description,
    double? Discount,
    string? MainImagePath
);

public record ClientDto(
    int ID,
    string FirstName,
    string LastName,
    string? Patronymic,
    string Phone
);
