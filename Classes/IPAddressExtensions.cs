using System.Collections.Generic;
using System.Linq;
using System.Net;

using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;
public static class IPAddressExtensions
{
    public static List<IPRange> ConvertToIPRanges( ref List<CandidateEntry> candidateEntries, out int numberConverted )
    {
        numberConverted = 0;
        List<IPRange> ipRanges = new( );
        List<IPAddress> ipAddresses = candidateEntries
            .Where( w => w.IPAddress is not null && w.IPAddressSet.Length == 0 && w.IPAddressRange is null && w.AddressType == Maintain.IPAddressType.IPv4 )
            .Select( s => IPAddress.Parse( s.IPAddress! ) )
            //            .Select( s => new { Address = s, Readable = s.ToString( ) } )
            .OrderBy( o => o.Address )
            .ToList( );

        var testy = ipAddresses.Select( s => s.ToString( ) );
        if ( ipAddresses == null || ipAddresses.Count == 0 )
        {
            return ipRanges;
        }

        //ipAddresses.Sort( ( a, b ) => a.GetAddressBytes( )
        //                        .AsSpan<byte>( )
        //                        .SequenceCompareTo( b.GetAddressBytes( ) ) );

        //IPAddress start = ipAddresses[ 0 ];
        //IPAddress end = start;

        //for ( int i = 1; i < ipAddresses.Count; i++ )
        //{
        //    IPAddress current = ipAddresses[ i ];
        //    if ( IsSequential( end, current ) )
        //    {
        //        end = current;
        //    }
        //    else
        //    {
        //        ipRanges.Add( new IPRange( start, end ) );
        //        start = current;
        //        end = start;
        //    }
        //}

        //ipRanges.Add( new IPRange( start, end ) );
        return ipRanges;
    }

    private static bool IsSequential( IPAddress first, IPAddress second )
    {
        byte[] firstBytes = first.GetAddressBytes( );
        byte[] secondBytes = second.GetAddressBytes( );

        for ( int i = firstBytes.Length - 1; i >= 0; i-- )
        {
            if ( firstBytes[ i ] + 1 == secondBytes[ i ] )
            {
                return true;
            }

            //if ( firstBytes[ i ] != secondBytes[ i ] )
            //{
            return false;
            //}
        }

        return false;
    }
}
