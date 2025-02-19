using System.Net.NetworkInformation;

namespace BlocklistManager.Classes;

internal sealed class DeviceNetworkInterface
{
    internal string Name { get; set; } = "";
    internal string Description { get; set; } = "";
    internal PhysicalAddress? PhysicalAddress { get; set; }
    internal required NetworkInterfaceType InterfaceType { get; set; }
    internal OperationalStatus Status { get; set; } = OperationalStatus.Unknown;
    internal string Index { get; set; } = "";
    internal string IPv6Index { get; set; } = "";
    internal string AdapterFlags { get; set; } = "";
    internal string Id { get; set; } = "";
}
