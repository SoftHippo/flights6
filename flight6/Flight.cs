using CsvHelper.Configuration;

public sealed class FlightMap : ClassMap<Flight>
{
    public FlightMap()
    {
        Map(m => m.FlightId).Name("Id");
        Map(m => m.Date).Name("Date");
        Map(m => m.FlightNumber).Name("Flight Number");
        Map(m => m.AircraftType).Name("Type_ref");
        Map(m => m.Tail).Name("Tail_ref");
        Map(m => m.Origin).Name("Origin");
        Map(m => m.Destination).Name("Dest");
        Map(m => m.Class).Name("Class");
        Map(m => m.Seat).Name("Seat");
        Map(m => m.Terminal).Name("Term");
        Map(m => m.Gate).Name("Gate");
        Map(m => m.Notes).Name("Notes");
    }
}

public class Flight
{
    public int FlightId { get; set; }
    public required DateTime Date { get; set; }
    public required string Airline { get; set; }
    public required string FlightNumber { get; set; }
    public string? AircraftType { get; set; }
    public string? Tail { get; set; }
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public string? Class { get; set; }
    public string? Seat { get; set; }
    public string? Terminal { get; set; }
    public string? Gate { get; set; }
    public string? Notes { get; set; }
    public int? Sequence { get; set; }
}
