using System.Collections.Specialized;

public static class FlightComparer
{
    public static Dictionary<string, string> flightClassMap = new()
    {
        {"ECONOMY", "e"},
        {"BUSINESS", "b"},
        {"FIRST", "f"},
        {"PREMIUM_ECONOMY", "p"},

    };
    public static string? FlightDiffer(Flight a, Flight b, bool flighty = false)
    {
        var diffs = new List<string>();
        if (flighty && a.Class != null) { a.Class = flightClassMap[a.Class]; }
        if (flighty && a.Tail != null && b.Tail != null) { a.Tail = a.Tail.Replace("-", ""); b.Tail = b.Tail.Replace("-", ""); }
        if (flighty && b.Terminal == "-") { b.Terminal = null; }
        if (flighty && a.Terminal == "Main") { a.Terminal = null;}
        if (flighty && a.Terminal == "INTL") { a.Terminal = "International"; }
        if (flighty && a.Terminal == "I") { a.Terminal = "International"; }
        if (flighty && a.Gate == null) { b.Gate = null; }
        if (flighty && a.Terminal == null) { b.Terminal = null; }

        if (a.AircraftType != b.AircraftType)
        {
            if (flighty)
            {
                if (a.AircraftModel != null && b.AircraftModel != null && a.AircraftModel[..2] != b.AircraftModel[..2])
                {
                    diffs.Add($"AircraftType: {a.AircraftModel} vs {b.AircraftModel}");
                }
            }
            else if (!((a.AircraftType != null && a.AircraftType.StartsWith("*")) || (b.AircraftType != null && b.AircraftType.StartsWith("*"))))
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
        if (a.Notes != b.Notes & !flighty) diffs.Add($"Notes: {a.Notes} ≠ {b.Notes}");

        return diffs.Count > 0 ? string.Join("; ", diffs) : null;
    }
}
