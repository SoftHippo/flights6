using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CsvHelper;
using CsvHelper.Configuration;

public static class FlightLoader
{
    public static void LoadFlightsCsv(bool excel = false, bool executeChanges = false, bool executeInserts = false)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var connectionString = config.GetConnectionString("Hippodb");

        var csvPath = Path.Combine(AppContext.BaseDirectory, "csv", "f5-log 278d0976205d81aeb40fc1891c3f0bfd_all.csv");
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
        csv.Context.RegisterClassMap<FlightMap>();
		var flights = csv.GetRecords<Flight>().ToList();

        flights = [.. flights.OrderBy(f => f.FlightId)];

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var transaction = conn.BeginTransaction();

        var certified = new List<Flight>();
        bool wroteDot = false;

        // give sequence numbers for multi flights in 1 day
        DateTime? lastDate = null;
        int seqNum = 1;
        for (int i = 0; i < flights.Count; i++)
        {
            var flight = flights[i];
            if (flight.Date == lastDate)
            {
                flights[i - 1].Sequence = seqNum;
                flight.Sequence = ++seqNum;
            }
            else
            {
                seqNum = 1;
            }
            lastDate = flight.Date;
        }

        foreach (var flight in flights)
        {
            flight.Airline = flight.FlightNumber[..2];
            flight.FlightNumber = flight.FlightNumber[2..];

            if (flight.Tail != null)
                flight.Tail = Regex.Replace(flight.Tail, @"\s*\([^)]*\)\s*", "").Trim();

            var selectCmd = new SqlCommand(@"
                SELECT Id, Date, Airline, FlightNumber, AircraftType, Tail, Origin, Destination, Class, Seat, Terminal, Gate, Notes
                FROM flights5
                WHERE Airline = @airline AND FlightNumber = @fno AND Date = @date AND Origin = @origin AND Destination = @dest
            ", conn, transaction);

            selectCmd.Parameters.AddWithValue("@airline", flight.Airline);
            selectCmd.Parameters.AddWithValue("@fno", flight.FlightNumber);
            selectCmd.Parameters.AddWithValue("@date", flight.Date);
            selectCmd.Parameters.AddWithValue("@origin", flight.Origin);
            selectCmd.Parameters.AddWithValue("@dest", flight.Destination);

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
                    Notes = readerDb.IsDBNull(12) ? null : readerDb.GetString(12)
                };

                readerDb.Close();

                var diff = FlightComparer.FlightDiffer(flight, dbFlight);
                if (diff != null)
                {
                    if (wroteDot) { Console.WriteLine(); wroteDot = false; }
                    Console.WriteLine($"Mismatch: {flight.Airline}{flight.FlightNumber} {flight.Date} {flight.Origin}-{flight.Destination} → {diff}");
                    if (executeChanges)
                    {
                        var updateCmd = new SqlCommand(@"
                            UPDATE flights5 SET
                                Id = @flight_id, AircraftType = @aircraft_type, Tail = @tail, Class = @cabin, Seat = @seat,
                                Terminal = @term, Gate = @gate, Notes = @notes, DayOrder = @dayord
                            WHERE Date = @date AND Airline = @airline AND FlightNumber = @flight_number AND Origin = @origin AND Destination = @dest
                        ", conn, transaction);

                        updateCmd.Parameters.AddWithValue("@flight_id", flight.FlightId);
                        updateCmd.Parameters.AddWithValue("@aircraft_type", flight.AircraftType ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@tail", flight.Tail ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@cabin", flight.Class ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@seat", flight.Seat ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@term", flight.Terminal ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@gate", flight.Gate ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@notes", flight.Notes ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@date", flight.Date);
                        updateCmd.Parameters.AddWithValue("@airline", flight.Airline);
                        updateCmd.Parameters.AddWithValue("@flight_number", flight.FlightNumber);
                        updateCmd.Parameters.AddWithValue("@origin", flight.Origin);
                        updateCmd.Parameters.AddWithValue("@dest", flight.Destination);
                        updateCmd.Parameters.AddWithValue("@dayord", flight.Sequence ?? (object)DBNull.Value);

                        updateCmd.ExecuteNonQuery();
                        Console.Write("\t......Updated.");
                    }
                    Console.WriteLine();
                }
                else
                {
                    certified.Add(flight);
                    Console.Write(".");
                    wroteDot = true;
                }
            }
            else
            {
                readerDb.Close();

                var seqStr = flight.Sequence != null ? $"seq: {flight.Sequence}" : "";
                if (wroteDot) { Console.WriteLine(); wroteDot = false; }
                Console.Write($"No match: {flight.FlightId} | {flight.Date.Date}\t {flight.Origin}-{flight.Destination}\t{flight.Airline}::{flight.FlightNumber}");

                if (flight.Tail == null || flight.Tail.StartsWith("*"))
                {
                    flight.Tail = null;
                }
                else
                {
                    var checkTailCmd = new SqlCommand(@"
                        select Tail from aircraft where Tail = @tail;
                    ", conn, transaction);

                    checkTailCmd.Parameters.AddWithValue("@tail", flight.Tail);

                    using var checkReader = checkTailCmd.ExecuteReader();
                    if (!checkReader.Read())
                    {
                        Console.Write($"\tMissing tail: {flight.Tail}");
                    }
                }
                if (flight.AircraftType != null && flight.AircraftType.StartsWith("*")) flight.AircraftType = null;

                var checkAirport = new SqlCommand(@"
                    select STRING_AGG(IataCode, ',') AS IataCodes from airports where IataCode = @a1 or IataCode = @a2;
                ", conn, transaction);

                checkAirport.Parameters.AddWithValue("@a1", flight.Origin);
                checkAirport.Parameters.AddWithValue("@a2", flight.Destination);

                var airportResult = ((string)checkAirport.ExecuteScalar())?.Split(',').ToList();

                if (airportResult == null || airportResult.Count < 2)
                {
                    Console.Write($"\tMissing airport {flight.Origin} or {flight.Destination} from db: {airportResult}");
                }

                var checkAirlineCmd = new SqlCommand(@"
                    select IataCode from airlines where IataCode = @al;
                ", conn, transaction);

                checkAirlineCmd.Parameters.AddWithValue("@al", flight.Airline);
                var airlineResult = (string)checkAirlineCmd.ExecuteScalar();

                if (airlineResult == null)
                {
                    Console.Write($"\tMissing airline {flight.Airline}");
                }

                if (executeChanges && executeInserts)
                {
                    var insertCmd = new SqlCommand(@"
                        INSERT INTO flights5 (Id, Date, Airline, FlightNumber, AircraftType, Tail, Origin, Destination, Class, Seat, Terminal, Gate, Notes, DayOrder)
                        VALUES (@id, @date, @airline, @fno, @type, @tail, @origin, @dest, @class, @seat, @term, @gate, @notes, @dayord)
                    ", conn, transaction);

                    insertCmd.Parameters.AddWithValue("@id", flight.FlightId);
                    insertCmd.Parameters.AddWithValue("@date", flight.Date);
                    insertCmd.Parameters.AddWithValue("@airline", flight.Airline);
                    insertCmd.Parameters.AddWithValue("@fno", flight.FlightNumber);
                    insertCmd.Parameters.AddWithValue("@type", flight.AircraftType ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@tail", flight.Tail ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@origin", flight.Origin);
                    insertCmd.Parameters.AddWithValue("@dest", flight.Destination);
                    insertCmd.Parameters.AddWithValue("@class", flight.Class ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@seat", flight.Seat ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@term", flight.Terminal ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@gate", flight.Gate ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@notes", flight.Notes ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@dayord", flight.Sequence ?? (object)DBNull.Value);

                    insertCmd.ExecuteNonQuery();
                    Console.Write("\t......Inserted.");
                }
                Console.WriteLine();
            }
        }

		var countCmd = new SqlCommand("SELECT COUNT(*) FROM flights5", conn, transaction);
        var totalFlights = (int)countCmd.ExecuteScalar();

        Console.WriteLine($"There are {totalFlights} flights in the DB.");
        Console.WriteLine($"Checked {certified.Count} flights from sheet data.");
        transaction.Commit();
		Console.WriteLine("Transaction committed");

        conn.Close();
    }
}
