using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

using static BlocklistManager.Classes.IAddressExtensions;

namespace BlocklistManager.Classes;

/// <summary>
/// The class defining a firewall rule candidate entry
/// </summary>
/// <param name="siteName">The name of the download site. Windows Firewall and are created with rule names composed by appending '_Blocklist' to this name</param>
/// <param name="blocklistFileName">The file name containing the source data. Currently only used for debugging.</param>
/// <param name="address">The IP address to block (as a string)</param>
/// <param name="subnetSuffix">Any subnet suffix which may have been inculded in the downloaded data. This will be converted to an IP address range</param>
/// <param name="addressRange">The IP address range to block</param>
/// <param name="addressSet">An IP address or IP address range array containing up to 10,000 entries (Win 11) or 1,000 entries (<= Win 10) after consolidation</param>
public sealed record class CandidateEntry( string? siteName, string blocklistFileName, string? address, int? subnetSuffix, IPRange? addressRange, IAddress[] addressSet/*, ushort[] portsArg, FirewallProtocol protocolArg*/ ) : IComparable<CandidateEntry>, IDisposable
{
    /// <summary>
    /// The private copy of the sort order for the IP address
    /// </summary>
    private long[] _sort = [ 0, 0, 0, 0 ];

    /// <summary>
    /// The name of the download site. Windows Firewall and are created with rule names composed by appending '_Blocklist' to this name
    /// </summary>
    [Display( Description = "Name" )]
    [Length( 2, 50 )]
    public string? Name { get; set; } = siteName;

    /// <summary>
    /// The IP address to block (as a string), NULL when IP address range is used
    /// </summary>
    [Display( Description = "IP Address" )]
    public string? IPAddress { get; set; } = address;

    /// <summary>
    /// Any subnet suffix which may have been inculded in the downloaded data. This will be converted to an IP address range
    /// </summary>
    [Display( Description = "Subnet if applicable" )]
    public int? Subnet { get; set; } = subnetSuffix;

    /// <summary>
    /// The IP address range to block, NULL when IP address is used
    /// </summary>
    [Display( Description = "IP Address Range" )]
    public IPRange? IPAddressRange { get; set; } = addressRange;

    /// <summary>
    /// An IP address or IP address range array containing up to 10,000 entries (Win 11) or 1,000 entries (<= Win 10) after consolidation
    /// </summary>
    [Display( AutoGenerateField = true, Description = "IP Address Set", Name = "IPAddressSet" )]
    public IAddress[] IPAddressSet { get; set; } = addressSet;

    /// <summary>
    /// IPAddressSet as a string for display purposes
    /// </summary>
    public string? IPAddressBatch => Maintain.IAddressesToString( IPAddressSet );

    /// <summary>
    /// The type of IP address: IPv4, IPv6, or Invalid
    /// </summary>
    internal IPAddressType AddressType { get; set; } = IPAddressType.Invalid;

    /// <summary>
    /// The name of the blocklist file that this entry came from
    /// </summary>
    public string FileName { get; set; } = blocklistFileName;

    /// <summary>
    /// New: Simply record whether the IP address(es) or ranger has been validated
    /// </summary>
    public bool Validated { get; set; }

    [Display( AutoGenerateField = false )]
    /// <summary>
    /// An array to help sort IP addresses
    /// </summary>
    internal long[] Sort => DetermineSort( );

    internal long Sort0 => Sort[ 0 ];
    internal long Sort1 => Sort.Length > 0 ? Sort[ 1 ] : 0;
    internal long Sort2 => Sort.Length > 1 ? Sort[ 2 ] : 0;
    internal long Sort3 => Sort.Length > 2 ? Sort[ 3 ] : 0;

    private IPAddressType DetermineAddressType( )
    {
        string tmpAddress = string.Empty;
        if ( string.IsNullOrEmpty( IPAddress ) && IPAddressRange is not null )
            tmpAddress = IPAddressRange!.StartAddress.ToString( );
        else if ( !string.IsNullOrEmpty( IPAddressBatch ) )
            tmpAddress = IPAddressSet[ 0 ].ToString( );
        else
            tmpAddress = IPAddress!;


        if ( tmpAddress.IndexOf( ':' ) > 0 )
            return IPAddressType.IPv6;
        else
            return IPAddressType.IPv4;
    }

    /// <summary>
    /// A single IP address representing the IP address, address range or IPAddressBatch as an IPAddress
    /// </summary>
    private IPAddress? RepresentativeIPAddress
    {
        get
        {
            string addressToUse = string.Empty;
            if ( !string.IsNullOrEmpty( IPAddress ) )
            {
                System.Net.IPAddress? address = null;
                if ( System.Net.IPAddress.TryParse( IPAddress!.Split( ';' )[ 0 ], out address ) )
                {
                    return address;
                }

                return address;
            }
            else if ( !string.IsNullOrEmpty( IPAddressBatch ) )
                return System.Net.IPAddress.Parse( IPAddressBatch!.Split( ';' )[ 0 ] );
            else if ( IPAddressRange is not null && IPAddressRange.StartAddress is not null )
                return IPAddressRange.StartAddress;
            else return null;
        }
    }

    /// <summary>
    /// Internal: Calculates values for the Sort property
    /// </summary>
    /// <returns>Values for the Sort property</returns>
    private long[] DetermineSort( )
    {
        if ( _sort.Sum( ) > 0 ) // _sort[ 0 ] > 0 && _sort[ 3 ] > 0 )
            return _sort;
        else
        {
            _sort = [ 0, 0, 0, 0 ];
            IPAddress? address = RepresentativeIPAddress;
            CultureInfo culture = CultureInfo.InvariantCulture;

            if ( address is not null )
            {
                if ( AddressType == IPAddressType.IPv4 )
                {
                    _sort = address.GetAddressBytes( )
                                   .Select( s => Convert.ToInt64( s ) )
                                   .ToArray( );
                }
                else if ( AddressType == IPAddressType.IPv6 )
                {
                    //byte[] bytes = address.GetAddressBytes( );
                    string[] addressParts = address!.ToString( )
                                                   .Split( ':' )
                                                   .Where( w => !string.IsNullOrEmpty( w ) )
                                                   .ToArray( );

                    List<long> sort = addressParts
                            .Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber, culture ) )
                            .Where( w => w != 0 )
                            .ToList( );

                    while ( sort.Count < 4 )
                        sort.Add( 0 );

                    _sort = [ .. sort ];
                    //Array.Reverse( bytes ); // Ensure correct endianness if needed
                    //ReadOnlySpan<byte> roBytes = bytes.AsSpan( );
                    //_sort[0] = BitConverter.ToInt64( bytes );
                }
                else
                    _sort = [ 0, 0, 0, 0 ];
            }

            return _sort;
        }
    }

    /// <summary>
    /// Compares this CandidateEntry to another CandidateEntry
    /// </summary>
    /// <param name="other">The entry to compare to</param>
    /// <returns>-1 if 'other' follows this, 0 it equal, 1 if this follows 'other'</returns>
    public int CompareTo( CandidateEntry? other )
    {
        int ret = 1;
        List<CandidateEntry> compare = [ this ];

        if ( other is not null )
        {
            ret = -1;
            compare.Add( other );
            if ( Equals( other ) )
                ret = 0;
            else if ( Equals( compare[ 1 ] ) )  // this follows other
                ret = 1;
        }

        return ret;
    }

    /// <summary>
    /// Compares this CandidateEntry to another CandidateEntry
    /// </summary>
    /// <param name="left">The entry to compare</param>
    /// <param name="right">The entry to compare to</param>
    /// <returns>True if 'left' is less than 'right'</returns>
    public static bool operator <( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) < 0;
    }

    /// <summary>
    /// Compares this CandidateEntry to another CandidateEntry
    /// </summary>
    /// <param name="left">The entry to compare</param>
    /// <param name="right">The entry to compare to</param>
    /// <returns>True if 'left' is less than or equal to 'right'</returns>
    public static bool operator <=( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) <= 0;
    }

    /// <summary>
    /// Compares this CandidateEntry to another CandidateEntry
    /// </summary>
    /// <param name="left">The entry to compare</param>
    /// <param name="right">The entry to compare to</param>
    /// <returns>True if 'left' is greater than 'right'</returns>
    public static bool operator >( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) > 0;
    }

    /// <summary>
    /// Compares this CandidateEntry to another CandidateEntry
    /// </summary>
    /// <param name="left">The entry to compare</param>
    /// <param name="right">The entry to compare to</param>
    /// <returns>True if 'left' is greater than or equal to 'right'</returns>
    public static bool operator >=( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) >= 0;
    }

    /// <summary>
    /// Default disposing property
    /// </summary>
    private bool disposedValue;

    /// <summary>
    /// Default disposing method
    /// </summary>
    private void Dispose( bool disposing )
    {
        if ( !disposedValue )
        {
            if ( disposing )
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    /// <summary>
    /// Default disposing method
    /// </summary>
    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
