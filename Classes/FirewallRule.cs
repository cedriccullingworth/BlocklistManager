using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using WindowsFirewallHelper;

using static BlocklistManager.Classes.Maintain;

namespace BlocklistManager.Classes;

public record class FirewallRule( string Name, FirewallAction Action, FirewallDirection Direction, FirewallProfiles Profiles, IAddress[] RemoteAddresses, FirewallProtocol Protocol, ushort[] RemotePorts ) : IFirewallRule, IDisposable
{
    //
    // Summary:
    //     Gets or sets the name of the rule in native format w/o auto string resolving
    public string Name { get; set; } = Name;

    //private string Description { get; set; } = Name;

    //
    // Summary:
    //     Gets or sets the local RemoteAddresses that the rule applies to
    public IAddress[] LocalAddresses { get; set; } = [];

    //
    // Summary:
    //     Gets or sets the local ports that the rule applies to
    public ushort[] LocalPorts { get; set; } = [];

    //
    // Summary:
    //     Gets or sets the remote RemoteAddresses that the rule applies to
    public IAddress[] RemoteAddresses { get; set; } = RemoteAddresses;

    //
    // Summary:
    //     Gets or sets the remote ports that the rule applies to
    public ushort[] RemotePorts { get; set; } = RemotePorts;

    public string? RemoteAddressList
    {
        get
        {
            if ( this.RemoteAddresses is null )
                return null;
            else
                return IAddressesToString( this.RemoteAddresses );
        }
    }

    //
    // Summary:
    //     Gets or sets the Action that the rules defines
    public FirewallAction Action { get; set; } = Action;

    //
    // Summary:
    //     Gets or sets a Boolean value indicating if this rule is active
    public bool IsEnable { get; set; } = true;

    //public IEquatable<IFirewallRule> Equals(IFirewallRule?)
    //{

    //}

    //
    // Summary:
    //     Gets or sets the data Direction that the rule applies to
    public FirewallDirection Direction { get; set; } = Direction;

    //
    // Summary:
    //     Gets or sets the type of local ports that the rules applies to
    public FirewallPortType LocalPortType { get; set; } = FirewallPortType.All;

    //
    // Summary:
    //     Gets or sets the name of the application that this rule is about
    public string ApplicationName { get; set; } = "Any";

    //
    // Summary:
    //     Gets the profiles that this rule belongs to
    public FirewallProfiles Profiles { get; } = Profiles;

    //
    // Summary:
    //     Gets or sets the protocol that the rule applies to
    public FirewallProtocol Protocol { get; set; } = Protocol;

    //
    // Summary:
    //     Gets or sets the scope that the rule applies to
    public FirewallScope Scope { get; set; } = FirewallScope.All;

    //
    // Summary:
    //     Gets or sets the resolved name of the rule
    public string FriendlyName { get; } = Name;

    //
    // Summary:
    //     Gets or sets the name of the service that this rule is about
    public string ServiceName { get; set; } = Name;

    public long[] SortValue
    {
        get
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            long[] parts = [];
            IPAddressType addressType = this.RemoteAddresses[ 0 ]
                                            .ToString( )
                                            .IndexOf( ':' ) > 0 ? IPAddressType.IPv6 : IPAddressType.IPv4;

            if ( this.RemoteAddresses.Length == 1 && this.RemoteAddresses[ 0 ].ToString( ).IndexOf( '-' ) > 0 )
            {
                int endOfStartAddress = this.RemoteAddresses[ 0 ].ToString( ).IndexOf( '-' );
                string firstAddress = this.RemoteAddresses[ 0 ].ToString( )[ ..endOfStartAddress ];

                string[] partsAsStrings = ( addressType == IPAddressType.IPv4
                                                ? firstAddress.Split( '.' )
                                                : firstAddress.Split( ':' ) )
                                          .Select( s => string.IsNullOrEmpty( s ) ? "0" : s )
                                          .ToArray( );
                try
                {
                    if ( addressType == IPAddressType.IPv6 )
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber, culture ) )
                                        .ToArray( );
                    }
                    else
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.Number, culture ) )
                                        .ToArray( );
                    }
                }
                catch
                {
                    Debug.Print( firstAddress );
                }
            }
            else if ( this.RemoteAddresses is not null && this.RemoteAddresses.Length > 0 )
            {
                string firstAddress = this.RemoteAddresses[ 0 ].ToString( );
                string[] partsAsStrings = ( addressType == IPAddressType.IPv4
                                                                ? firstAddress.Split( '.' )
                                                                : firstAddress.Split( ':' ) )
                                          .Select( s => string.IsNullOrEmpty( s ) ? "0" : s )
                                          .ToArray( );
                try
                {
                    if ( addressType == IPAddressType.IPv6 )
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, NumberStyles.HexNumber, culture ) )
                                    .ToArray( );
                    }
                    else
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, NumberStyles.Number, culture ) )
                                        .ToArray( );
                    }
                }
                catch
                {
                    Debug.Print( firstAddress );
                }
            }

            return parts;
        }
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
