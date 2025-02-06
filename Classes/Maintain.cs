using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Models;

using SBS.Utilities;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.FirewallRules;

namespace BlocklistManager.Classes;

internal static class Maintain
{
    internal const int MAX_FIREWALL_BATCH_SIZE = 1000;
    private const char BATCH_DELIMITER = ';';
    private static readonly string _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;
    internal static string LogFileFullname = Assembly.GetEntryAssembly( )!.FullName!.Replace( ".exe", ".log" );
    private static string? macAddress = GetMACAddress( ); // "00:00:00:00:00:00";
    private static Device? device;

    internal static List<FileType> FILETYPES => ( new BlocklistData( ) ).ListFileTypes( );

    internal static FirewallProfiles _AllProfiles = FirewallProfiles.Public | FirewallProfiles.Domain | FirewallProfiles.Private;

    internal static Device? ConnectedDevice
    {
        get
        {
            if ( device is null )
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

    internal record Adapter( string NetworkType, string MACAddress );

    /// <summary>
    /// Get the client computer's MAC address for use in identifying the device
    /// NOTE: This currently only considers Ethernet and 802.11 WiFi adapters with IP addresses and an active gateway, add other types as required
    /// </summary>
    /// <returns>The MAC address of the first active Ethernet or WiFi adapter found</returns>
    public static string? GetMACAddress( )
    {
        List<Adapter> macAddresses =
            NetworkInterface.GetAllNetworkInterfaces( )
                            .Where( w => w.OperationalStatus == OperationalStatus.Up )
                            .Where( w => w.NetworkInterfaceType == NetworkInterfaceType.Ethernet || w.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 )
                            .Where( w => w.GetIPProperties( ) != null && ( w.GetIPProperties( ).GetIPv4Properties( ) != null ) || w.GetIPProperties( ).GetIPv6Properties( ) != null )
                            .Where( w => w.GetPhysicalAddress( ).ToString( ).Length >= 12 )
                            .Where( w => w.GetIPProperties( ).GatewayAddresses.Count > 0 )
                            .Select( s => new Adapter
                                (
                                    s.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : "WiFi",
                                    MACAddress( s.GetPhysicalAddress( ) )
                                ) )
                            .OrderBy( o => o.NetworkType )
                            .ToList( );

        return macAddresses.Count > 0 ? macAddresses.First( ).MACAddress : null;
    }

    /// <summary>
    /// Formats the MAC address as a string, "00:00:00:00:00:00" if no valid physical address is found
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

    //internal static void EnsureStartupDataExists(/*BlocklistContext store*/)
    //{
    //    try
    //    {
    //        using BlocklistDbContext store = new( );
    //        store.Database.EnsureCreated( );
    //        if ( store.Database.GetService<IDatabaseCreator>( ) is RelationalDatabaseCreator databaseCreator )
    //        {
    //            try
    //            {
    //                databaseCreator.CreateTables( );
    //            }
    //            catch
    //            {
    //                //A SqlException will be thrown if tables already exist, so simply ignore it.
    //            }
    //        }
    //        else
    //        {
    //            throw new InvalidCastException( "Database creation service is null" );
    //        }

    //        store.EnsureDataExists( );
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve message
    //    }
    //}

    //internal static RemoteSite? AddRemoteSite( RemoteSite remoteSite )
    //{
    //    using BlocklistDbContext ctx = new( );
    //    RemoteSite? newSite = null;

    //    if ( remoteSite.ID > 0 || ctx.RemoteSites.Any( c => c.Name == remoteSite.Name ) )
    //    {
    //        return UpdateRemoteSite( remoteSite );
    //    }
    //    else
    //    {
    //        ctx.RemoteSites.Add( remoteSite );
    //        ctx.SaveChanges( );
    //        newSite = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );
    //    }

    //    return newSite;
    //}

    //internal static bool DeleteRemoteSite( RemoteSite remoteSite )
    //{
    //    using BlocklistDbContext ctx = new( );
    //    bool deleted = false;

    //    try
    //    {
    //        ctx.RemoteSites.Remove( remoteSite );
    //        ctx.SaveChanges( );
    //        if ( !ctx.RemoteSites.Any( c => c.Name == remoteSite.Name ) )
    //        {
    //            deleted = true;
    //        }
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return deleted;
    //}

    //internal static RemoteSite? UpdateRemoteSite( RemoteSite remoteSite )
    //{
    //    using BlocklistDbContext ctx = new( );
    //    RemoteSite? existing = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );

    //    if ( existing != null )
    //    {
    //        existing.SiteUrl = remoteSite.SiteUrl;
    //        existing.FileUrls = remoteSite.FileUrls;
    //        existing.FileType = remoteSite.FileType;
    //        existing.Active = remoteSite.Active;
    //        ctx.SaveChanges( );
    //        existing = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );
    //    }
    //    else
    //    {
    //        MessageBox.Show( $"No site matching '{remoteSite.Name}' was found." );
    //    }

    //    return existing;
    //}

    internal static bool UpdateLastDownloaded( RemoteSite site )
    {
        int deviceID = ConnectedDevice!.ID;
        var result = new BlocklistData( ).SetLastDownloaded( ConnectedDevice!.ID, site.ID );

        //    try
        //    {
        //        using BlocklistDbContext ctx = new( );
        //        ctx.SetDownloadedDateTime( site );
        //        return true;
        //    }

        //    catch
        //    {
        return true;
        //    }
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
                    .ThenBy( o => o.SortValue[ 0 ] )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 1 ] : 0 )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 2 ] : 0 )
                    .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 3 ] : 0 ) ];
    }

    /// <summary>
    /// Delete all rules for a blocklist download site
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
    /// Add both inbound and outbound rules for an IP address array or range
    /// </summary>
    /// <param name="ruleName">The name to use for the rule</param>
    /// <param name="ipAddressSet">an IP address array</param>
    /// <param name="ipAddressRange">an IP address range</param>
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

    private static SingleIP StringToIAddress( string address )
    {
        return new( IPAddress.Parse( address ) );
    }

    /// <summary>
    /// Convert an array of IAddress to a delimited string
    /// </summary>
    /// <param name="addresses"></param>
    /// <returns></returns>
    internal static string? IAddressesToString( IAddress[] addresses )
    {
        if ( addresses.Length > 0 )
            return string.Join( BATCH_DELIMITER, addresses.Select( s => s.ToString( ) ).ToArray( ) );
        else
            return string.Empty;
    }

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

    internal static IList<IFirewallRule> AddFirewallRulesFor( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules, ref MaintainUI? maintainUI )
    {
        try
        {
            // newRules is an accumulating list
            CreateRulesForAddressSets( ruleName, siteName, ref newEntries, ref newRules );
            CreateRulesForAddressRanges( ruleName, siteName, ref newEntries, ref newRules );
        }
        catch ( Exception e )
        {
            //if ( LogFileFullname != null )
            StatusMessage( _appName, e, null );
            //Logger.Log( _appName, e );
            //else
            if ( maintainUI is not null )
                MessageBox.Show( StringUtilities.ExceptionMessage( "AddFirewallRulesFor", e ) );
        }

        return newRules;
    }

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
                newRules.AddRange( AddInboundAndOutboundRules( ruleName, null, entry.IPAddressRange, /*entry.Protocol, */entry.Ports ) );
            }
        }
    }

    private static bool CreateRulesForAddressSets( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    {
        int savedCount = 0;

        // NOTE: IP V6 addresses are currently in the IPAddress property
        foreach ( var entry in newEntries.Where( w => w.Name == siteName && w.IPAddressSet is not null && w.IPAddressSet.Length > 0 ) )
        {
            newRules.AddRange( AddInboundAndOutboundRules( ruleName, entry.IPAddressSet, entry.IPAddressRange, /*entry.Protocol, */entry.Ports ) );
            savedCount++;
        }

        return savedCount == newEntries.Where( w => w.Name == siteName && w.IPAddressSet is not null && w.IPAddressSet.Length > 0 ).Count( );
    }

    internal static IPAddressType InternetAddressType( string address )
    {
        IPAddressType addressType = IPAddressType.Invalid;
        // IPAddress ipAddress = IPAddress.None;

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
            StatusMessage( _appName, $"Invalid address {address} was ignored", null );

        return addressType;
    }

    //internal static byte[] StringToBytes( string source )
    //{
    //    System.Text.ASCIIEncoding enc = new( );
    //    return enc.GetBytes( source );
    //}

    private sealed record StringAndType( string AddressString, IAddress IpAddress, CandidateEntry Owner );

    /// <summary>
    /// Download blocklists from all sites or from each chosen site ( limited to 1 for the interactive app )
    /// </summary>
    /// <param name="maintainUIForm">The UI form if applicable</param>
    /// <param name="sitesToProcess">A list of download sites to download from ( limited to 1 for the interactive app )</param>
    /// <param name="logPath">The log file path (optional)</param>
    /// <returns>A list of the downloaded address entries</returns>
    internal static List<CandidateEntry> DownloadBlocklists( MaintainUI? maintainUIForm, List<RemoteSite> sitesToProcess )
    {
        List<CandidateEntry> data = [];

        int counter = 0;
        foreach ( RemoteSite site in sitesToProcess )
        {
            counter++;
            string eol = counter == sitesToProcess.Count ? "\r\n" : string.Empty;
            List<CandidateEntry> newData = DownloadSiteData( site );
            if ( newData.Count > 0 )
            {
                data.AddRange( newData );

                StatusMessage( _appName, $"Downloaded {site.Name} blocklist(s){eol}", maintainUIForm );

                if ( maintainUIForm is not null )
                    maintainUIForm.StatusMessage.Text = $"Updating {site!.Name} downloaded date and time ...";

                // TODO: Add DeviceID so that date last downloaded can be set
                // TODO: Find out where the log is being written to if not set

                UpdateLastDownloaded( site );
            }
        }

        return [ .. data.Select( s => new CandidateEntry( s.Name, s.IPAddress ?? "", s.IPAddressRange, s.IPAddressSet, s.Ports, s.Protocol ) )
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
    internal static void ConvertIPAddressesToIPAddressSets( ref List<CandidateEntry> data, List<RemoteSite> sites )
    {
        List<CandidateEntry> rationalised = [];

        // This should be run per download site
        foreach ( RemoteSite site in sites )
        {
            List<CandidateEntry> work = data.Where( w => w.Name == site.Name ) // Validate
                                            .Where( w => !string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is null )
                                            .ToList( );

            while ( work.Count > 0 && work.Count > MAX_FIREWALL_BATCH_SIZE )
            {
                List<CandidateEntry> candidates = work.Take( MAX_FIREWALL_BATCH_SIZE )
                                                      .ToList( );

                rationalised.Add( BuildIPAddressSet( candidates/*, sequence */) );
                foreach ( var candidate in candidates ) // I'd love to find a better way to do this. I can't count on getting the correct arguments for RemoveRange
                {
                    work.Remove( candidate );
                }
            }

            // Finally, add an entry for a set containing the leftover entries
            if ( work.Count > 0 )
                rationalised.Add( BuildIPAddressSet( work, /*sequence, */work.Count ) );

            rationalised.AddRange( data.Where( w => w.Name == site.Name ) // Validate
                                       .Where( w => w.IPAddressRange is not null && w.IPAddressRange.StartAddress is not null && w.IPAddressRange.EndAddress is not null ) );
        }

        data.Clear( );
        data.AddRange( rationalised );
        rationalised.Clear( );
    }

    internal static void RemovePrivateAddressesRanges( ref List<CandidateEntry> candidates, out int numberRemoved )
    {
        StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        numberRemoved = candidates.Count;
        // Rebuild the list without any 'private' address ranges; this is intended to be more performant than identifying all possibilities and then removing any matches found
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
                                // Only apply the same check to address ranges if this becomes necessary
                                .Union( candidates.Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange != null ) )
                                .ToList( );

        numberRemoved -= candidates.Count;
    }

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
                                                                                            null,
                                                                                            s.IPAddressRange,
                                                                                            s.data.Min( m => m.IPAddressSet ) ?? [],
                                                                                            s.data.Min( m => m.Ports ?? [] )!,
                                                                                            s.data.Min( m => m.Protocol ?? FirewallProtocol.Any )!
                                                                                        )
                    )
                   // Deduplicate IPAddress entries
                   .Union( data.Where( w => !string.IsNullOrEmpty( w.IPAddress ) )
                               .GroupBy( g => g.IPAddress )
                               .Select( s => new { IPAddress = s.Key, data = s.AsEnumerable( ) } )
                               .Select( s =>
                                    new CandidateEntry( s.data.Min( m => m.Name ), s.IPAddress, null, [], MergeShorts( s.data.Select( t => t.Ports ) ), s.data.Max( m => m.Protocol ?? FirewallProtocol.Any )! )
                               )
                    )
                   .ToList( );

        numberRemoved -= data.Count;
    }

    private static ushort[] MergeShorts( IEnumerable<ushort[]> shorts )
    {
        List<ushort> results = [];

        string test = string.Join( ';', shorts.Select( s => string.Join( ';', s.ToArray( ) ) ).ToArray( ) );
        if ( !string.IsNullOrEmpty( test ) && test.Length > 1 )
        {
            List<string> pre = [];
            foreach ( string x in test.Split( ';' ) )
            {
                if ( string.IsNullOrEmpty( x ) )
                    continue;
                else
                    pre.Add( x );
            }

            if ( pre.Count > 0 )
            {
                results = pre.Select( p => (ushort)Convert.ToInt16( p, CultureInfo.InvariantCulture ) )
                             .Distinct( )
                             .ToList( );
            }
        }

        return [ .. results ];
    }

    private static List<CandidateEntry> DownloadSiteData( RemoteSite site )
    {
        List<CandidateEntry> data = [];
        foreach ( string fileUrl in site.FilePaths )
        {
            string url = fileUrl.Replace( ",", "" ); //.ToLower();
            switch ( site.FileTypeID )
            {
                case 2: // Json
                    {
                        string fileExtension = string.Empty;
                        string textData = Downloader.ReadData( site, out fileExtension!, url );
                        if ( textData.Length > 2 )
                        {
                            using DataTranslatorJson translator = new( );
                            return translator.TranslateFileData( site, textData );
                        }
                        break;
                    }
                case 3: // Xml
                    {
                        Stream? downloaded = Downloader.ReadHtmlStreamFromUrl( site, fileUrl );

                        if ( downloaded is not null )
                        {
                            using DataTranslatorXml translator = new( );
                            data = translator.TranslateDataStream( site, downloaded );
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
                            data = translator.TranslateFileData( site, downloaded );
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
                        string textData = Downloader.ReadData( site, out fileExtension!, url );
                        if ( textData.Length > 0 )
                        {
                            using DataTranslatorText translator = new( );
                            data = translator.TranslateFileData( site, textData );
                        }
                        break;
                    }
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
    private static CandidateEntry BuildIPAddressSet( List<CandidateEntry> entriesForBatch, /*int sequence, */ int batchSize = MAX_FIREWALL_BATCH_SIZE )
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
        candidateRules = CleanupDownloadedIPAddressData( sites, maintainUI, createFirewallRules, candidateRules, out ipAddressCount );
        numberOfRules = candidateRules.Count;
        return candidateRules!;
    }

    private static List<CandidateEntry> CleanupDownloadedIPAddressData( List<RemoteSite> sites, MaintainUI? maintainUI, bool createFirewallRules, List<CandidateEntry> candidateRules, out int ipAddressCount )
    {
        ipAddressCount = candidateRules.Count;
        if ( candidateRules.Count > 0 )
        {
            StatusMessage( _appName, "Removing private address ranges ...", maintainUI );
            RemovePrivateAddressesRanges( ref candidateRules, out int numberRemoved );
            StatusMessage( _appName, $"Removed {numberRemoved} private address ranges", maintainUI );

            StatusMessage( _appName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out numberRemoved );
            StatusMessage( _appName, $"Removed {numberRemoved} duplicates", maintainUI );

            StatusMessage( _appName, "Removing any invalid addresses ...", maintainUI );
            RemoveInvalidAddresses( ref candidateRules, out numberRemoved );
            StatusMessage( _appName, $"Removed {numberRemoved} invalid addresses", maintainUI );

            StatusMessage( _appName, $"Consolidating addresses into sets of {MAX_FIREWALL_BATCH_SIZE} ...", maintainUI );
            ConvertIPAddressesToIPAddressSets( ref candidateRules, sites );
            StatusMessage( _appName, $"Consolidation completed succesfully\r\n", maintainUI );

            if ( maintainUI is not null )
            {
                maintainUI.RemoteData.DataSource = candidateRules;
                maintainUI.RemoteData.Refresh( );
            }

            if ( createFirewallRules && candidateRules is not null && candidateRules.Count > 0 )
            {
                List<IFirewallRule> rules = [];
                foreach ( RemoteSite site in sites )
                {
                    try
                    {
                        ReplaceSiteRules( site, candidateRules, ref rules, maintainUI );
                    }
                    catch ( Exception ex )
                    {
                        StatusMessage( _appName, ex, maintainUI );
                        return candidateRules;
                    }
                }

                if ( maintainUI is not null )
                    maintainUI.StatusMessage.Text = "Firewall rule creation completed successfully";
            }
        }

        return candidateRules!;
    }

    internal static void ReplaceSiteRules( RemoteSite site, List<CandidateEntry> candidateRules, ref List<IFirewallRule> newRules, MaintainUI? maintainUI )
    {
        string ruleName = $"{site.Name}_Blocklist";
        // List<IFirewallRule> newRules = [];

        // Delete all rules added by this program
        StatusMessage( _appName, $"Removing existing rules for the {site!.Name} blocklist(s) ...", maintainUI );
        DeleteExistingFirewallRulesFor( ruleName );
        StatusMessage( _appName, $"Existing firewall rules for the {site!.Name} blocklist(s) were removed", maintainUI );

        // Add all of the rules that we've just imported
        StatusMessage( _appName, $"Creating new rules for the {site!.Name} blocklist(s) ...", maintainUI );
        AddFirewallRulesFor( ruleName, site.Name, ref candidateRules, ref newRules, ref maintainUI );
        StatusMessage( _appName, $"Firewall rules for the {site.Name} blocklist(s) were added successfully\r\n", maintainUI );
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

    //[GeneratedRegex( "dir.*?<a href=\"(http:)?(?<dir>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexDirectory( );

    //[GeneratedRegex( "[0-9] <a href=\"(http:)?(?<file>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexFile( );
}
