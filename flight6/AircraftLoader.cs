using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;


public class NullIfEmptyConverter : StringConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        return string.IsNullOrWhiteSpace(text) ? null : base.ConvertFromString(text, row, memberMapData);
    }
}

public static class AircraftLoader
{
	public static void LoadAircraftCsv(bool executeChanges = false, bool executeInserts = false)
	{
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
		var connectionString = config.GetConnectionString("Hippodb");

		var csvPath = Path.Combine(AppContext.BaseDirectory, "aircraft 27ed0976205d818eb340e01590bdc127_all.csv");
		if (!File.Exists(csvPath))
		{
			Console.WriteLine($"CSV file not found: {csvPath}");
			return;
		}

		using var reader = new StreamReader(csvPath);
		using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			HasHeaderRecord = true,
			IgnoreBlankLines = true,
			TrimOptions = TrimOptions.Trim
		});

		csv.Context.TypeConverterCache.AddConverter<string>(new NullIfEmptyConverter());

		var aircraftList = csv.GetRecords<Aircraft>().ToList();

		using var conn = new SqlConnection(connectionString);
		conn.Open();
		using var transaction = conn.BeginTransaction();

		var certified = new List<Aircraft>();

		foreach (var aircraft in aircraftList)
		{
			if (aircraft.Tail.StartsWith("*"))
			{
				continue;
			}
			if (aircraft.Month == 0) { aircraft.Month = null; }
			if (aircraft.Year == 0) { aircraft.Year = null; }

			var selectCmd = new SqlCommand(@"
                SELECT Tail, IcaoCode, SerialNumber, Year, Month, ModeS, Model, Notes
                FROM Aircraft
                WHERE Tail = @tail", conn, transaction
			);

			selectCmd.Parameters.AddWithValue("@tail", aircraft.Tail);

			using var readerDb = selectCmd.ExecuteReader();
			if (readerDb.Read())
			{
				var dbAircraft = new Aircraft
				{
					Tail = readerDb.GetString(0),
					IcaoCode = readerDb.GetString(1),
					SerialNumber = readerDb.IsDBNull(2) ? null : readerDb.GetString(2),
					Year = readerDb.IsDBNull(3) ? null : readerDb.GetInt32(3),
					Month = readerDb.IsDBNull(4) ? null : readerDb.GetInt32(4),
					ModeS = readerDb.IsDBNull(5) ? null : readerDb.GetString(5),
					Model = readerDb.IsDBNull(6) ? null : readerDb.GetString(6),
					Notes = readerDb.IsDBNull(7) ? null : readerDb.GetString(7)
				};

				readerDb.Close();

				var diff = AircraftComparer.AircraftDiffer(aircraft, dbAircraft);
				if (diff != null)
				{
					Console.Write($"Mismatch for {aircraft.Tail} → {diff}");
					if (executeChanges)
					{
						var updateCmd = new SqlCommand(@"
                            UPDATE Aircraft SET
                                IcaoCode = @icao, SerialNumber = @serial, Year = @year, Month = @month,
                                ModeS = @modeS, Model = @model, Notes = @notes
                            WHERE Tail = @tail", conn, transaction
						);

						updateCmd.Parameters.AddWithValue("@icao", aircraft.IcaoCode ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@serial", aircraft.SerialNumber ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@year", aircraft.Year ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@month", aircraft.Month ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@modeS", aircraft.ModeS ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@model", aircraft.Model ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@notes", aircraft.Notes ?? (object)DBNull.Value);
						updateCmd.Parameters.AddWithValue("@tail", aircraft.Tail);

						updateCmd.ExecuteNonQuery();
						Console.Write("\t......Updated.");
					}
					Console.WriteLine();
				}
				else
				{
					certified.Add(aircraft);
				}
			}
			else
			{
				readerDb.Close();
				Console.Write($"No match, inserting: {aircraft.Tail}");
				if (executeChanges && executeInserts)
				{
					var insertCmd = new SqlCommand(@"
                        INSERT INTO Aircraft (Tail, IcaoCode, SerialNumber, Year, Month, ModeS, Model, Notes)
                        VALUES (@tail, @icao, @serial, @year, @month, @modeS, @model, @notes)
					", conn, transaction);

					insertCmd.Parameters.AddWithValue("@tail", aircraft.Tail);
					insertCmd.Parameters.AddWithValue("@icao", aircraft.IcaoCode ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@serial", aircraft.SerialNumber ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@year", aircraft.Year ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@month", aircraft.Month ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@modeS", aircraft.ModeS ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@model", aircraft.Model ?? (object)DBNull.Value);
					insertCmd.Parameters.AddWithValue("@notes", aircraft.Notes ?? (object)DBNull.Value);

					insertCmd.ExecuteNonQuery();
					Console.Write("\t......Inserted.");
				}
				Console.WriteLine();
			}
		}

		var countCmd = new SqlCommand("SELECT COUNT(*) FROM Aircraft", conn, transaction);
		var totalAircraft = (int)countCmd.ExecuteScalar();

		Console.WriteLine($"There are {totalAircraft} aircraft in the DB.");
		Console.WriteLine($"Checked {certified.Count} aircraft from sheet data.");

		transaction.Commit();
		Console.WriteLine("Transaction committed");
		conn.Close();
	}
}
