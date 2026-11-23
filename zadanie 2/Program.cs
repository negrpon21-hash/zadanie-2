using MySqlConnector;
using System.IO;
using System.Text;

namespace zadanie_2;

internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var db = new DatabaseHelper();

        bool connected = await db.TestConnectionAsync();
        if (!connected)
        {
            MessageBox.Show(
                "Не удалось подключиться к серверу базы данных. Проверьте подключение к сети.",
                "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        bool schemaCreated = await db.CreateSchemaIfNotExistsAsync();
        if (!schemaCreated)
        {
            MessageBox.Show(
                "Не удалось создать схему базы данных.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var dumpPath = @"C:\Users\dayn\Desktop\drive-download-20261122T150421Z-1-001\Сессия 1\Dump20220408.sql";
        await ImportSqlDumpAsync(db, dumpPath);

        Application.Run(new ServiceForm(db));
    }

    static async Task ImportSqlDumpAsync(DatabaseHelper db, string dumpPath)
    {
        if (!File.Exists(dumpPath))
        {
            return;
        }

        var sql = File.ReadAllText(dumpPath, Encoding.UTF8);

        var commands = sql.Split(new[] { ";\n", ";\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        using var conn = db.CreateConnection();
        await conn.OpenAsync();

        foreach (var cmdText in commands)
        {
            var trimmed = cmdText.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            if (trimmed.StartsWith("--")) continue;
            if (trimmed.StartsWith("/*!")) continue;

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = trimmed + ";";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
            }
        }
    }
}
