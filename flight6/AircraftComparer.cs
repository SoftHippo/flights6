public static class AircraftComparer
{
    public static string? AircraftDiffer(Aircraft a, Aircraft b)
    {
        var diffs = new List<string>();

        if (a.IcaoCode != b.IcaoCode) diffs.Add($"ICAO: {a.IcaoCode} ≠ {b.IcaoCode}");
        if (a.SerialNumber != b.SerialNumber) diffs.Add($"Serial: {a.SerialNumber} ≠ {b.SerialNumber}");
        if (a.Year != b.Year) diffs.Add($"Year: {a.Year} ≠ {b.Year}");
        if (a.Month != b.Month) diffs.Add($"Month: {a.Month} ≠ {b.Month}");
        if (a.ModeS != b.ModeS) diffs.Add($"ModeS: {a.ModeS} ≠ {b.ModeS}");
        if (a.Model != b.Model) diffs.Add($"Model: {a.Model} ≠ {b.Model}");
        if (a.Notes != b.Notes) diffs.Add($"Notes: {a.Notes} ≠ {b.Notes}");

        return diffs.Count > 0 ? string.Join("; ", diffs) : null;
    }
}
