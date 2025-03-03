using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

using static BlocklistManager.Classes.IAddressExtensions;

namespace BlocklistManager.Classes;

public sealed record class CandidateEntry( string? siteName, string blocklistFileName, string? address, int? subnetSuffix, IPRange? addressRange, IAddress[] addressSet/*, ushort[] portsArg, FirewallProtocol protocolArg*/ ) : IComparable<CandidateEntry>, IDisposable
{

    private long[] _sort = [ 0, 0, 0, 0 ];

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="iPAddress"></param>
    /// <param name="iPAddressRange"></param>
    /// <param name="iPAddressSet"></param>
    /// <param name="ports"></param>
    /// <param name="protocol"></param>
    //public CandidateEntry( string? siteName, string? address, IPRange? addressRange, IAddress[] addressSet, ushort[] portsArg, FirewallProtocol protocolArg )
    //{
    //    this.Name = siteName;
    //    this.IPAddress = address;
    //    this.IPAddressRange = addressRange;
    //    this.IPAddressSet = addressSet;
    //    this.Ports = portsArg;
    //    this.Protocol = protocolArg;
    //}

    [Display( Description = "Name" )]
    [Length( 2, 50 )]
    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
    public string? Name { get; set; } = siteName;

    //    [Required]
    [Display( Description = "IP Address" )]
    public string? IPAddress { get; set; } = address;

    [Display( Description = "Subnet if applicable" )]
    public int? Subnet { get; set; } = subnetSuffix;

    [Display( Description = "IP Address Range" )]
    public IPRange? IPAddressRange { get; set; } = addressRange;

    [Display( AutoGenerateField = true, Description = "IP Address Batch", Name = "IPAddressSet" )]
    public IAddress[] IPAddressSet { get; set; } = addressSet;

    public string? IPAddressBatch => Maintain.IAddressesToString( IPAddressSet );
    //{
    //    get
    //    {
    //        return Maintain.IAddressesToString( this.IPAddressSet );
    //    }
    //}

    internal IPAddressType AddressType { get; set; } // => DetermineAddressType( ); // string.IsNullOrEmpty( this.IPAddress ) ? IPAddressType.Invalid : this.IPAddress.IndexOf( ':' ) > 0 ? IPAddressType.IPv6 : IPAddressType.IPv4; // { get; set; } = Maintain.IPAddressType.IPv4;

    /// <summary>
    /// The name of the blocklist file that this entry came from
    /// </summary>
    public string FileName { get; set; } = blocklistFileName;

    /// <summary>
    /// New: Simply record whether the IP address(es) or ranger has been validated
    /// </summary>
    public bool Validated { get; set; } = false;

    /// <summary>
    /// No longer needed as we need both port numbers and protocols before we can set these in the Windows Firewall and I'd rather fully block the IP address
    /// </summary>
    //[Display( Description = "Ports (default 'Any')" )]
    //public ushort[] Ports { get; set; } = portsArg;

    /// <summary>
    /// No longer needed as we need both port numbers and protocols before we can set these in the Windows Firewall and I'd rather fully block the IP address
    /// </summary>
    //[Display( Description = "Protocol (default 'Any')" )]
    //public FirewallProtocol Protocol { get; set; } = protocolArg;

    /* Removed because none of these are used for the firewall rules */
    //[Display( Description = "Status" )]
    //public string? Status { get; set; } = "-";

    //public string? Number { get; set; }

    //[Length( 2, 255 )]
    //internal string? Description { get; set; } = null;

    //[Display( Description = "Country" )]
    //[Length( 2, 50 )]
    //public string? Country { get; set; } = null;

    //[Display( Description = "Malware Name" )]
    //public string? Malware { get; set; }

    [Display( AutoGenerateField = false )]
    internal long[] Sort => DetermineSort( );

    internal long Sort0 => Sort[ 0 ];
    internal long Sort1 => Sort.Length > 0 ? Sort[ 1 ] : 0;
    internal long Sort2 => Sort.Length > 1 ? Sort[ 2 ] : 0;
    internal long Sort3 => Sort.Length > 2 ? Sort[ 3 ] : 0;

    //private IPAddressType DetermineAddressType( )
    //{
    //    string tmpAddress = string.Empty;
    //    if ( string.IsNullOrEmpty( IPAddress ) && IPAddressRange is not null )
    //        tmpAddress = IPAddressRange!.StartAddress.ToString( );
    //    else if ( !string.IsNullOrEmpty( IPAddressBatch ) )
    //        tmpAddress = IPAddressSet[ 0 ].ToString( );
    //    else
    //        tmpAddress = IPAddress!;


    //    if ( tmpAddress.IndexOf( ':' ) > 0 )
    //        return IPAddressType.IPv6;
    //    else
    //        return IPAddressType.IPv4;
    //}

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

    public static bool operator <( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) < 0;
    }

    public static bool operator <=( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) <= 0;
    }

    public static bool operator >( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) > 0;
    }

    public static bool operator >=( CandidateEntry left, CandidateEntry right )
    {
        return left.CompareTo( right ) >= 0;
    }

    private bool disposedValue;

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

    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
