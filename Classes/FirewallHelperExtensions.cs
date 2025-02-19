using System;
using System.Net;

using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

public static class IPRangeExtensions
{
    public static IPRange FromSubnet( this IPRange owner, IPAddress subnet )
    {
        string[] parts = subnet.ToString( ).Split( '/' );
        if ( parts.Length != 2 )
            throw new ArgumentException( "Invalid subnet format." );
        IPAddress startAddress = IPAddress.Parse( parts[ 0 ] );
        IPAddress endAddress = IPAddress.Parse( parts[ 1 ] );
        return new IPRange( startAddress, endAddress );
    }
}
