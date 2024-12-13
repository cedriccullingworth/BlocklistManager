using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

using Microsoft.IdentityModel.Tokens;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

public record class CandidateEntry : IComparable<CandidateEntry>, IDisposable
{
    private long[] _sort = [ 0, 0, 0, 0 ];

    [Display( Description = "Name" )]
    [Length( 2, 50 )]
    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
    public string? Name { get; set; } = null;

    //    [Required]
    [Display( Description = "IP Address" )]
    public string? IPAddress { get; set; } = null;

    [Display( Description = "IP Address Range" )]
    public IPRange? IPAddressRange { get; set; } = null;

    [Display( AutoGenerateField = true, Description = "IP Address Batch", Name = "IPAddressSet" )]
    public IAddress[] IPAddressSet { get; set; } = [];

    public string? IPAddressBatch
    {
        get
        {
            return Maintain.IAddressesToString( this.IPAddressSet );
        }
    }

    internal Maintain.IPAddressType AddressType { get; set; } = Maintain.IPAddressType.IPv4;

    [Display( Description = "Ports (default 'Any')" )]
    public ushort[] Ports { get; set; } = [];

    [Display( Description = "Protocol" )]
    public FirewallProtocol Protocol { get; set; } = FirewallProtocol.Any;

    [Display( Description = "Status" )]
    public string? Status { get; set; } = "-";

    //public string? Number { get; set; }

    [Length( 2, 255 )]
    internal string? Description { get; set; } = null;

    [Display( Description = "Country" )]
    [Length( 2, 50 )]
    public string? Country { get; set; } = null;

    [Display( Description = "Malware Name" )]
    public string? Malware { get; set; }

    [Display( AutoGenerateField = false )]
    internal long[] Sort
    {
        get
        {
            if ( _sort[ 0 ] > 0 )
                return _sort;
            else
            {
                IPAddress address = System.Net.IPAddress.Parse( "8.8.8.8" );
                if ( !string.IsNullOrEmpty( IPAddressBatch ) )
                    address = System.Net.IPAddress.Parse( IPAddressBatch!.Split( ';' )[ 0 ] );
                else if ( !string.IsNullOrEmpty( IPAddress ) )
                    address = System.Net.IPAddress.Parse( IPAddress );
                else if ( IPAddressRange is not null && IPAddressRange.StartAddress is not null )
                    address = IPAddressRange.StartAddress;

                if ( AddressType == Maintain.IPAddressType.IPv4 )
                {
                    _sort = address.GetAddressBytes( )
                                   .Select( s => Convert.ToInt64( s ) )
                                   .ToArray( );
                }
                else if ( AddressType == Maintain.IPAddressType.IPv6 )
                {
                    //byte[] bytes = address.GetAddressBytes( );
                    string[] addressParts = address.ToString( )
                                                   .Split( ':' )
                                                   .Where( w => !string.IsNullOrEmpty( w ) )
                                                   .ToArray( );

                    List<long> sort = addressParts
                            .Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber ) )
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

                return _sort;
            }
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
            if ( this.Equals( other ) )
                ret = 0;
            else if ( this.Equals( compare[ 1 ] ) )  // this follows other
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

    protected virtual void Dispose( bool disposing )
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

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~CandidateEntry()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
