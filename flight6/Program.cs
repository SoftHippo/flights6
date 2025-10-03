class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a command. Example: dotnet run import");
            return;
        }

        var command = args[0];

        switch (command)
        {
			case "IngestAircraft":
				AircraftLoader.LoadAircraftCsv(false, false);
				break;

			case "IngestAircraft-commit":
				AircraftLoader.LoadAircraftCsv(true, false);
				break;

			case "IngestAircraft-write":
				AircraftLoader.LoadAircraftCsv(true, true);
				break;

			case "IngestFlights":
				FlightLoader.LoadFlightsCsv(false, false, false);
				break;

			case "IngestFlights-commit":
				FlightLoader.LoadFlightsCsv(false, false, true);
				break;

			case "IngestFlights-write":
				FlightLoader.LoadFlightsCsv(false, true, true);
				break;

			case "Flighty":
				Flighty.CheckFlightyCsv();
				break;

            // case "firstsetup":
			//     RunCsvImport();
			//     break;

			default:
                Console.WriteLine($"Unknown command: {command}");
                Console.WriteLine("Available commands: import, query, health");
                break;
        }
    }

	// static void RunCsvImport()
	// {
	// 	Console.WriteLine("Running CSV import...");
	// 	CsvImporter.InsertCsvToDatabase("Flights4-aircraft.csv", "dbo.aircraft_types", new Dictionary<string, string>()
	// 	{
	// 		{ "IcaoCode", "IcaoCode" },
	// 		{ "Name", "Name" },
	// 	});
	// 	CsvImporter.InsertCsvToDatabase("Flights4-airlines.csv", "dbo.airlines", new Dictionary<string, string>()
	// 	{
	// 		{ "IataCode", "IataCode" },
	// 		{ "ThreeDigitCode", "ThreeDigitCode" },
	// 		{ "IcaoCode", "IcaoCode" },
	// 		{ "Country", "Country" },
	// 		{ "Name", "Name" },
	// 	});
	// 	CsvImporter.InsertCsvToDatabase("Flights4-airports.csv", "dbo.airports", new Dictionary<string, string>()
	// 	{
	// 		{ "IataCode", "IataCode" },
	// 		{ "City", "City" },
	// 		{ "Country", "Country" },
	// 	});
	// }
}
