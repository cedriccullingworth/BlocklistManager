using System;
using System.Collections.Generic;
using System.Linq;

using BlocklistManager.Models;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

internal static class Tests
{
    private static List<FirewallRule> _allFirewallRules = [];


    internal static bool Execute( List<RemoteSite> sites )
    {
        bool passed = false;

        List<CandidateEntry> allDownloaded = Maintain.DownloadBlocklists( null, sites )!;
        List<CandidateEntry> cleaned = Maintain.CleanupDownloadedIPAddressData( sites, null, false, allDownloaded, out int numberOfIPs );
        if ( FetchAllAppFirewallRules( sites ) )
        {
            passed = NoIPsWereLost( sites, allDownloaded, cleaned );
        }

        return passed;
    }

    internal static bool TestIPValidation( string[] ipAddresses )
    {
        bool passed = false;
        foreach ( string ipAddress in ipAddresses )
        {
            passed = Maintain.ValidateIPAddress( ipAddress, out IAddressExtensions.IPAddressType addressType );
        }

        return passed;
    }

    private static bool FetchAllAppFirewallRules( List<RemoteSite> sites )
    {
        bool noErrors = false;

        try
        {
            _allFirewallRules = Maintain.FetchFirewallRulesFor( );
            _allFirewallRules = ( from a in _allFirewallRules
                                  join s in sites
                                  on a.Name equals s.Name + "_Blocklist"
                                  select a
                                ).ToList( );
            noErrors = true;
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "Tests:FetchAllAppFirewallRules", ex, null );
        }

        return noErrors;
    }

    private static FirewallRuleRemoteAddress FWEntryFromAddress( IAddress address )
    {
        string addresString = address.ToString( );
        FirewallAddressType addressType = FirewallAddressType.FWIPAddress;
        if ( addresString.Contains( '-' ) && IPRange.TryParse( address.ToString( ), out IPRange newRange ) )
        {
            addressType = FirewallAddressType.FWIPRange;
            return new FirewallRuleRemoteAddress( newRange, addressType );
        }
        else if ( SingleIP.TryParse( address.ToString( ), out SingleIP newIP ) )
        {
            return new FirewallRuleRemoteAddress( newIP, addressType );
        }
        else
            throw new NotImplementedException( $"FWEntryFromAddress: Cannot work with {address.ToString( )}" );
    }

    internal static bool NoIPsWereLost( List<RemoteSite> sites, List<CandidateEntry> downloaded, List<CandidateEntry> candidates )
    {
        // Changed to strings 20/2/2025 for simplification 
        //bool matched = false;

        // Simplify downloaded
        //        Maintain.SubnetsToRanges( ref downloaded, out int itemCount );

        // Read all the data for which rules should have been created
        var downloads = downloaded.Where( e => e.IPAddress is not null && e.IPAddressRange is null )
                                           .Select( s => new Download( s.IPAddress!.ToString( ), s.Name! ) )
                                           .ToList( );
        downloads.AddRange( downloaded.Where( e => e.IPAddressRange is not null )
                                           .Select( s => new Download( s.IPAddressRange!.ToString( ), s.Name! ) ) );
        //.Select( s => s.IPAddressRange!.ToString( ) ) );

        // Read the firewall rules which have been created
        List<string> fwIPAddresses = _allFirewallRules.Where( w => w.Direction == FirewallDirection.Inbound )
                                                      .SelectMany( m => m.RemoteAddresses.Select( s => s.ToString( ) ) )
                                                      .Distinct( )
                                                      .ToList( );

        // Identify any downloaded data not found in the firewall
        var notInFirewall = downloads.GroupJoin(
          fwIPAddresses,
          d => d.IPAddressOrRange,
          f => f,
          ( d, f ) => new { Site = d.siteName, Download = d.IPAddressOrRange, Firewall = f.SingleOrDefault( ) } ).Where( w => w.Firewall is null );

        return !notInFirewall.Any( );
    }

    private enum FirewallAddressType
    {
        FWIPAddress,
        FWIPRange
    }

    private record FirewallRuleRemoteAddress( IAddress AddressOrRange, FirewallAddressType AddressType );

    private record Download( string IPAddressOrRange, string siteName );
}
