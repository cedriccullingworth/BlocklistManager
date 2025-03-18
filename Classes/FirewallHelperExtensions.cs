namespace BlocklistManager.Classes;

/// <summary>
///  Any extension methods or properties that are used to extend the functionality of the FirewallHelper library.
/// </summary>
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