using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using WindowsFirewallHelper;

using static BlocklistManager.Classes.IAddressExtensions;
using static BlocklistManager.Classes.Maintain;

namespace BlocklistManager.Classes;

public record class FirewallRule( string Name, FirewallAction Action, FirewallDirection Direction, FirewallProfiles Profiles, IAddress[] RemoteAddresses, FirewallProtocol Protocol, ushort[] RemotePorts ) : IFirewallRule, IDisposable
{
    private long[] _sortValue = [];

    // Summary:
    // Gets or sets the name of the rule in native format w/o auto string resolving
    public string Name { get; set; } = Name;

    // Summary:
    // Gets or sets the local RemoteAddresses that the rule applies to
    public IAddress[] LocalAddresses { get; set; } = [];

    // Summary:
    // Gets or sets the local ports that the rule applies to
    public ushort[] LocalPorts { get; set; } = [];

    // Summary:
    // Gets or sets the remote RemoteAddresses that the rule applies to
    public IAddress[] RemoteAddresses { get; set; } = RemoteAddresses;

    // Summary:
    // Gets or sets the remote ports that the rule applies to
    public ushort[] RemotePorts { get; set; } = RemotePorts;

    public string? RemoteAddressList
    {
        get
        {
            if ( RemoteAddresses is null )
                return null;
            else
            {
                string addresses = IAddressesToString( RemoteAddresses );
                if ( addresses.Length > 32767 )
                    addresses = addresses[ ..32767 ];
                return addresses;
            }

        }
    }

    // Summary:
    // Gets or sets the Action that the rules defines
    public FirewallAction Action { get; set; } = Action;

    // Summary:
    // Gets or sets a Boolean value indicating if this rule is active
    public bool IsEnable { get; set; } = true;

    // Summary:
    // Gets or sets the data Direction that the rule applies to
    public FirewallDirection Direction { get; set; } = Direction;

    // Summary:
    // Gets or sets the type of local ports that the rules applies to
    public FirewallPortType LocalPortType { get; set; } = FirewallPortType.All;

    // Summary:
    // Gets or sets the name of the application that this rule is about
    public string ApplicationName { get; set; } = "Any";

    // Summary:
    // Gets the profiles that this rule belongs to
    public FirewallProfiles Profiles { get; } = Profiles;

    // Summary:
    // Gets or sets the protocol that the rule applies to
    public FirewallProtocol Protocol { get; set; } = Protocol;

    // Summary:
    // Gets or sets the scope that the rule applies to
    public FirewallScope Scope { get; set; } = FirewallScope.Specific;

    //
    // Summary:
    // Gets or sets the resolved name of the rule
    public string FriendlyName { get; } = Name;

    //
    // Summary:
    // Gets or sets the name of the service that this rule is about
    public string ServiceName { get; set; } = Name;

    public long[] SortValue
    {
        get
        {
            //if ( _sortValue.Length >= 4 && this._sortValue[ 0 ] == 0 && this.RemoteAddresses[ 0 ].ToString( ).Contains( '-' ) ) // This one is an address range
            //{
            //    string addressRangeString = this.RemoteAddresses[ 0 ].ToString( );
            //    string startAddressStrig = addressRangeString[ ..addressRangeString.IndexOf( '-' ) ];
            //    var startAddress = SingleIP.Parse( startAddressStrig );
            //};

            if ( this._sortValue.Length >= 4 && this._sortValue[ 0 ] > 0 )
                return this._sortValue;

            if ( this.RemoteAddresses.Length < 1 )
                return this._sortValue;

            CultureInfo culture = CultureInfo.InvariantCulture;
            IPAddressType addressType = RemoteAddresses[ 0 ].ToString( )
                                                            .IndexOf( ':' ) > 0 ? IPAddressType.IPv6 : IPAddressType.IPv4;

            if ( RemoteAddresses.Length == 1 && RemoteAddresses[ 0 ].ToString( ).IndexOf( '-' ) > 0 ) // Single IP address
            {
                _sortValue = GetSortValueForSingeIPAddress( culture, addressType );
            }
            else if ( RemoteAddresses is not null && RemoteAddresses.Length > 0 ) // Multiple IP addresses or "IPAddressSet"
            {
                _sortValue = GetSortValueForIPAddressSet( culture, addressType );
            }

            return _sortValue;
        }
    }

    private long[] GetSortValueForIPAddressSet( CultureInfo culture, IPAddressType addressType )
    {
        string firstAddress = RemoteAddresses[ 0 ].ToString( );
        if ( firstAddress.Contains( '-' ) )
            firstAddress = firstAddress[ ..firstAddress.IndexOf( '-' ) ];

        string[] partsAsStrings = ( addressType == IPAddressType.IPv4
                                                        ? firstAddress.Split( '.' )
                                                        : firstAddress.Split( ':' ) )
                                  .Select( s => string.IsNullOrEmpty( s ) ? "0" : s )
                                  .ToArray( );
        try
        {
            if ( addressType == IPAddressType.IPv6 )
                _sortValue = partsAsStrings.Select( s => long.Parse( s, NumberStyles.HexNumber, culture ) )
                            .ToArray( );
            else
                _sortValue = partsAsStrings.Select( s => long.Parse( s, NumberStyles.Number, culture ) )
                                .ToArray( );
        }
        catch ( Exception ex )
        {
            Debug.Print( firstAddress );
            StatusMessage( $"FirewallRule SortValue {firstAddress}", ex, null );
        }

        return _sortValue;
    }

    private long[] GetSortValueForSingeIPAddress( CultureInfo culture, IPAddressType addressType )
    {
        int endOfStartAddress = RemoteAddresses[ 0 ].ToString( ).IndexOf( '-' );
        string firstAddress = RemoteAddresses[ 0 ].ToString( )[ ..endOfStartAddress ];
        string[] partsAsStrings = ( addressType == IPAddressType.IPv4
                                        ? firstAddress.Split( '.' )
                                        : firstAddress.Split( ':' ) )
                                  .Select( s => string.IsNullOrEmpty( s ) ? "0" : s )
                                  .ToArray( );
        try
        {
            if ( addressType == IPAddressType.IPv6 )
                _sortValue = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber, culture ) )
                                .ToArray( );
            else
                _sortValue = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.Number, culture ) )
                                .ToArray( );
        }
        catch
        {
            Debug.Print( firstAddress );
        }

        return _sortValue;
    }

    bool IEquatable<IFirewallRule>.Equals( IFirewallRule? other )
    {
        throw new NotImplementedException( );
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
    // ~FirewallRule()
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
