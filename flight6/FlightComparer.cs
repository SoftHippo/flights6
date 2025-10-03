public static class FlightComparer
{
    public static string? FlightDiffer(Flight a, Flight b)
    {
        var diffs = new List<string>();

        if (a.AircraftType != b.AircraftType)
        {
            if (!((a.AircraftType != null && a.AircraftType.StartsWith("*")) || (b.AircraftType != null && b.AircraftType.StartsWith("*"))))
            {
                diffs.Add($"AircraftType: {a.AircraftType} ≠ {b.AircraftType}");
            }
        }

        if (a.Tail != b.Tail)
        {
            if (!((a.Tail != null && a.Tail.StartsWith("*")) || (b.Tail != null && b.Tail.StartsWith("*"))))
            {
                diffs.Add($"Tail: {a.Tail} ≠ {b.Tail}");
            }
        }

        if (a.Class != b.Class) diffs.Add($"Class: {a.Class} ≠ {b.Class}");
        if (a.Seat != b.Seat) diffs.Add($"Seat: {a.Seat} ≠ {b.Seat}");
        if (a.Terminal != b.Terminal) diffs.Add($"Terminal: {a.Terminal} ≠ {b.Terminal}");
        if (a.Gate != b.Gate) diffs.Add($"Gate: {a.Gate} ≠ {b.Gate}");
        if (a.Notes != b.Notes) diffs.Add($"Notes: {a.Notes} ≠ {b.Notes}");

        return diffs.Count > 0 ? string.Join("; ", diffs) : null;
    }
}
