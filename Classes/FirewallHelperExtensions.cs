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

public static class IAddressExtensions
{
    public enum IPAddressType
    {
        IPv4,
        IPv6,
        Invalid
    }

    //    public static IPAddressType AddressType( this IPAddress iPAddress )
    //    {
    //        return IPAddressType.IPv4;
    //string tmpAddress = string.Empty;
    //if ( string.IsNullOrEmpty( iPAddress ) && IPAddressRange is not null )
    //    tmpAddress = IPAddressRange!.StartAddress.ToString( );
    //else if ( !string.IsNullOrEmpty( IPAddressBatch ) )
    //    tmpAddress = IPAddressSet[ 0 ].ToString( );
    //else
    //    tmpAddress = IPAddress!;


    //if ( tmpAddress.IndexOf( ':' ) > 0 )
    //    return IPAddressType.IPv6;
    //else
    //    return IPAddressType.IPv4;
    //    }

    //public static bool IPAddressValidated 
}