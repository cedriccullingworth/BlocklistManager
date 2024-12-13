using System;
using System.Diagnostics;
using System.Linq;

using WindowsFirewallHelper;

using static BlocklistManager.Classes.Maintain;

namespace BlocklistManager.Classes;

public class FirewallRule( string ruleName, FirewallAction action, FirewallDirection direction, FirewallProfiles profile, IAddress[] addresses ) : IFirewallRule
{
    //
    // Summary:
    //     Gets or sets the name of the rule in native format w/o auto string resolving
    public string Name { get; set; } = ruleName;

    //private string Description { get; set; } = ruleName;

    //
    // Summary:
    //     Gets or sets the local addresses that the rule applies to
    public IAddress[] LocalAddresses { get; set; } = [];

    //
    // Summary:
    //     Gets or sets the local ports that the rule applies to
    public ushort[] LocalPorts { get; set; } = [];

    //
    // Summary:
    //     Gets or sets the remote addresses that the rule applies to
    public IAddress[] RemoteAddresses { get; set; } = addresses;

    //
    // Summary:
    //     Gets or sets the remote ports that the rule applies to
    public ushort[] RemotePorts { get; set; } = [];

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
    //     Gets or sets the action that the rules defines
    public FirewallAction Action { get; set; } = action;

    //
    // Summary:
    //     Gets or sets a Boolean value indicating if this rule is active
    public bool IsEnable { get; set; }

    //public IEquatable<IFirewallRule> Equals(IFirewallRule?)
    //{

    //}

    //
    // Summary:
    //     Gets or sets the data direction that the rule applies to
    public FirewallDirection Direction { get; set; } = direction;

    //
    // Summary:
    //     Gets or sets the type of local ports that the rules applies to
    public FirewallPortType LocalPortType { get; set; }

    //
    // Summary:
    //     Gets or sets the name of the application that this rule is about
    public string ApplicationName { get; set; } = "Any";

    //
    // Summary:
    //     Gets the profiles that this rule belongs to
    public FirewallProfiles Profiles { get; } = profile;

    //
    // Summary:
    //     Gets or sets the protocol that the rule applies to
    public FirewallProtocol Protocol { get; set; } = FirewallProtocol.Any;

    //
    // Summary:
    //     Gets or sets the scope that the rule applies to
    public FirewallScope Scope { get; set; }

    //
    // Summary:
    //     Gets or sets the resolved name of the rule
    public string FriendlyName { get; } = ruleName;

    //
    // Summary:
    //     Gets or sets the name of the service that this rule is about
    public string ServiceName { get; set; } = ruleName;

    public long[] SortValue
    {
        get
        {
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
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber ) )
                                        .ToArray( );
                    }
                    else
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.Number ) )
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
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.HexNumber ) )
                                    .ToArray( );
                    }
                    else
                    {
                        parts = partsAsStrings.Select( s => long.Parse( s, System.Globalization.NumberStyles.Number ) )
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
}
