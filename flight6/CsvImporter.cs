using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public static class CsvImporter
{
    public static void InsertCsvToDatabase(string csvFileName, string tableName, Dictionary<string, string> columnMapping)
    {
        // Load connection string from appsettings.json
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var connectionString = config.GetConnectionString("Hippodb");

        // Build full path to CSV file
        var csvPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", csvFileName);
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }

        using var conn = new SqlConnection(connectionString);
        conn.Open();

        using var reader = new StreamReader(csvPath);
        var headerLine = reader.ReadLine();
        if (headerLine == null) throw new Exception("CSV file is empty.");

        var csvHeaders = headerLine.Split(',');

        // Prepare SQL insert statement
        var dbColumns = new List<string>();
        var csvIndices = new List<int>();

        foreach (var kvp in columnMapping)
        {
            var csvIndex = Array.IndexOf(csvHeaders, kvp.Key);
            if (csvIndex == -1)
                throw new Exception($"CSV column '{kvp.Key}' not found in header.");

            dbColumns.Add(kvp.Value);
            csvIndices.Add(csvIndex);
        }

        var columnList = string.Join(", ", dbColumns);
        var paramList = string.Join(", ", dbColumns.ConvertAll(c => "@" + c));

        var insertSql = $"INSERT INTO {tableName} ({columnList}) VALUES ({paramList})";

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');

            using var cmd = new SqlCommand(insertSql, conn);
            for (int i = 0; i < dbColumns.Count; i++)
            {
                cmd.Parameters.AddWithValue("@" + dbColumns[i], values[csvIndices[i]]);
            }

            cmd.ExecuteNonQuery();
        }

        Console.WriteLine("CSV data inserted successfully.");
    }
}
