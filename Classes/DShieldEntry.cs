namespace BlocklistManager;

internal class DShieldEntry
{
    internal string rangeStart { get; set; } = string.Empty;

    internal string rangeEnd { get; set; } = string.Empty;

    internal ushort Subnet { get; set; }

    internal long TargetsScanned { get; set; }

    internal string? NetworkName { get; set; }

    internal string? Country { get; set; }

    internal string? EmailAddress { get; set; }
}
