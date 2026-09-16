using Microsoft.Data.SqlClient;

namespace CursoClaudeCode.Database;

public sealed class SqlServerConnector
{
    private readonly string connectionString;

    private SqlServerConnector(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public static SqlServerConnector FromEnvironment(string? projectRoot = null)
    {
        var settings = LoadDotEnv(projectRoot);

        foreach (var key in new[] { "MSSQL_SA_PASSWORD", "SQLSERVER_HOST_PORT", "SQLSERVER_DATABASE" })
        {
            var environmentValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                settings[key] = environmentValue;
            }
        }

        return new SqlServerConnector(BuildConnectionString(settings));
    }

    public static string BuildConnectionString(IReadOnlyDictionary<string, string?> settings)
    {
        var password = GetRequired(settings, "MSSQL_SA_PASSWORD");
        var port = GetPort(settings);
        var database = GetOptional(settings, "SQLSERVER_DATABASE") ?? "master";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"localhost,{port}",
            InitialCatalog = database,
            UserID = "sa",
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5,
            ApplicationName = "CursoClaudeCode"
        };

        return builder.ConnectionString;
    }

    public async Task VerifyConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync("SELECT 1;", cancellationToken: cancellationToken);
        if (Convert.ToInt32(result) != 1)
        {
            throw new InvalidOperationException("SQL Server no devolvió la respuesta esperada.");
        }
    }

    public async Task VerifyFullAccessAsync(CancellationToken cancellationToken = default)
    {
        var tableName = $"AgentConnectorVerification_{Guid.NewGuid():N}";
        var tableIdentifier = $"[dbo].[{tableName}]";
        var tableCreated = false;

        try
        {
            await ExecuteNonQueryAsync(
                $"CREATE TABLE {tableIdentifier} (Id INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL);",
                cancellationToken: cancellationToken);
            tableCreated = true;

            var inserted = await ExecuteNonQueryAsync(
                $"INSERT INTO {tableIdentifier} (Id, Name) VALUES (@id, @name);",
                new[]
                {
                    new SqlParameter("@id", 1),
                    new SqlParameter("@name", "connector")
                },
                cancellationToken);

            var updated = await ExecuteNonQueryAsync(
                $"UPDATE {tableIdentifier} SET Name = @name WHERE Id = @id;",
                new[]
                {
                    new SqlParameter("@id", 1),
                    new SqlParameter("@name", "connector-updated")
                },
                cancellationToken);

            var rows = await ExecuteQueryAsync(
                $"SELECT Id, Name FROM {tableIdentifier} WHERE Name = @name;",
                new[] { new SqlParameter("@name", "connector-updated") },
                cancellationToken);

            var deleted = await ExecuteNonQueryAsync(
                $"DELETE FROM {tableIdentifier} WHERE Id = @id;",
                new[] { new SqlParameter("@id", 1) },
                cancellationToken);

            await ExecuteNonQueryAsync(
                $"DROP TABLE {tableIdentifier};",
                cancellationToken: cancellationToken);
            tableCreated = false;

            if (inserted != 1 || updated != 1 || rows.Count != 1 || deleted != 1)
            {
                throw new InvalidOperationException("La verificación de acceso completo no devolvió los resultados esperados.");
            }
        }
        catch
        {
            if (tableCreated)
            {
                try
                {
                    await ExecuteNonQueryAsync(
                        $"DROP TABLE {tableIdentifier};",
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Preserve the original database operation error.
                }
            }

            throw;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await ExecuteNonQueryAsync(connection, null, sql, parameters, cancellationToken);
    }

    public async Task<object?> ExecuteScalarAsync(
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await ExecuteScalarAsync(connection, null, sql, parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteQueryAsync(
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateCommand(connection, null, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var column = 0; column < reader.FieldCount; column++)
            {
                row[reader.GetName(column)] = reader.IsDBNull(column) ? null : reader.GetValue(column);
            }

            rows.Add(row);
        }

        return rows;
    }

    public Task<object?> ExecuteReadOnlyScalarAsync(
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSql = sql.TrimStart();
        if (!normalizedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "El conector del agente solo permite consultas SELECT.",
                nameof(sql));
        }

        return ExecuteScalarAsync(sql, parameters, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        IEnumerable<SqlParameter>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        IEnumerable<SqlParameter>? parameters)
    {
        var command = new SqlCommand(sql, connection, transaction)
        {
            CommandTimeout = 5
        };

        if (parameters is not null)
        {
            command.Parameters.AddRange(parameters.ToArray());
        }

        return command;
    }

    private static Dictionary<string, string?> LoadDotEnv(string? projectRoot)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var root = projectRoot ?? FindProjectRoot();
        var envPath = Path.Combine(root, ".env");

        if (!File.Exists(envPath))
        {
            return settings;
        }

        foreach (var line in File.ReadLines(envPath))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmedLine.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmedLine[..separator].Trim();
            var value = trimmedLine[(separator + 1)..].Trim();
            if (value.Length >= 2 && value[0] == value[^1] && (value[0] == '\'' || value[0] == '"'))
            {
                value = value[1..^1];
            }

            settings[key] = value;
        }

        return settings;
    }

    private static string FindProjectRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, ".env")) ||
                    File.Exists(Path.Combine(directory.FullName, "CursoClaudeCode.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string GetRequired(IReadOnlyDictionary<string, string?> settings, string key)
    {
        var value = GetOptional(settings, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Falta la variable de configuración {key}.");
        }

        return value;
    }

    private static string? GetOptional(IReadOnlyDictionary<string, string?> settings, string key) =>
        settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static int GetPort(IReadOnlyDictionary<string, string?> settings)
    {
        var rawPort = GetOptional(settings, "SQLSERVER_HOST_PORT") ?? "11433";
        if (!int.TryParse(rawPort, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("SQLSERVER_HOST_PORT debe ser un puerto entre 1 y 65535.");
        }

        return port;
    }
}
