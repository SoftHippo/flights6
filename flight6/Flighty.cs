using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CsvHelper;
using CsvHelper.Configuration;

public static class Flighty
{
	public static string flightyDate = "2025-10-02";

	public static void CheckFlightyCsv()
	{
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();
		var connectionString = config.GetConnectionString("Hippodb");

		var csvPath = Path.Combine(AppContext.BaseDirectory, "csv", $"FlightyExport-{flightyDate}.csv");
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
		csv.Context.RegisterClassMap<FlightyCsvMap>();
		var flights = csv.GetRecords<Flight>().ToList();

		flights = [.. flights.OrderBy(f => f.Date)];

		using var conn = new SqlConnection(connectionString);
		conn.Open();
		using var transaction = conn.BeginTransaction();

		bool wroteDot = false;
		foreach (var flight in flights)
		{
			var selectCmd = new SqlCommand(@"
                SELECT Id, Date, Airline, FlightNumber, AircraftType, Tail, Origin, Destination, Class, Seat, Terminal, Gate, Notes, at.Name as ATName
                FROM flights5
				join airlines al on al.IataCode = flights5.Airline
				left join aircraft_types at on at.IcaoCode = flights5.AircraftType
                WHERE
					al.IcaoCode = @airline_icao AND
					FlightNumber = @fno AND
					Date = @date AND
					Origin = @origin AND
					(Destination = @dest or Destination = @diversion)
            ", conn, transaction);
			selectCmd.Parameters.AddWithValue("@airline_icao", flight.Airline);
			selectCmd.Parameters.AddWithValue("@fno", flight.FlightNumber);
			selectCmd.Parameters.AddWithValue("@date", flight.Date);
			selectCmd.Parameters.AddWithValue("@origin", flight.Origin);
			selectCmd.Parameters.AddWithValue("@dest", flight.Destination);
			selectCmd.Parameters.AddWithValue("@diversion", flight.Diversion ?? (object)DBNull.Value);

			using var readerDb = selectCmd.ExecuteReader();
			if (readerDb.Read())
			{
				var dbFlight = new Flight
				{
					FlightId = readerDb.GetInt32(0),
					Date = readerDb.GetDateTime(1),
					Airline = readerDb.GetString(2),
					FlightNumber = readerDb.GetString(3),
					AircraftType = readerDb.IsDBNull(4) ? null : readerDb.GetString(4),
					Tail = readerDb.IsDBNull(5) ? null : readerDb.GetString(5),
					Origin = readerDb.GetString(6),
					Destination = readerDb.GetString(7),
					Class = readerDb.IsDBNull(8) ? null : readerDb.GetString(8),
					Seat = readerDb.IsDBNull(9) ? null : readerDb.GetString(9),
					Terminal = readerDb.IsDBNull(10) ? null : readerDb.GetString(10),
					Gate = readerDb.IsDBNull(11) ? null : readerDb.GetString(11),
					Notes = readerDb.IsDBNull(12) ? null : readerDb.GetString(12),
					AircraftModel = readerDb.IsDBNull(13) ? null : readerDb.GetString(13),
				};

				readerDb.Close();
				flight.Airline = dbFlight.Airline;

				var diff = FlightComparer.FlightDiffer(flight, dbFlight, flighty: true);
				if (diff != null)
				{
					if (wroteDot) { Console.WriteLine(); wroteDot = false; }
					Console.WriteLine($"Mismatch: {flight.Airline}{flight.FlightNumber} {flight.Date} {flight.Origin}-{flight.Destination} → {diff}");
				}
				else
				{
					Console.Write(".");
					wroteDot = true;
				}
			}
			else
			{
				if (flight.Date > DateTime.Now.Date) { continue; }
				if (wroteDot) { Console.WriteLine(); wroteDot = false; }
				Console.WriteLine($"MISSING: {flight.Airline}{flight.FlightNumber} {flight.Date} {flight.Origin}-{flight.Destination}");
			}
		}
		Console.WriteLine();
	}
}
