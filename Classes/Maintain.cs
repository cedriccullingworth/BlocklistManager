using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Models;

using NetTools;

using OSVersionExtension;

using SBS.Utilities;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.FirewallRules;



namespace BlocklistManager.Classes;

internal static class Maintain
{
    internal static readonly OSVersionExtension.OperatingSystem osVersion = GetOSVersion( );
    internal static readonly int MAX_FIREWALL_BATCH_SIZE = GetFirewallBatchSize;
    private const char BATCH_DELIMITER = ';';
    private static readonly string _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;
    internal static string LogFileFullname = Assembly.GetEntryAssembly( )!.FullName!.Replace( ".exe", ".log" );
    private static Device? device;
    private static string? macAddress = GetMACAddress( ); // "00:00:00:00:00:00";
    private static CultureInfo culture = CultureInfo.CurrentCulture;

    internal static List<FileType> FILETYPES => ( new BlocklistData( ) ).ListFileTypes( );

    internal static FirewallProfiles _AllProfiles = FirewallProfiles.Public | FirewallProfiles.Domain | FirewallProfiles.Private;

    internal static OSVersionExtension.OperatingSystem GetOSVersion( ) => OSVersion.GetOperatingSystem( );

    internal static int GetFirewallBatchSize => ( osVersion == OSVersionExtension.OperatingSystem.Windows11 || osVersion == OSVersionExtension.OperatingSystem.WindowsServer2022 ) ? 10000 : 1000;

    internal static Device? ConnectedDevice
    {
        get
        {
            if ( device == null )
            {
                device = macAddress is null ? null : new BlocklistData( ).GetDevice( macAddress );
            }

            return device!;
        }
    }

    internal enum IPAddressType
    {
        IPv4,
        IPv6,
        Invalid
    }

    internal sealed record Adapter( string NetworkType, string MACAddress );

    /// <summary>
    /// Get the client computer's MAC addressRange for use in identifying the device
    /// NOTE: This currently only considers Ethernet and 802.11 WiFi adapters with IP addresses and an active gateway, add other types as required
    /// </summary>
    /// <returns>The MAC addressRange of the first active Ethernet or WiFi adapter found</returns>
    public static string? GetMACAddress( )
    {
        string firstMACAddressFound = NetworkInterface.GetAllNetworkInterfaces( )
                                    .Where( w => w.GetIPProperties( ).GatewayAddresses.Count > 0 )
                                    .Select( s => MACAddress( s.GetPhysicalAddress( ) ) )
                                    .First( );

        if ( firstMACAddressFound is null )
            return null;

        try
        {
            device = new BlocklistData( ).GetDevice( firstMACAddressFound );

            List<Adapter> macAddresses = NetworkInterface.GetAllNetworkInterfaces( )
                                    .Where( w => w.OperationalStatus == OperationalStatus.Up )
                                    .Where( w => w.NetworkInterfaceType == NetworkInterfaceType.Ethernet || w.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 )
                                    .Where( w => w.GetPhysicalAddress( ).ToString( ).Length >= 12 )
                                    .Where( w => w.GetIPProperties( ).GatewayAddresses.Count > 0 )
                                    .Where( w => w.GetIPProperties( ) != null && ( w.GetIPProperties( ).GetIPv4Properties( ) != null ) || w.GetIPProperties( ).GetIPv6Properties( ) != null )
                                    .Select( s => new Adapter
                                        (
                                            s.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : "WiFi",
                                            MACAddress( s.GetPhysicalAddress( ) )
                                        ) )
                                    .OrderBy( o => o.NetworkType )
                                    .ToList( );

            return macAddresses.Count > 0 ? macAddresses.First( ).MACAddress : null;
        }
        catch ( Exception ex )
        {
            StatusMessage( _appName, ex, null );
            return null;
        }

    }

    /// <summary>
    /// Formats the MAC addressRange as a string, "00:00:00:00:00:00" if no valid physical addressRange is found
    /// </summary>
    /// <param name="physicalAddress"></param>
    /// <returns></returns>
    private static string MACAddress( PhysicalAddress physicalAddress )
    {
        /* MAC Address for tests: 2C:3B:70:0C:DA:F5 */
        string s = physicalAddress.ToString( );
        if ( s.Length >= 12 )
            return string.Format( new CultureInfo( "en-US" ), "{0}:{1}:{2}:{3}:{4}:{5}", s[ ..2 ], s[ 2..4 ], s[ 4..6 ], s[ 6..8 ], s[ 8..10 ], s[ 10..12 ] );
        else
            return "00:00:00:00:00:00";
    }

    internal static List<RemoteSite> ListDownloadSites( RemoteSite? remoteSite, bool showAll = false )
    {
        return new BlocklistData( ).ListDownloadSites( ConnectedDevice!.ID, remoteSite, showAll );
    }

    internal static DateTime UpdateLastDownloaded( RemoteSite site )
    {
        int deviceID = ConnectedDevice!.ID;
        using BlocklistData blocklistData = new BlocklistData( );
        return blocklistData.SetLastDownloaded( ConnectedDevice!.ID, site.ID );
    }

    /// <summary>
    /// Validate a blocklist download site and its file Urls
    /// </summary>
    /// <param name="site">The blocklist download site</param>
    /// <returns></returns>
    internal static bool ValidRemoteSite( RemoteSite site )
    {
        bool valid = !string.IsNullOrEmpty( site.Name )
                    && !string.IsNullOrEmpty( site.FileUrls );

        site.Name = site.Name.Trim( );
        site.SiteUrl = site.SiteUrl!.Trim( );
        site.FileUrls = site.FileUrls.Trim( );
        if ( site.FileUrls.EndsWith( ',' ) )
            site.FileUrls = site.FileUrls[ ..^1 ];

        while ( valid )
        {
            site.Name = site.Name.Length > 50 ? site.Name[ ..50 ] : site.Name;
            valid = site.Name.Length >= 2;
            if ( valid )
                valid = Maintain.UrlHostExists( site.SiteUrl ); // make url a ref

            // Confirm the existence of the FileUrls
            if ( valid )
            {
                for ( int i = 0; i < site.FilePaths.Count; i++ )
                {
                    valid = false;
                    try
                    {
                        // Using ReadHtmlContentFromUrl simply because it doesn't attempt any sort of validation of the remote document, only confirms that it exists and can be read
                        //string readAttempt = HttpHelper.ReadHtmlContentFromUrl( site, site.FilePaths[ i ] );
                        Stream? readAttempt = Downloader.ReadHtmlStreamFromUrl( site, site.FilePaths[ i ] );
                        valid = readAttempt is not null; // && readAttempt.Length > 0;
                    }
                    catch { }
                }
            }

            break;
        }

        return valid;
    }

    /// <summary>
    /// Fetch firewall rules for a blocklist download site
    /// </summary>
    /// <param name="ruleName">The blocklist download site rule name</param>
    /// <returns>A list of all rules found for the site</returns>
    internal static List<FirewallRule> FetchFirewallRulesFor( string? name = null )
    {
        StringComparison comparison = StringComparison.Ordinal;
        if ( name is not null && !name.EndsWith( "_Blocklist", comparison ) )
            name = $"{name}_Blocklist";

        IEnumerable<IFirewallRule> rules = FirewallManager.Instance
                                                          .Rules
                                                          .Where( w => w.Name.EndsWith( "_Blocklist", comparison ) )
                                                          .Where( w => name is null || w.Name == name );

        return [ .. rules.Select( s => new FirewallRule( s.Name, s.Action, s.Direction, s.Profiles, s.RemoteAddresses, s.Protocol, s.RemotePorts ) )
                    .OrderBy( o => o.Name )
                    .ThenBy( o => o.SortValue.Length > 0 ? o.SortValue[ 0 ] : 0 )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 1 ] : 0 )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 2 ] : 0 )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 3 ] : 0 ) ];
    }

    /// <summary>
    /// Delete all rules for a blocklist download site
    /// TODO: This doesn't provide for ruleName "(All)"
    /// </summary>
    /// <param name="ruleName">The blocklist download site rule name</param>
    /// <param name="logFilePath">An optional log file path. Consider rather making this a property of the class</param>
    /// <returns>True when no errors were encountered</returns>
    internal static bool DeleteExistingFirewallRulesFor( string ruleName )
    {
        bool deleted = false;
        var rules = FirewallManager.Instance
                                                         .Rules
                                                         .Where( w => w.Name == ruleName );
        try
        {
            foreach ( var rule in rules )
            {
                FirewallManager.Instance.Rules.Remove( rule );
            }

            deleted = true;
        }
        catch ( Exception ex )
        {
            if ( LogFileFullname is not null )
                StatusMessage( _appName, ex, null );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "DeleteExistingFirewallRulesFor", ex ) );
        }

        return deleted;
    }

    /// <summary>
    /// Add both inbound and outbound rules for an IP addressRange array or range
    /// </summary>
    /// <param name="ruleName">The name to use for the rule</param>
    /// <param name="ipAddressSet">an IP addressRange array</param>
    /// <param name="ipAddressRange">an IP addressRange range</param>
    /// <param name="protocol">Protocol if provided, defaults to Any</param>
    /// <param name="ports">remote ports if provided, defaults to All</param>
    /// <param name="logFilePath">An optional log file path. Consider rather making this a property of the class</param>
    /// <returns></returns>
    private static List<IFirewallRule> AddInboundAndOutboundRules
        (
            string ruleName,
            IAddress[]? ipAddressSet = null,
            IPRange? ipAddressRange = null,
            //FirewallProtocol? protocol = null,
            ushort[]? ports = null
        )
    {
        //protocol ??= FirewallProtocol.Any;
        ports ??= [];
        List<IFirewallRule> rules = [];
        IAddress[] remoteIPRange = ipAddressRange is not null
                                 ? new IPRange[] { ipAddressRange }
                                 : [];

        // Delete any existing rules matching on name and IP ipAddress set
        // This is redundant as all entries for the ruleName are deleted before this, but leaving it here to make sure
        DeleteMatchingEntries( ruleName, ipAddressSet, ipAddressRange, remoteIPRange );

        SingleIP[] local = [ new( IPAddress.Any ) ];

        // Create and add the new rules. I have separated these because assigning property values to elements in a collection didn't work
        IFirewallRule ruleOut = CreateRule( ruleName, ipAddressSet, ports, remoteIPRange, local, FirewallDirection.Outbound );
        IFirewallRule ruleIn = CreateRule( ruleName, ipAddressSet, ports, remoteIPRange, local, FirewallDirection.Inbound );

        try
        {
            FirewallManager.Instance.Rules.Add( ruleOut );
            rules.Add( ruleOut ); // Succeeded
            FirewallManager.Instance.Rules.Add( ruleIn );
            rules.Add( ruleIn ); // Succeeded
        }
        catch ( Exception ex )
        {
            if ( LogFileFullname != null )
                StatusMessage( _appName, ex, null );
            //Logger.Log( _appName, ex );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "AddInboundAndOutboundRules", ex ) );
        }

        return rules;
    }

    /// <summary>
    /// Delete firewall rules with names matching ruleName and 
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="ipAddressSet"></param>
    /// <param name="ipAddressRange"></param>
    /// <param name="remoteIPRange"></param>
    private static void DeleteMatchingEntries( string ruleName, IAddress[]? ipAddressSet, IPRange? ipAddressRange, IAddress[] remoteIPRange )
    {
        IEnumerable<IFirewallRule> existing = [];
        if ( ipAddressSet is not null && ipAddressSet.Length > 0 )
            existing = FirewallManager.Instance
                                      .Rules
                                      .Where( f => f.Name == ruleName && f.RemoteAddresses == remoteIPRange );
        else if ( ipAddressRange is not null && ipAddressRange.StartAddress is not null && ipAddressRange.EndAddress is not null )
            existing = FirewallManager.Instance
                                      .Rules
                                      .Where( f => f.Name == ruleName && f.RemoteAddresses == ipAddressSet );

        if ( existing is not null && existing.Any( ) )
        {
            foreach ( var item in existing )
                FirewallManager.Instance
                               .Rules
                               .Remove( item );
        }
    }

    /// <summary>
    /// 
    /// Create a new FirewallWASRule from the arguments provided
    /// </summary>
    /// <param name="ruleName"></param>
    /// <param name="ipAddressSet"></param>
    /// <param name="ports"></param>
    /// <param name="remoteIPRange"></param>
    /// <param name="local"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    private static FirewallWASRule CreateRule( string ruleName, IAddress[]? ipAddressSet, ushort[]? ports, IAddress[] remoteIPRange, SingleIP[] local, FirewallDirection direction )
    {
        FirewallWASRule newRule = new( ruleName, FirewallAction.Block, direction, _AllProfiles )
        {
            Description = ruleName,
            Grouping = _appName,
            Direction = direction, // Trying to force this as the above parameter didn't have an effect
            LocalAddresses = local,
            RemoteAddresses = remoteIPRange.Length > 0 ? remoteIPRange : ipAddressSet,
            Protocol = FirewallProtocol.Any,
            IsEnable = true,
        };

        if ( ports is not null && ports.Length > 0 && ports[ 0 ] > 0 && ( newRule.Protocol == FirewallProtocol.TCP || newRule.Protocol == FirewallProtocol.UDP ) )
            newRule.RemotePorts = ports;

        return newRule;
    }

    /// <summary>
    /// Convert a string to an IAddress SingleIP
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private static SingleIP StringToIAddress( string address )
    {
        return new( IPAddress.Parse( address ) );
    }

    /// <summary>
    /// Convert a string to an IAddress IPRange
    /// </summary>
    /// <param name="addressRange"></param>
    /// <returns></returns>
    private static IPRange StringToIAddressRange( string addressRange )
    {
        return IPRange.Parse( addressRange );
    }

    /// <summary>
    /// Convert an array of IAddress to a delimited string
    /// </summary>
    /// <param name="addresses"></param>
    /// <returns></returns>
    internal static string IAddressesToString( IAddress[] addresses )
    {
        if ( addresses.Length > 0 )
            return string.Join( BATCH_DELIMITER, addresses.Select( s => s.ToString( ) ).ToArray( ) );
        else
            return string.Empty;
    }

    /// <summary>
    /// Return true is a rule matching remoteIPAddressRange and direction already exists
    /// </summary>
    /// <param name="remoteIPAddressRange">The IP addressRange range to look for in the firewall rules</param>
    /// <param name="direction">The rule direction (Windows Firewall separates inbound and outbound rules</param>
    /// <returns>True if found</returns>
    private static bool RuleExists( IPRange remoteIPAddressRange, FirewallDirection direction )
    {
        IAddress[] remoteIPRange = [ new SingleIP( remoteIPAddressRange.StartAddress ), new SingleIP( remoteIPAddressRange.EndAddress ) ];
        // IFirewallRule newRule = CreateRule( ruleName, null, [], description, remoteIPRange, [], direction );
        int count = FirewallManager.Instance
                        .Rules
                        .Where( w => w.RemoteAddresses == remoteIPRange && w.Direction == direction )
                        .Count( );
        return count > 0;
    }

    /// <summary>
    /// Create Windows Firewall rules for IP addressRange sets and ranges i.e all entries because individual IP addresses were grouped into sets
    /// </summary>
    /// <param name="ruleName">The rule name</param>
    /// <param name="siteName">The name of the download site</param>
    /// <param name="newEntries">A list of entries for all downloaded data</param>
    /// <param name="newRules">Returns the list of firewall rules created</param>
    /// <param name="maintainUI">A by-reference variable containing the UI form when applicable</param>
    /// <returns>The list of firewall rules created</returns>
    internal static void AddFirewallRulesFor( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules, ref MaintainUI? maintainUI )
    {
        try
        {
            // newRules is an accumulating list
            CreateRulesForAddressSets( ruleName, siteName, ref newEntries, ref newRules );
            CreateRulesForAddressRanges( ruleName, siteName, ref newEntries, ref newRules );
        }
        catch ( Exception e )
        {
            StatusMessage( _appName, e, null );
            if ( maintainUI is not null )
                MessageBox.Show( StringUtilities.ExceptionMessage( "AddFirewallRulesFor", e ) );
        }

        return;
    }

    /// <summary>
    /// Create firewall rules  for IP addressRange ranges
    /// </summary>
    /// <param name="ruleName">The rule name to use</param>
    /// <param name="siteName">The name of the download site</param>
    /// <param name="newEntries">The list of all blocklist entries</param>
    /// <param name="newRules">Return the windows firewall rules which were created</param>
    /// <returns>True if completed without errors</returns>
    private static void CreateRulesForAddressRanges( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    {
        foreach ( var entry in newEntries.Where( w => w.Name == siteName && w.IPAddressRange is not null )
                                                     .OrderBy( o => o.Sort[ 0 ] )
                                                     .ThenBy( o => o.Sort[ 1 ] )
                                                     .ThenBy( o => o.Sort[ 2 ] )
                                                     .ThenBy( o => o.Sort[ 3 ] ) )
        {
            if ( !RuleExists( entry.IPAddressRange!, FirewallDirection.Outbound )//, ruleName )
                && !RuleExists( entry.IPAddressRange!, FirewallDirection.Inbound ) ) //, ruleName ) )
            {
                IPRange? addressRange = entry.IPAddressRange;
                newRules.AddRange( AddInboundAndOutboundRules( ruleName, null, entry.IPAddressRange/*, entry.Protocol, entry.Ports*/ ) );
            }
        }
    }

    /// <summary>
    /// Create firewall rules  for IP addressRange sets
    /// </summary>
    /// <param name="ruleName">The rule name to use</param>
    /// <param name="siteName">The name of the download site</param>
    /// <param name="newEntries">The list of all blocklist entries</param>
    /// <param name="newRules">Return the windows firewall rules which were created</param>
    /// <returns>True if completed without errors</returns>
    private static bool CreateRulesForAddressSets( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    {
        int savedCount = 0;

        foreach ( var entry in newEntries.Where( w => w.Name == siteName && w.IPAddressSet is not null && w.IPAddressSet.Length > 0 ) )
        {
            newRules.AddRange( AddInboundAndOutboundRules( ruleName, entry.IPAddressSet, entry.IPAddressRange/*, entry.Protocol, entry.Ports*/ ) );
            savedCount++;
        }

        return savedCount == newEntries.Where( w => w.Name == siteName && w.IPAddressSet is not null && w.IPAddressSet.Length > 0 ).Count( );
    }

    /// <summary>
    /// Determine whether 'adddress' is IP v4 or IP v6
    /// </summary>
    /// <param name="address">The addressRange to analyze</param>
    /// <returns></returns>
    internal static IPAddressType InternetAddressType( string address )
    {
        IPAddressType addressType = IPAddressType.Invalid;

        if ( IPAddress.TryParse( address, out IPAddress? ipAddress ) )
        {
            if ( ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork )
            {
                addressType = IPAddressType.IPv4;
            }
            else if ( ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 )
            {
                addressType = IPAddressType.IPv6;
            }
        }
        else
            StatusMessage( _appName, $"Invalid IP address {address} was ignored", null );

        return addressType;
    }

    private sealed record StringAndType( string AddressString, IAddress IpAddress, CandidateEntry Owner );

    /// <summary>
    /// Download blocklists from all sites or from each chosen site ( limited to 1 for the interactive app )
    /// </summary>
    /// <param name="maintainUIForm">The UI form if applicable</param>
    /// <param name="sitesToProcess">A list of download sites to download from ( limited to 1 for the interactive app )</param>
    /// <param name="logPath">The log file path (optional)</param>
    /// <returns>A list of the downloaded addressRange entries</returns>
    internal static List<CandidateEntry> DownloadBlocklists( MaintainUI? maintainUIForm, List<RemoteSite> sitesToProcess )
    {
        List<CandidateEntry> data = [];

        int counter = 0;
        if ( maintainUIForm is not null )
            maintainUIForm.UpdateProgress( 0, sitesToProcess.Count );

        foreach ( RemoteSite site in sitesToProcess )
        {
            counter++;
            string newLineText = counter == sitesToProcess.Count ? "\r\n" : string.Empty;
            try
            {
                List<CandidateEntry> newData = DownloadSiteData( site );
                if ( newData.Count > 0 )
                {
                    data.AddRange( newData );
                    newLineText = "";
                    StatusMessage( _appName, $"Read {site.Name} blocklist(s){newLineText} containing {newData.Count} entries", maintainUIForm );
                    if ( maintainUIForm is not null )
                    {
                        maintainUIForm.UpdateProgress( counter, sitesToProcess.Count );
                        maintainUIForm.StatusMessage.Text = $"Updating {site!.Name} downloaded date and time ...";
                    }

                    DateTime lastDownloaded = UpdateLastDownloaded( site );
                    if ( maintainUIForm is not null )
                    {
                        int siteRowIndex = maintainUIForm.RemoteSites.Rows.Cast<DataGridViewRow>( )
                                                                          .First( f => f.Cells[ "Name" ].Value!.ToString( ) == site.Name )
                                                                          .Index;
                        maintainUIForm.RemoteSites[ "LastDownloaded", siteRowIndex ].Value = lastDownloaded.ToString( "G", culture );
                        maintainUIForm.RemoteSites.Refresh( );
                    }
                }

                if ( maintainUIForm is not null )
                    maintainUIForm.UpdateProgress( counter, sitesToProcess.Count );
            }
            catch ( Exception ex )
            {
                StatusMessage( _appName, $"Downloading data from {site.Name} failed: {StringUtilities.ExceptionMessage( _appName, ex )}", maintainUIForm );
            }
        }

        return [ .. data.Select( s => new CandidateEntry( s.Name, s.FileName, s.IPAddress ?? "", s.Subnet, s.IPAddressRange, s.IPAddressSet/*, s.Ports, s.Protocol*/ ) )
                   .OrderBy( o => o.Sort0 )
                   .ThenBy( o => o.Sort1 )
                   .ThenBy( o => o.Sort2 )
                   .ThenBy( o => o.Sort3 ) ];
    }

    /// <summary>
    /// Reduce the number of entries for the firewall by bundling individual addresses into sets of the maximum size. Currently apparently 1000.
    /// </summary>
    /// <param name="data">The data to be rationlised </param>
    /// <param name="sites">The list of sites being processed</param>
    internal static List<CandidateEntry> ConvertIPAddressesToIPAddressSets( List<CandidateEntry> data, List<RemoteSite> sites )
    {
        // Preserve the data which isn't being manipulated in this step
        List<CandidateEntry> results = data.Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null )
                                           .ToList( );
        int unRationalisedCount = data.Count( w => !string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is null ), rationalisedCount = 0;

        // run per download site so that we don't get sets containing entries from multiple sites
        foreach ( RemoteSite site in sites )
        {
            List<CandidateEntry> work = data.Where( w => w.Name == site.Name ) // Validate
                                            .Where( w => !string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is null )
                                            .ToList( );

            while ( work.Count > 0 && work.Count > MAX_FIREWALL_BATCH_SIZE )
            {
                List<CandidateEntry> candidates = work.Take( MAX_FIREWALL_BATCH_SIZE )
                                                      .ToList( );

                results.Add( BuildIPAddressSet( candidates/*, sequence */, MAX_FIREWALL_BATCH_SIZE ) );
                // TESTING TESTING TESTING
                //work.RemoveAll( r => candidates.Contains( r ) );
                work = work.Except( candidates ).ToList( );
            }

            // Finally, add an entry for a set containing the leftover entries
            if ( work.Count > 0 )
            {
                List<CandidateEntry> candidates = work.ToList( );
                results.Add( BuildIPAddressSet( candidates, /*sequence, */candidates.Count ) );
            }
        }

        rationalisedCount = results.Where( w => w.IPAddressRange is null ).Count( );

        if ( rationalisedCount == 1 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entry" );
        else if ( rationalisedCount > 1 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries" );
        else if ( rationalisedCount > 0 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries: INVESTIGATE!" );

        return results;
    }

    /// <summary>
    /// Reduce the number of entries for the firewall by bundling individual addresses into sets of the maximum size. Currently apparently 1000.
    /// </summary>
    /// <param name="data">The data to be rationlised </param>
    /// <param name="sites">The list of sites being processed</param>
    internal static List<CandidateEntry> ConvertIPAddressRangesToIPAddressRangeSets( List<CandidateEntry> data, List<RemoteSite> sites )
    {
        List<CandidateEntry> results = data.Where( w => w.IPAddressRange is null )
                                           .ToList( );
        int unRationalisedCount = data.Count( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null ), rationalisedCount = 0;

        // This should be run per download site
        foreach ( RemoteSite site in sites )
        {
            List<CandidateEntry> work = data.Where( w => w.Name == site.Name ) // Validate
                                            .Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null )
                                            .ToList( );

            while ( work.Count > 0 && work.Count > MAX_FIREWALL_BATCH_SIZE )
            {
                List<CandidateEntry> candidates = work.Take( MAX_FIREWALL_BATCH_SIZE )
                                                      .ToList( );

                results.Add( BuildIPAddressRangeSet( candidates/*, sequence */, MAX_FIREWALL_BATCH_SIZE ) );
                rationalisedCount++;
                work = work.Except( candidates ).ToList( );
            }

            // Finally, add an entry for a set containing the leftover entries
            if ( work.Count > 0 )
            {
                List<CandidateEntry> candidates = work.ToList( );
                results.Add( BuildIPAddressRangeSet( candidates, /*sequence, */candidates.Count ) );
                rationalisedCount++;
            }
        }

        if ( rationalisedCount == 1 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entry" );
        else if ( rationalisedCount > 1 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries" );
        else if ( rationalisedCount > 0 )
            StatusMessage( _appName, $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries: INVESTIGATE!" );

        return results;
    }

    /// <summary>
    /// Removes entries belonging to private IP addressRange ranges (e.g. the 192.168 range)<CandidateEntry>
    /// </summary>
    /// <param name="candidates">The list to process</param>
    /// <param name="numberRemoved">Report back on how many were deleted</param>
    internal static void RemovePrivateAddressesRanges( ref List<CandidateEntry> candidates, out int numberRemoved )
    {
        StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        numberRemoved = candidates.Count;
        // Rebuild the list without any 'private' addressRange ranges; this is intended to be more performant than identifying all possibilities and then removing any matches found
        candidates = candidates.Where( x => !string.IsNullOrEmpty( x.IPAddress )
                                                    && !x.IPAddress!.StartsWith( "10.", comparison )
                                                    && !x.IPAddress!.StartsWith( "127.", comparison )
                                                    && !x.IPAddress!.StartsWith( "169.254.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.16.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.17.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.18.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.19.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.20.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.21.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.22.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.23.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.24.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.25.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.26.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.27.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.28.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.29.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.30.", comparison )
                                                    && !x.IPAddress!.StartsWith( "172.31.", comparison )
                                                    && !x.IPAddress!.StartsWith( "192.168.", comparison )
                                    )
                                // Only apply the same check to addressRange ranges if this becomes necessary
                                .Union( candidates.Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange != null ) )
                                .ToList( );

        numberRemoved -= candidates.Count;
    }

    /// <summary>
    /// Removes entries containing invalid IP addresses<CandidateEntry>
    /// </summary>
    /// <param name="data">The list to process</param>
    /// <param name="numberRemoved">Report back on how many were deleted</param>
    internal static void RemoveInvalidAddresses( ref List<CandidateEntry> data, out int numberRemoved )
    {
        numberRemoved = data.Count;
        string[] invalidIPs = data.Where( w => w.AddressType == IPAddressType.IPv4 || w.AddressType == IPAddressType.Invalid )
                                  .Where( w => !string.IsNullOrEmpty( w.IPAddress ) )
                                  .Select( s => new { IPAddress = s.IPAddress!, Parts = s.IPAddress!.Split( '.' ) } )
                                  .Where( w => w.Parts.Length < 4 )
                                  .Select( s => s.IPAddress! )
                                  .Union( [ "0.0.0.0" ] )
                                  .ToArray( );

        foreach ( string ipAddress in invalidIPs )
        {
            CandidateEntry? candidateEntry = data.Find( f => f.IPAddress == ipAddress );
            if ( candidateEntry != null )
            {
                data.Remove( candidateEntry );
            }
        }

        numberRemoved -= data.Count;
    }

    /// <summary>
    /// Convert IP addresses with subnets (e.g. 1.2.3.0/24) to ip addressRange ranges
    /// </summary>
    /// <param name="data">A list of IP addressRange data to process</param>
    /// <param name="numberConverted">Report back the number of addresses converted</param>
    internal static void SubnetsToRanges( ref List<CandidateEntry> data, out int numberConverted )
    {
        numberConverted = 0;
        foreach ( CandidateEntry entry in data.Where( w => !string.IsNullOrEmpty( w.IPAddress ) && w.Subnet != null ) )
        {
            try
            {
                string addressWithSubnet = $"{entry.IPAddress!.ToString( )}/{Convert.ToString( entry.Subnet, CultureInfo.InvariantCulture )}";
                entry.IPAddressRange = new IPRange( IPAddressRange.Parse( addressWithSubnet ).Begin, IPAddressRange.Parse( addressWithSubnet ).End );
                entry.IPAddress = null;
                entry.Subnet = null;
            }
            catch ( Exception ex )
            {
                StatusMessage( _appName, ex, null );
            }
        }

        return;
    }

    /// <summary>
    /// Convert a string too a byte array
    /// </summary>
    /// <param name="source">The string to convert</param>
    /// <returns></returns>
    internal static byte[] StringToBytes( string source )
    {
        System.Text.ASCIIEncoding enc = new( );
        return enc.GetBytes( source );
    }

    /// <summary>
    /// Removes entries duplicating IP addresses or ranges in a List<CandidateEntry>
    /// </summary>
    /// <param name="data">The list to process</param>
    /// <param name="numberRemoved">Report back on how many were deleted</param>
    internal static void RemoveDuplicates( ref List<CandidateEntry> data, out int numberRemoved )
    {
        numberRemoved = data.Count;
        // Address ranges stay as they are; not attempting deduplication
        data = data.Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null )
                   .GroupBy( g => g.IPAddressRange )
                   .Select( s => new { IPAddressRange = s.Key, data = s.AsEnumerable( ) } )
                   .Select( s => new CandidateEntry
                                                                                        (
                                                                                            s.data.Min( m => m.Name ),
                                                                                            s.data.Min( m => m.FileName )!,
                                                                                            null,
                                                                                            null,
                                                                                            s.IPAddressRange,
                                                                                            s.data.Min( m => m.IPAddressSet ) ?? []/*,
                                                                                            s.data.Min( m => m.Ports ?? [] )!,
                                                                                            s.data.Min( m => m.Protocol ?? FirewallProtocol.Any )! */
                                                                                        )
                    )
                   // Deduplicate IPAddress entries
                   .Union( data.Where( w => !string.IsNullOrEmpty( w.IPAddress ) )
                               .GroupBy( g => g.IPAddress )
                               .Select( s => new { IPAddress = s.Key, data = s.AsEnumerable( ) } )
                               .Select( s =>
                                    new CandidateEntry( s.data.Min( m => m.Name ), s.data.Min( m => m.FileName )!, s.IPAddress, s.data.Min( m => m.Subnet ), null, []/*, MergeShorts( s.data.Select( t => t.Ports ) ), s.data.Max( m => m.Protocol ?? FirewallProtocol.Any )!*/ )
                               )
                    )
                   .ToList( );

        numberRemoved -= data.Count;
    }

    // Obsolete
    //private static ushort[] MergeShorts( IEnumerable<ushort[]> shorts )
    //{
    //    List<ushort> results = [];

    //    string test = string.Join( ';', shorts.Select( s => string.Join( ';', s.ToArray( ) ) ).ToArray( ) );
    //    if ( !string.IsNullOrEmpty( test ) && test.Length > 1 )
    //    {
    //        List<string> pre = [];
    //        foreach ( string x in test.Split( ';' ) )
    //        {
    //            if ( string.IsNullOrEmpty( x ) )
    //                continue;
    //            else
    //                pre.Add( x );
    //        }

    //        if ( pre.Count > 0 )
    //        {
    //            results = pre.Select( p => (ushort)Convert.ToInt16( p, CultureInfo.InvariantCulture ) )
    //                         .Distinct( )
    //                         .ToList( );
    //        }
    //    }

    //    return [ .. results ];
    //}

    private static List<CandidateEntry> DownloadSiteData( RemoteSite site )
    {
        List<CandidateEntry> data = [];
        foreach ( string fileUrl in site.FilePaths )
        {
            string url = fileUrl.Replace( ",", "" ); //.ToLower();
            string fileName = string.Empty;
            switch ( site.FileTypeID )
            {
                case 2: // Json
                    {
                        string fileExtension = string.Empty;
                        string textData = Downloader.ReadData( site, out fileExtension!, url );
                        if ( textData.Length > 2 )
                        {
                            using DataTranslatorJson translator = new( );
                            return translator.TranslateFileData( site, textData, fileName );
                        }
                        break;
                    }
                case 3: // Xml
                    {
                        Stream? downloaded = Downloader.ReadHtmlStreamFromUrl( site, fileUrl );

                        if ( downloaded is not null )
                        {
                            using DataTranslatorXml translator = new( );
                            data = translator.TranslateDataStream( site, downloaded, fileName );
                        }

                        break;
                    }
                case 4: // Tab delimited
                case 9: // Comma delimited
                    {
                        string downloaded = Downloader.ReadData( site, out string? fileExtension, url );

                        if ( !string.IsNullOrEmpty( downloaded ) )
                        {
                            using DataTranslatorDelimited translator = new( );
                            data = translator.TranslateFileData( site, downloaded, fileName );
                        }

                        break;
                    }
                case 5: // Zip archive containing Json
                case 6: // Zip archive containing text
                case 7: // Zip archive containing delimited data
                    {
                        // Not in use; not finished
                        // Extract the data file from the ZIP archive
                        //string fileContents = HttpHelper.ReadZipFileContents( site, url, out string extension );
                        string fileContents = Downloader.ReadZipData( site, out string extension, url );

                        // Process the extracted data
                        data = site.FileTypeID switch
                        {
                            5 => [],
                            6 => [],
                            7 => [],
                            _ => []
                        };

                        break;
                    }
                default:
                    {
                        //HttpHelper.PrepareEntriesFromUrl_Text( site, url, ref data, logPath );
                        string fileExtension = string.Empty;
                        try
                        {
                            string textData = Downloader.ReadData( site, out fileExtension!, url );
                            if ( textData.Length > 0 )
                            {
                                using DataTranslatorText translator = new( );
                                data = translator.TranslateFileData( site, textData, fileName );
                            }
                        }
                        catch ( Exception ex )
                        {
                            StatusMessage( _appName, StringUtilities.ExceptionMessage( "DownloadSiteData", ex ) );
                        }

                        break;
                    }
            }

            if ( site.ID == 7 && data.Count > 15000 )
            {
                Console.WriteLine( "CINS: File data exceeds 15000 entries!" );
                Console.ReadLine( );
            }
        }

        return data;
    }

    /// <summary>
    /// Converts a string to an array of strings for lines identified by NewLine
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    internal static List<string> TextToStringList( string text )
    {
        List<string> asList = [];
        string temp = text.Replace( Environment.NewLine, "\n" );
        if ( temp.Contains( '\n' ) )
            asList.AddRange( temp.Split( '\n' ) );

        return asList;
    }

    internal static bool UrlHostExists( string url )
    {
        string domain = url;
        StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        if ( domain.Contains( ':' ) )
            domain = domain[ ( domain.IndexOf( "://", comparison ) + 3 ).. ];

        if ( domain.Contains( '/' ) )
            domain = domain[ ..domain.IndexOf( '/' ) ];

        bool exists = false;
        try
        {
            exists = Dns.GetHostAddresses( domain ).Length > 0;
        }
        catch { }

        //if ( exists )
        //    url = domain;

        return exists;
    }

    /// <summary>
    /// Merge a batch of entries into an IPAddressSet( iAddress[] )
    /// </summary>
    /// <param name="entriesForBatch">Only has the entries to batch, not any others</param>
    /// <param name="sequence"></param>
    /// <param name="batchSize">The number of entries to batch - redundant?></param>
    /// <returns>A consolidated entry for as many entries as possible</returns>
    private static CandidateEntry BuildIPAddressSet( List<CandidateEntry> entriesForBatch, /*int sequence, */ int batchSize )
    {

        if ( batchSize == 0 )
            batchSize = entriesForBatch.Count;

        if ( batchSize > MAX_FIREWALL_BATCH_SIZE )
        {
            MessageBox.Show( $"The maximum number of addresses the Windows Firewall currently accepts per entry is {MAX_FIREWALL_BATCH_SIZE}.\r\nOnly the first {MAX_FIREWALL_BATCH_SIZE} are being grouped." );
            batchSize = MAX_FIREWALL_BATCH_SIZE;
        }

        if ( entriesForBatch.Any( c => c.IPAddressRange is not null || c.IPAddress is null ) )
        {
            MessageBox.Show( $"BuildIPAddressSet: Invalid entries were provided for processing and will be excluded." );
        }

        List<StringAndType> addressSet = entriesForBatch.Where( w => w.IPAddress is not null && w.IPAddressRange is null )
                                             //                                             .Take( batchSize )
                                             .Select( s => new StringAndType( s.IPAddress!, StringToIAddress( s.IPAddress! ), s ) )
                                             .ToList( );

        CandidateEntry result = entriesForBatch.First( );
        if ( addressSet.Count > 0 )
        {
            result.IPAddressSet = addressSet.Select( s => s.IpAddress )
                                            .ToArray( );
            result.IPAddress = null;
        }

        return result;
    }

    /// <summary>
    /// Merge a batch of entries into an IPAddressSet( iAddress[] )
    /// </summary>
    /// <param name="entriesForBatch">Only has the entries to batch, not any others</param>
    /// <param name="sequence"></param>
    /// <param name="batchSize">The number of entries to batch - redundant?></param>
    /// <returns>A consolidated entry for as many entries as possible</returns>
    private static CandidateEntry BuildIPAddressRangeSet( List<CandidateEntry> entriesForBatch, /*int sequence, */ int batchSize )
    {
        if ( batchSize == 0 )
            batchSize = entriesForBatch.Count;

        if ( batchSize > MAX_FIREWALL_BATCH_SIZE )
        {
            MessageBox.Show( $"The maximum number of addressRange ranges the Windows Firewall currently accepts per entry is {MAX_FIREWALL_BATCH_SIZE}.\r\nOnly the first {MAX_FIREWALL_BATCH_SIZE} are being grouped." );
            batchSize = MAX_FIREWALL_BATCH_SIZE;
        }

        if ( entriesForBatch.Any( c => c.IPAddressRange is null || c.IPAddress is not null ) )
        {
            MessageBox.Show( $"BuildIPAddressRangeSet: Invalid entries were provided for processing and will be excluded." );
        }

        List<StringAndType> addressRangeSet = entriesForBatch.Where( w => w.IPAddress is null && w.IPAddressRange is not null )
                                                             .Select( s => new StringAndType( s.IPAddressRange!.ToString( ), StringToIAddressRange( s.IPAddressRange!.ToString( ) ), s ) )
                                                             .ToList( );

        CandidateEntry result = entriesForBatch.First( );
        if ( addressRangeSet.Count > 0 )
        {
            result.IPAddressSet = addressRangeSet.Select( s => s.IpAddress )
                                                 .ToArray( );
            result.IPAddressRange = null;
        }

        return result;
    }

    /// <summary>
    /// Clean up downloaded lists after downloading them
    /// </summary>
    /// <param name="sites">The sites to download for</param>
    /// <param name="maintainUI">User interface form when applicable</param>
    /// <returns>A list containing standardised and cleaned up downloaded blocklist data</returns>
    internal static List<CandidateEntry> ProcessDownloads( List<RemoteSite> sites, MaintainUI? maintainUI, bool createFirewallRules, out int numberOfRules, out int ipAddressCount )
    {
        // createFirewallRules should only be true when the program is running from command line or scheduler
        StatusMessage( _appName!, "Blocklist downloads started... ", maintainUI );
        List<CandidateEntry> candidateRules = DownloadBlocklists( maintainUI, sites )!;
        List<IFirewallRule> newRules = [];
        numberOfRules = 0;
        candidateRules = CleanupDownloadedIPAddressData( sites, maintainUI, createFirewallRules, candidateRules, out ipAddressCount );
        if ( maintainUI is not null )
        {
            maintainUI.RemoteData.DataSource = candidateRules
                .Select( s => new { s.Name, s.AddressType, s.IPAddress, Range = s.IPAddressRange, AddressBatch = ( s.IPAddressBatch ?? "" ).Length > 15000 ? s.IPAddressBatch!.Substring( 0, 15000 ) : s.IPAddressBatch ?? "", s.FileName } )
                .ToList( );
            maintainUI.RemoteData.Refresh( );
        }

        if ( createFirewallRules && candidateRules is not null && candidateRules.Count > 0 )
        {
            newRules = ReplaceFirewallRules( sites, maintainUI, candidateRules );
            numberOfRules = newRules.Count;
            if ( maintainUI is not null )
            {
                maintainUI.FirewallRulesData.DataSource = newRules;
                maintainUI.FirewallRulesData.ForeColor = Color.Green;
                maintainUI.FirewallRulesData.Refresh( );
            }
        }

        return candidateRules!;
    }

    private static List<CandidateEntry> CleanupDownloadedIPAddressData( List<RemoteSite> sites, MaintainUI? maintainUI, bool createFirewallRules, List<CandidateEntry> candidateRules, out int ipAddressCount )
    {
        ipAddressCount = candidateRules.Count;

        if ( maintainUI is not null )
        {
            maintainUI.UpdateProgress( 0, 7 );
        }

        if ( candidateRules.Count > 0 )
        {
            StatusMessage( _appName, "Removing private addressRange ranges ...", maintainUI );
            RemovePrivateAddressesRanges( ref candidateRules, out int numberRemoved );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 1, 7 );

            StatusMessage( _appName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out numberRemoved );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 2, 7 );

            StatusMessage( _appName, "Removing any invalid addresses ...", maintainUI );
            RemoveInvalidAddresses( ref candidateRules, out numberRemoved );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 3, 7 );

            StatusMessage( _appName, "Convert any IP address subnets to address ranges ...", maintainUI );
            SubnetsToRanges( ref candidateRules, out int numberConverted );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 4, 7 );

            // Rerun deduplication in case subnet conversion introduced duplicate ranges
            StatusMessage( _appName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out numberRemoved );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 5, 7 );

            StatusMessage( _appName, $"Consolidating {candidateRules.Count( c => c.IPAddressRange is null )} addresses into sets of {MAX_FIREWALL_BATCH_SIZE} ...", maintainUI );
            candidateRules = ConvertIPAddressesToIPAddressSets( candidateRules, sites );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 6, 7 );

            StatusMessage( _appName, $"Consolidating {candidateRules.Count( c => c.IPAddressRange is not null )} address ranges into sets of {MAX_FIREWALL_BATCH_SIZE} ...", maintainUI );
            candidateRules = ConvertIPAddressRangesToIPAddressRangeSets( candidateRules, sites );
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( 7, 7 );
            StatusMessage( _appName, $"Consolidation into {candidateRules.Count( c => c.IPAddressRange is null )} address sets and {candidateRules.Count( c => c.IPAddressRange is not null )} address range sets completed succesfully\r\n", maintainUI );
        }

        return candidateRules!;
    }

    internal static List<IFirewallRule> ReplaceFirewallRules( List<RemoteSite> sites, MaintainUI? maintainUI, List<CandidateEntry> candidateRules )
    {
        List<IFirewallRule> rules = [];
        int counter = 0;
        if ( maintainUI is not null )
            maintainUI.UpdateProgress( 0, sites.Count );

        foreach ( RemoteSite site in sites )
        {
            counter++;
            try
            {
                ReplaceSiteRules( site, candidateRules, ref rules, maintainUI );
            }
            catch ( Exception ex )
            {
                StatusMessage( _appName, ex, maintainUI );
            }

            if ( maintainUI is not null )
                maintainUI.UpdateProgress( counter, sites.Count );
        }

        StatusMessage( _appName, "Firewall rule creation completed successfully" );

        return rules;
    }

    internal static void ReplaceSiteRules( RemoteSite site, List<CandidateEntry> candidateRules, ref List<IFirewallRule> newRules, MaintainUI? maintainUI )
    {
        string ruleName = $"{site.Name}_Blocklist";

        int ruleCount = FirewallManager.Instance.Rules.Where( r => r.Name == ruleName ).Count( );

        // Delete all rules added by this program
        StatusMessage( _appName, $"Removing {ruleCount} existing firewall rules for the {site!.Name} blocklist(s) ...", maintainUI );
        DeleteExistingFirewallRulesFor( ruleName );
        StatusMessage( _appName, $"Existing firewall rules for the {site!.Name} blocklist(s) were removed", maintainUI );

        ruleCount = candidateRules.Where( w => w.Name == site.Name ).Count( ) * 2; /* Inbound AND Outbound rules will be created */

        // Add all of the rules that we've just imported
        StatusMessage( _appName, $"Creating {ruleCount} new firewall rules for the {site!.Name} blocklist(s) ...", maintainUI );
        AddFirewallRulesFor( ruleName, site.Name, ref candidateRules, ref newRules, ref maintainUI );
        int siteCount = newRules.Count( c => c.Name == site.Name + "_Blocklist" );
        StatusMessage( _appName, $"{siteCount} New firewall rules for the {site.Name} blocklist(s) were created successfully\r\n", maintainUI );
    }

    internal static void StatusMessage( string caller, string message, MaintainUI? maintainUI = null )
    {
        if ( string.IsNullOrEmpty( Logger.LogPath ) )
            Logger.LogPath = LogFileFullname;

        Logger.Log( caller, message );

        if ( maintainUI is not null )
        {
            maintainUI.StatusMessage.Text = message;
            maintainUI.Refresh( );
        }
    }

    internal static void StatusMessage( string caller, Exception ex, MaintainUI? maintainUI )
    {
        string message = StringUtilities.ExceptionMessage( string.Empty, ex );
        StatusMessage( caller, message, maintainUI );
    }
}
