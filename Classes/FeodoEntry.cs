namespace BlocklistManager;

internal class FeodoEntry
{
    //internal FeodoEntry()
    //{

    //}

    internal string ip_address { get; set; } = string.Empty;

    internal ushort[] ports { get; set; } = [];

    internal string? status { get; set; }

    internal string? hostname { get; set; } = null;

    internal int? as_number { get; set; } = 0;

    internal string? as_name { get; set; }

    internal string? country { get; set; }

    internal string? first_seen { get; set; }

    internal string? last_online { get; set; }

    internal string? malware { get; set; }
}
