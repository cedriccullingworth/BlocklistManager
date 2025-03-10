namespace BlocklistManager.Classes;

/// <summary>
/// Contains the configuration data for the BlocklistData class.
/// </summary>
internal sealed class BlocklistDataConfig
{
    /// <summary>
    /// The host server's domain name or IP address.
    /// </summary>
    internal string HostAddress { get; set; } = "localhost";

    /// <summary>
    /// The host server's API port number.
    /// </summary>
    internal string HostPort { get; set; } = "44318";
}
