using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BlocklistManager.Models;

using Microsoft.Extensions.Configuration;

using NetTools;

using OSVersionExtension;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.FirewallRules;

using static BlocklistManager.Classes.IAddressExtensions;

namespace BlocklistManager.Classes;

internal static class Maintain
{
    internal static readonly OSVersionExtension.OperatingSystem OsVersion = GetOSVersion( );
    internal static readonly int MaximumFirewallBatchSize = GetFirewallBatchSize;
    private const char BatchDelimiter = ';';
    //private const int MaxIpv4PartValue = 255;
    private static readonly string AppName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;
    private static Device? _device;
    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
    private static readonly string? MacAddress = GetMACAddress( ); // "00:00:00:00:00:00";
    private static readonly CultureInfo Culture = CultureInfo.CurrentCulture;
    private static readonly Regex Ipv4Regex = new Regex( "(([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])" );
    //    private static readonly Regex Ipv6Regex = new Regex( "((([0-9a-fA-F]){1,4})\\:){7}([0-9a-fA-F]){1,4}" );
    private static readonly Regex Ipv6Regex = new Regex( "(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))" );

    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
    [UnconditionalSuppressMessage( "AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>" )]
    //internal static List<FileType> FileTypes => new BlocklistData( ).ListFileTypes( );

    internal static FirewallProfiles AllProfiles = FirewallProfiles.Public | FirewallProfiles.Domain | FirewallProfiles.Private;

    internal static OSVersionExtension.OperatingSystem GetOSVersion( ) => OSVersion.GetOperatingSystem( );

    internal static string LogFileFullname { get; set; } = @$"C:\Program Files\BlocklistManager\Log\BlocklistManager.log";

    internal static int GetFirewallBatchSize => ( OsVersion == OSVersionExtension.OperatingSystem.Windows11 || OsVersion == OSVersionExtension.OperatingSystem.WindowsServer2022 ) ? 10000 : 1000;

    internal static string ApplicationVersion
    {
        get
        {
            try
            {
                return Assembly.GetExecutingAssembly( ).GetName( ).Version!.ToString( );
            }
            catch
            {
                try
                {
                    return AppSettings.Sections.First( f => f.Key == "ApplicationVersion" ).Value ?? "Unknown";
                }
                catch ( Exception ex )
                {
                    StatusMessage( AppName, StringUtilities.ExceptionMessage( "", ex ) );
                    return "Unknown";
                }
            }
        }
    }

    internal static Device? ConnectedDevice
    {
        [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
        [UnconditionalSuppressMessage( "AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>" )]
        get
        {
            if ( _device == null )
            {
                BlocklistData data = new( );
                if ( MacAddress is not null )
                    _device = data.GetDevice( MacAddress! );
            }

            return _device!;
        }
    }

    //internal enum IPAddressType
    //{
    //    IPv4,
    //    IPv6,
    //    Invalid
    //}

    internal sealed record Adapter( string NetworkType, string MACAddress );

    /// <summary>
    /// Get the client computer's MAC address for use in identifying the _device
    /// NOTE: This currently only considers Ethernet and 802.11 WiFi adapters with IP addresses and an active gateway, add other types as required
    /// </summary>
    /// <returns>The MAC addressRange of the first active Ethernet or WiFi adapter found</returns>
    [UnconditionalSuppressMessage( "AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>" )]
    [RequiresUnreferencedCode( "Calls BlocklistManager.Classes.BlocklistData.GetDevice(String)" )]
    public static string? GetMACAddress( )
    {
        string firstMACAddressFound = NetworkInterface.GetAllNetworkInterfaces( )
                                    .Where( w => w.GetIPProperties( ).GatewayAddresses.Count > 0 )
                                    .Select( s => MACAddress( s.GetPhysicalAddress( ) ) )
                                    .First( );

        if ( firstMACAddressFound is null )
            return "Unable to resolve the network controller's MAC address. Please ensure that at least one is active.";

        List<Adapter> macAddresses = [];
        try
        {
            firstMACAddressFound = NetworkInterface.GetAllNetworkInterfaces( )
                  .Where( w => w.OperationalStatus == OperationalStatus.Up )
                  .Where( w => w.NetworkInterfaceType == NetworkInterfaceType.Ethernet || w.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 )
                  .Where( w => w.GetPhysicalAddress( ).ToString( ).Length >= 12 )
                  .Where( w => w.GetIPProperties( ).GatewayAddresses.Count > 0 )
                  .Where( w => ( w.GetIPProperties( ) != null && ( w.GetIPProperties( ).GetIPv4Properties( ) != null ) ) || w.GetIPProperties( ).GetIPv6Properties( ) != null )
                  .Select( s => new Adapter
                      (
                          s.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : "WiFi",
                          MACAddress( s.GetPhysicalAddress( ) )
                      ) )
                  .First( ).MACAddress;

            if ( firstMACAddressFound is null )
                return "Unable to resolve the network controller's MAC address. Please ensure that at least one is active.";

            // NOTE that GetDevice will add the device if not found, so this should not return NULL
            _device = new BlocklistData( ).GetDevice( firstMACAddressFound );
            if ( _device is null )
            {
                MessageBox.Show( "This device is not registered" );
                return null;
            }
        }
        catch ( Exception ex )
        {
            string message = StringUtilities.ExceptionMessage( "GetMACAddress Exception", ex );
            StatusMessage( AppName, message );
        }

        return firstMACAddressFound;
    }

    /// <summary>
    /// Formats the MAC addressRange as a string, "00:00:00:00:00:00" if no valid physical addressRange is found
    /// </summary>
    /// <param name="physicalAddress"></param>
    /// <returns></returns>
    private static string MACAddress( PhysicalAddress physicalAddress )
    {
        /* MAC Address for tests: 2C:3B:70:0C:DA:F5 */
        /*
         * 1 (SBSNB2) : 2C:3B:70:0C:DA:F5
         * 2 (SBSNB1) : 24:0A:64:32:7F:2B
         */
        string s = physicalAddress.ToString( );
        if ( s.Length >= 12 )
            return string.Format( new CultureInfo( "en-US" ), "{0}:{1}:{2}:{3}:{4}:{5}", s[ ..2 ], s[ 2..4 ], s[ 4..6 ], s[ 6..8 ], s[ 8..10 ], s[ 10..12 ] );
        else
            return "00:00:00:00:00:00";
    }

    [UnconditionalSuppressMessage( "AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>" )]
    [RequiresUnreferencedCode( "Calls BlocklistManager.Classes.BlocklistData.ListDownloadSites(Int32, RemoteSite, Boolean)" )]
    internal static List<RemoteSite> ListDownloadSites( RemoteSite? remoteSite, bool showAll = false )
    {
        try
        {
            using BlocklistData data = new BlocklistData( );
            return data.ListDownloadSites( ConnectedDevice!.ID, remoteSite, showAll );
        }
        catch ( Exception ex )
        {
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "ListDownloadSites", ex ) );
            return new BlocklistData( ).ListDownloadSites( ConnectedDevice!.ID, remoteSite, showAll );
        }
    }

    [UnconditionalSuppressMessage( "AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>" )]
    [RequiresUnreferencedCode( "Calls BlocklistManager.Classes.BlocklistData.SetLastDownloaded(Int32, Int32)" )]
    internal static DateTime UpdateLastDownloaded( RemoteSite site )
    {
        if ( ConnectedDevice is null )
            Console.WriteLine( "UpdateLastDownloaded: ConnectedDevice is null" );

        try
        {
            using BlocklistData data = new BlocklistData( );
            data.Dispose( );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"UpdateLastDownloaded: {ex.Message}" );
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "UpdateLastDownloaded", ex ) );
        }

        int deviceID = ConnectedDevice!.ID;
        return new BlocklistData( ).SetLastDownloaded( ConnectedDevice!.ID, site.ID );
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
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "DeleteExistingFirewallRulesFor", ex ) );
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
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "AddInboundAndOutboundRules", ex ) );
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
        FirewallWASRule newRule = new( ruleName, FirewallAction.Block, direction, AllProfiles )
        {
            Description = ruleName,
            Grouping = AppName,
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
            return string.Join( BatchDelimiter, addresses.Select( s => s.ToString( ) ).ToArray( ) );
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
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "AddFirewallRulesFor", e ), maintainUI );
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

    #region Not used: InternetAddressType( string address )
    /// <summary>
    /// Determine whether 'adddress' is IP v4 or IP v6
    /// </summary>
    /// <param name="address">The addressRange to analyze</param>
    /// <returns></returns>
    //internal static IPAddressType InternetAddressType( string address )
    //{
    //    IPAddressType addressType = IPAddressType.Invalid;

    //    //if ( IPAddress.TryParse( address, out IPAddress? ipAddress ) ) // Not a good validator, converts "207.63.218" to "207.63.0.218"
    //    if ( ValidateIPAddress( address, out IPAddressType addresstype ) )
    //    {
    //        IPAddress ipAddress = IPAddress.Parse( address );
    //        if ( ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork )
    //        {
    //            addressType = IPAddressType.IPv4;
    //        }
    //        else if ( ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 )
    //        {
    //            addressType = IPAddressType.IPv6;
    //        }
    //    }
    //    else
    //        StatusMessage( AppName, $"Invalid IP address {address} was ignored", null );

    //    return addressType;
    //}
    #endregion

    private sealed record StringAndType( string AddressString, IAddress IpAddress, CandidateEntry Owner );

    /// <summary>
    /// Download blocklists from all sites or from each chosen site ( limited to 1 for the interactive app )
    /// </summary>
    /// <param name="maintainUIForm">The UI form if applicable</param>
    /// <param name="sitesToProcess">A list of download sites to download from ( limited to 1 for the interactive app )</param>
    /// <param name="logPath">The log file path (optional)</param>
    /// <returns>A list of the downloaded addressRange entries</returns>
    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
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
                    bool addLineFeed = sitesToProcess[ sitesToProcess.Count - 1 ].Name == site.Name;
                    string newLine = addLineFeed ? Environment.NewLine : string.Empty;
                    StatusMessage( AppName, $"Read {site.Name} blocklist(s){newLineText} containing {newData.Count} entries{newLine}", maintainUIForm );

                    if ( maintainUIForm is not null )
                    {
                        maintainUIForm.UpdateProgress( counter, sitesToProcess.Count );
                        maintainUIForm.StatusMessage.Text = $"Updating {site!.Name} downloaded date and time ...";
                    }

                    DateTime lastDownloaded = UpdateLastDownloaded( site );
                    UpdateSiteListDateDownloaded( maintainUIForm, site, lastDownloaded );
                }

                if ( maintainUIForm is not null )
                    maintainUIForm.UpdateProgress( counter, sitesToProcess.Count );
            }
            catch ( Exception ex )
            {
                StatusMessage( AppName, $"Downloading data from {site.Name} failed: {StringUtilities.ExceptionMessage( AppName, ex )}", maintainUIForm );
            }
        }

        return [ .. data.Select( s => new CandidateEntry( s.Name, s.FileName, s.IPAddress ?? "", s.Subnet, s.IPAddressRange, s.IPAddressSet/*, s.Ports, s.Protocol*/ ) )
                   .OrderBy( o => o.Sort0 )
                   .ThenBy( o => o.Sort1 )
                   .ThenBy( o => o.Sort2 )
                   .ThenBy( o => o.Sort3 ) ];
    }

    private static void UpdateSiteListDateDownloaded( MaintainUI? maintainUIForm, RemoteSite site, DateTime lastDownloaded )
    {
        if ( maintainUIForm is not null )
        {
            int siteRowIndex = maintainUIForm.RemoteSites.Rows.Cast<DataGridViewRow>( )
                                                              .First( f => f.Cells[ "Name" ].Value!.ToString( ) == site.Name )
                                                              .Index;
            maintainUIForm.RemoteSites[ "LastDownloaded", siteRowIndex ].Value = lastDownloaded.ToString( "G", Culture );
            maintainUIForm.RemoteSites.Refresh( );
        }
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

            while ( work.Count > 0 && work.Count > MaximumFirewallBatchSize )
            {
                List<CandidateEntry> candidates = work.Take( MaximumFirewallBatchSize )
                                                      .ToList( );

                results.Add( BuildIPAddressSet( candidates/*, sequence */, MaximumFirewallBatchSize ) );
                // Remove the entries which have been processed
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
        string message = rationalisedCount switch
        {
            1 => $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entry",
            > 1 => $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries",
            _ => rationalisedCount > 1 ? $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries" : $"Consolidated {unRationalisedCount} addresses into {rationalisedCount} entries: INVESTIGATE!"
        };

        StatusMessage( AppName, message );
        return results;
    }

    /// <summary>
    /// Reduce the number of entries for the firewall by bundling individual addresses into sets of the maximum size. Currently apparently 1000.
    /// </summary>
    /// <param name="data">The data to be rationlised </param>
    /// <param name="sites">The list of sites being processed</param>
    internal static List<CandidateEntry> ConvertIPAddressRangesToIPAddressRangeSets( List<CandidateEntry> data, List<RemoteSite> sites )
    {
        // Preserve the entries  that aren't being modified in the section
        List<CandidateEntry> results = data.Where( w => w.IPAddressRange is null )
                                           .ToList( );
        int unRationalisedCount = data.Count( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null ),
            rationalisedCount = 0;

        // This should be run per download site
        foreach ( RemoteSite site in sites )
        {
            var /*List<CandidateEntry> */ work = data.Where( w => w.Name == site.Name ) // Validate
                                            .Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null );
            //.ToList( );

            while ( work.Any( ) && work.Count( ) > MaximumFirewallBatchSize )
            {
                List<CandidateEntry> candidates = work.Take( MaximumFirewallBatchSize )
                                                      .ToList( );

                results.Add( BuildIPAddressRangeSet( candidates/*, sequence */, MaximumFirewallBatchSize ) );
                rationalisedCount++;
                work = work.Except( candidates ).ToList( );
            }

            // Finally, add an entry for a set containing the leftover entries
            if ( work.Any( ) )
            {
                List<CandidateEntry> candidates = work.ToList( );
                results.Add( BuildIPAddressRangeSet( candidates, /*sequence, */candidates.Count ) );
                rationalisedCount++;
            }
        }

        if ( rationalisedCount == 1 )
            StatusMessage( AppName, $"Consolidated {unRationalisedCount} address ranges into {rationalisedCount} entry{Environment.NewLine}" );
        else if ( rationalisedCount > 1 )
            StatusMessage( AppName, $"Consolidated {unRationalisedCount} address ranges into {rationalisedCount} entries{Environment.NewLine}" );
        //else if ( rationalisedCount > 0 )
        //    StatusMessage( AppName, $"Consolidated {unRationalisedCount} address ranges into {rationalisedCount} entries: INVESTIGATE!" );

        return results;
    }

    /// <summary>
    /// Removes entries belonging to private IP addressRange ranges (e.g. the 192.168 range)<CandidateEntry>
    /// </summary>
    /// <param name="candidates">The list to process</param>
    /// <param name="numberRemoved">Report back on how many were deleted</param>
    internal static void RemovePrivateAddressesRanges( ref List<CandidateEntry> candidates, out int numberRemoved )
    {
        // TODO: Store the private addressRange ranges in a configuration file/the database
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
                                .Union( candidates.Where( w => string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is not null ) )
                                .ToList( );

        numberRemoved -= candidates.Count;
    }

    // internal record addressIPValid( string? IPAddress, string? IPAddressRangeStart, string? IPAddressRangeEnd, IPAddressType AddressType ) { internal bool Valid { get; set; } };

    /// <summary>
    /// Removes entries containing invalid IP addresses<CandidateEntry>
    /// </summary>
    /// <param name="data">The list to process</param>
    /// <param name="numberRemoved">Report back on how many were deleted</param>
    internal static void RemoveInvalidIPAddresses( ref List<CandidateEntry> data, out int numberRemoved )
    {
        ValidateIPAddressesAndRanges( data ); // Already done so this is just in case
        string[] invalidIPs = data.Where( w => !w.Validated )
                                  .Select( s => string.IsNullOrEmpty( s.IPAddress ) ? s.IPAddressRange!.ToString( ) : s.IPAddress )
                                  .ToArray( );

        numberRemoved = invalidIPs.Length; //  data.Count( c => !c.Validated || w.IPAddress == "0.0.0.0" );
        data = data.Where( w => w.Validated && w.IPAddress != "0.0.0.0" )
                   .ToList( );
        if ( invalidIPs.Length == 0 )
            invalidIPs = [ "NONE FOUND." ];
        else
            invalidIPs = [ $"{Environment.NewLine}\t{string.Join( Environment.NewLine + "\t", invalidIPs )}" ];

        StatusMessage( AppName, $"Removed invalid IP addresses or ranges: {invalidIPs[ 0 ]}", null );
    }

    /// <summary>
    /// Perform basic IP address validation (v4 and v6)
    /// </summary>
    /// <param name="ipAddress">The IP address to validate</param>
    /// <param name="addressType">Makes the determined IP version available to the caller</param>
    /// <returns>True if valid</returns>
    internal static bool ValidateIPAddress( string ipAddress, out IPAddressType addressType )
    {
        bool valid = false;
        addressType = IPAddressType.Invalid;

        if ( IPAddress.TryParse( ipAddress, out IPAddress? resolvedAddress ) )
        {
            Match match = Regex.Match( ipAddress, Ipv4Regex.ToString( ) );
            valid = match.Success;
            addressType = valid ? IPAddressType.IPv4 : IPAddressType.Invalid;

            if ( !valid )
            {
                match = Regex.Match( ipAddress, Ipv6Regex.ToString( ) );
                valid = match.Success;
                addressType = match.Success ? IPAddressType.IPv6 : IPAddressType.Invalid;
            }
        }

        return valid;
    }

    #region Older version
    //private static void ValidateIPAddresses( List<CandidateEntry> data )
    //{
    //    //Regex Ipv4Regex = new Regex( "(([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])" );
    //    //Regex Ipv6Regex = new Regex( "((([0-9a-fA-F]){1,4})\\:){7}([0-9a-fA-F]){1,4}" );

    //    foreach ( CandidateEntry item in data.Where( w => !w.Validated ) )
    //    {
    //        string address = string.IsNullOrEmpty( item.IPAddress )
    //                           ? item.IPAddressRange!.StartAddress!.ToString( )
    //                           : item.IPAddress;
    //        item.Validated = ValidateIPAddress( address, out IPAddressType addressType );
    //        item.AddressType = addressType;
    //        if ( item.Validated && item.IPAddressRange is not null )
    //        {
    //            item.Validated = ValidateIPAddress( item.IPAddressRange!.EndAddress.ToString( ), out addressType );
    //        }

    //        if ( item.AddressType != addressType && addressType != IPAddressType.Invalid )
    //            item.AddressType = addressType;

    //        //if ( IPAddress.TryParse( address, out IPAddress? ipAddress ) )
    //        //{
    //        //    if ( item.AddressType == IPAddressType.IPv4 )
    //        //    {
    //        //        Match match = Regex.Match( address, Ipv4Regex.ToString( ) );
    //        //        if ( match.Success && item.IPAddressRange is not null )
    //        //        {
    //        //            match = Regex.Match( item.IPAddressRange.EndAddress.ToString( ), Ipv4Regex.ToString( ) );
    //        //        }

    //        //        item.Validated = match.Success;
    //        //    }

    //        //    if ( item.AddressType == IPAddressType.IPv6 )
    //        //    {
    //        //        Match match = Regex.Match( address, Ipv6Regex.ToString( ) );
    //        //        if ( match.Success && item.IPAddressRange is not null )
    //        //        {
    //        //            match = Regex.Match( item.IPAddressRange!.EndAddress.ToString( ), Ipv6Regex.ToString( ) );
    //        //        }

    //        //        item.Validated = match.Success;
    //        //    }
    //        //}
    //    }
    //}
    #endregion Older version

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
                StatusMessage( AppName, ex, null );
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

    #region Obsolete - we no longer attempt to process ports without protocols
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
    #endregion Obsolete - we no longer attempt to process ports without protocols

    [UnconditionalSuppressMessage( "Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>" )]
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
                        data = ReadJson( site, url, fileName );
                        break;
                    }
                case 3: // Xml
                    {
                        data = ReadXml( site, fileUrl, fileName );
                        break;
                    }
                case 4: // Tab delimited
                case 9: // Comma delimited
                    {
                        data = ReadDelimited( site, url, fileName );
                        break;
                    }
                #region Not in use yet
                case 5: // Zip archive containing Json
                case 6: // Zip archive containing text
                case 7: // Zip archive containing delimited data
                    {
                        // Not in use; not finished
                        // Extract the data file from the ZIP archive
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
                #endregion Not in use yet
                default:
                    {
                        data = ReadText( site, url, fileName );
                        break;
                    }
            }
        }

        return data;
    }

    /// <summary>
    /// Read text data from a Url
    /// </summary>
    /// <param name="site">The download site</param>
    /// <param name="url">The download Url</param>
    /// <param name="fileName">Here only to fit the ITranslator interface</param>
    /// <returns>The processed data for this site</returns>
    private static List<CandidateEntry> ReadText( RemoteSite site, string url, string fileName )
    {
        string fileExtension = string.Empty;
        List<CandidateEntry> processedData = [];
        try
        {
            string textData = Downloader.ReadData( site, out fileExtension!, url );
            if ( textData.Length > 0 )
            {
                using DataTranslatorText translator = new( );
                processedData = translator.TranslateFileData( site, textData, fileName );
            }
        }
        catch ( Exception ex )
        {
            StatusMessage( AppName, StringUtilities.ExceptionMessage( "DownloadSiteData", ex ) );
        }

        return processedData;
    }

    /// <param name="site">The download site</param>
    /// <param name="url">The download Url</param>
    /// <param name="fileName">Here only to fit the ITranslator interface</param>
    /// <returns>The processed data for this site</returns>
    private static List<CandidateEntry> ReadDelimited( RemoteSite site, string url, string fileName )
    {
        string downloaded = Downloader.ReadData( site, out string? fileExtension, url );
        List<CandidateEntry> processedData = [];

        if ( !string.IsNullOrEmpty( downloaded ) )
        {
            using DataTranslatorDelimited translator = new( );
            var rawData = downloaded.Split( Environment.NewLine ); // .Where( w => w.Contains( "207.63.218" ) );
            processedData = translator.TranslateFileData( site, downloaded, fileName );
        }

        return processedData;
    }

    /// <param name="site">The download site</param>
    /// <param name="fileUrl">The download Url</param>
    /// <param name="fileName">Here only to fit the ITranslator interface</param>
    /// <returns>The processed data for this site</returns>
    [RequiresUnreferencedCode( "Calls BlocklistManager.Classes.DataTranslatorXml.TranslateDataStream(RemoteSite, Stream, String)" )]
    private static List<CandidateEntry> ReadXml( RemoteSite site, string fileUrl, string fileName )
    {
        Stream? downloaded = Downloader.ReadHtmlStreamFromUrl( site, fileUrl );
        List<CandidateEntry> processedData = [];

        if ( downloaded is not null )
        {
            using DataTranslatorXml translator = new( );
            processedData = translator.TranslateDataStream( site, downloaded, fileName );
        }

        return processedData;
    }

    /// <param name="site">The download site</param>
    /// <param name="url">The download Url</param>
    /// <param name="fileName">Here only to fit the ITranslator interface</param>
    /// <returns>The processed data for this site</returns>
    private static List<CandidateEntry> ReadJson( RemoteSite site, string url, string fileName )
    {
        string fileExtension = string.Empty;
        List<CandidateEntry> processedData = [];
        string textData = Downloader.ReadData( site, out fileExtension!, url );
        if ( textData.Length > 2 )
        {
            using DataTranslatorJson translator = new( );
            processedData = translator.TranslateFileData( site, textData, fileName );
        }

        return processedData;
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

    /// <summary>
    /// Validates DNS resolution of a site URL
    /// </summary>
    /// <param name="url">The site URL to validate</param>
    /// <returns>True if the Url is valid</returns>
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
        catch ( Exception ex )
        {
            StatusMessage( AppName, $"DNS resolution of {domain} failed: {ex.Message}" );
        }

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

        if ( batchSize > MaximumFirewallBatchSize )
        {
            StatusMessage( AppName, $"The maximum number of addresses the Windows Firewall currently accepts per entry is {MaximumFirewallBatchSize}.\r\nOnly the first {MaximumFirewallBatchSize} are being grouped." );
            batchSize = MaximumFirewallBatchSize;
        }

        if ( entriesForBatch.Any( c => c.IPAddressRange is not null || c.IPAddress is null ) )
        {
            StatusMessage( AppName, $"BuildIPAddressSet: Invalid entries were provided for processing and will be excluded." );
        }

        List<StringAndType> addressSet = entriesForBatch.Where( w => w.IPAddress is not null && w.IPAddressRange is null )
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

        if ( batchSize > MaximumFirewallBatchSize )
        {
            StatusMessage( AppName, $"The maximum number of addressRange ranges the Windows Firewall currently accepts per entry is {MaximumFirewallBatchSize}.\r\nOnly the first {MaximumFirewallBatchSize} are being grouped." );
            batchSize = MaximumFirewallBatchSize;
        }

        if ( entriesForBatch.Any( c => c.IPAddressRange is null || c.IPAddress is not null ) )
        {
            StatusMessage( AppName, $"BuildIPAddressRangeSet: Invalid entries were provided for processing and will be excluded." );
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
    internal static List<CandidateEntry> ProcessDownloads( List<RemoteSite> sites, MaintainUI? maintainUI, bool createFirewallRules, out int numberOfRules, out int ipAddressCount, out int allAddressCount )
    {
        // createFirewallRules should only be true when the program is running from command line or scheduler
        StatusMessage( AppName!, "Blocklist downloads started... ", maintainUI );
        List<CandidateEntry> candidateRules = DownloadBlocklists( maintainUI, sites )!;
        allAddressCount = candidateRules.Count;

        //candidateRules = candidateRules.Where( w => w.IPAddress == "171.41.151.160" ).ToList( );
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
            // Added 3/3/2025 to not waste time on sites with no entries left after cleanups
            sites = ( from s in sites join c in candidateRules on s.Name equals c.Name select s ).Distinct( ).ToList( );
            newRules = ReplaceFirewallRules( sites, maintainUI, candidateRules );
            numberOfRules = newRules.Count;
            if ( maintainUI is not null )
            {
                string siteName = sites.Count == 1 ? sites[ 0 ].Name : "all active sites";
                maintainUI.FirewallRulesLabel.Text = $"New firewall entries for the {siteName} blocklist";
                maintainUI.FirewallRulesData.DataSource = newRules;
                maintainUI.FirewallRulesData.ForeColor = Color.Green;
                maintainUI.FirewallRulesData.Refresh( );
            }
        }

        return candidateRules!;
    }

    internal static List<CandidateEntry> CleanupDownloadedIPAddressData( List<RemoteSite> sites, MaintainUI? maintainUI, bool createFirewallRules, List<CandidateEntry> candidateRules, out int ipAddressCount )
    {
        ipAddressCount = candidateRules.Count;
        int cleanupSteps = 9, cleanupStep = 0;

        // Reset the progress bar
        if ( maintainUI is not null )
        {
            maintainUI.UpdateProgress( cleanupStep, cleanupSteps );
        }

        if ( candidateRules.Count > 0 )
        {
            StatusMessage( AppName, $"Rationalising {candidateRules.Count} blocklist entries ...", maintainUI );

            // First remove duplicates so that we don't waste time on them
            StatusMessage( AppName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out int numberRemoved );
            ipAddressCount = candidateRules.Count;
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, "Determining IP address types ...", maintainUI );
            ValidateIPAddressesAndRanges( candidateRules );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            // Are there any other address types to validate?
            var peek = candidateRules.Where( w => w.IPAddress is null && w.IPAddressRange is null ).ToList( );
            if ( peek.Count > 0 )
                Debug.Print( "" );

            // Remove invalid addresses so that we don't waste time and effort on them
            RemoveInvalidIPAddresses( ref candidateRules, out numberRemoved );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, "Removing private addressRange ranges ...", maintainUI );
            RemovePrivateAddressesRanges( ref candidateRules, out numberRemoved );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out numberRemoved );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, "Convert any IP address subnets to address ranges ...", maintainUI );
            SubnetsToRanges( ref candidateRules, out int numberConverted );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            // Rerun deduplication in case subnet conversion introduced duplicate ranges
            StatusMessage( AppName, "Removing duplicates ...", maintainUI );
            RemoveDuplicates( ref candidateRules, out numberRemoved );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, $"Consolidating {candidateRules.Count( c => c.IPAddressRange is null )} addresses into sets of {MaximumFirewallBatchSize} ...", maintainUI );
            candidateRules = ConvertIPAddressesToIPAddressSets( candidateRules, sites );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );

            StatusMessage( AppName, $"Consolidating {candidateRules.Count( c => c.IPAddressRange is not null )} address ranges into sets of {MaximumFirewallBatchSize} ...", maintainUI );
            candidateRules = ConvertIPAddressRangesToIPAddressRangeSets( candidateRules, sites );
            cleanupStep++;
            if ( maintainUI is not null )
                maintainUI.UpdateProgress( cleanupStep, cleanupSteps );
        }

        return candidateRules!;
    }

    /// <summary>
    /// Validate all IP addresses and address ranges in a list of CandidateEntry
    /// </summary>
    /// <param name="candidateRules">The list of CandidateEntry for validation</param>
    private static void ValidateIPAddressesAndRanges( List<CandidateEntry> candidateRules )
    {
        foreach ( CandidateEntry entry in candidateRules.Where( w => w.IPAddress is not null && !w.Validated ) )
        {
            entry.Validated = ValidateIPAddress( entry.IPAddress!, out IPAddressType addressType );
            entry.AddressType = addressType;
        }

        foreach ( CandidateEntry entry in candidateRules.Where( w => w.IPAddressRange is not null && !w.Validated ) )
        {
            entry.Validated = ValidateIPAddress( entry.IPAddressRange!.StartAddress.ToString( ), out IPAddressType addressType );
            if ( entry.Validated )
                entry.Validated = ValidateIPAddress( entry.IPAddressRange!.EndAddress.ToString( ), out addressType );
            entry.AddressType = addressType;
        }
    }

    /// <summary>
    /// Deletes existing firewall rules for each site in sites and replaces them with the latest rules from that site's blocklist(s)
    /// </summary>
    /// <param name="sites">The list of sites for processing</param>
    /// <param name="maintainUI">The user interface by reference to enable status messages and the progress bar to be updated</param>
    /// <param name="candidateRules">The list of data for new rules to be added</param>
    /// <returns>The list of new firewall rules</returns>
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
                StatusMessage( AppName, ex, maintainUI );
            }

            if ( maintainUI is not null )
                maintainUI.UpdateProgress( counter, sites.Count );
        }

        StatusMessage( AppName, "Firewall rule creation completed successfully", maintainUI );
        return rules;
    }

    /// <summary>
    /// Deletes existing firewall rules for a specific site and replaces them with the latest rules from that site's blocklist(s)
    /// </summary>
    /// <param name="site">The site for processing</param>
    /// <param name="candidateRules">The list of data for new rules to be added</param>
    /// <param name="newRules">The list of new rules (by reference) created</param>
    /// <param name="maintainUI">The user interface by reference to enable status messages and the progress bar to be updated</param>
    internal static void ReplaceSiteRules( RemoteSite site, List<CandidateEntry> candidateRules, ref List<IFirewallRule> newRules, MaintainUI? maintainUI )
    {
        string ruleName = $"{site.Name}_Blocklist";
        // Existing rules
        int ruleCount = FirewallManager.Instance.Rules.Where( r => r.Name == ruleName ).Count( );

        // Delete all rules added by this program
        StatusMessage( AppName, $"Removing {ruleCount} existing firewall rules for the {site!.Name} blocklist(s) ...", maintainUI );
        DeleteExistingFirewallRulesFor( ruleName );
        StatusMessage( AppName, $"{ruleCount} existing firewall rules for the {site!.Name} blocklist(s) were removed", maintainUI );

        ruleCount = candidateRules.Where( w => w.Name == site.Name ).Count( ) * 2; /* Inbound AND Outbound rules will be created */

        // Add all of the rules that we've just imported
        StatusMessage( AppName, $"Creating {ruleCount} new firewall rules for the {site!.Name} blocklist(s) ...", maintainUI );
        AddFirewallRulesFor( ruleName, site.Name, ref candidateRules, ref newRules, ref maintainUI );
        int siteCount = newRules.Count( c => c.Name == site.Name + "_Blocklist" );
        StatusMessage( AppName, $"{siteCount} new firewall rules for the {site.Name} blocklist(s) were created successfully\r\n", maintainUI );
    }

    /// <summary>
    /// Logs a message to the log file when running in console mode and to the user interface when running in GUI mode
    /// </summary>
    /// <param name="caller">The name of the caller module or method</param>
    /// <param name="message">The message to log</param>
    /// <param name="maintainUI">The user interface (when applicable) by reference to enable status messages to be updated</param>
    internal static void StatusMessage( string caller, string message, MaintainUI? maintainUI = null )
    {
        //if ( string.IsNullOrEmpty( Logger.LogPath ) )
        try
        {
            if ( !LogFileFullname.Equals( Logger.LogPath, StringComparison.OrdinalIgnoreCase ) )
                Logger.LogPath = LogFileFullname;
        }
        catch ( Exception ex )
        {
            Console.WriteLine( "StatusMessage 1a failure: " + ex.Message + Environment.NewLine + ex.InnerException != null ? ex.InnerException!.Message : "" );
            try
            {
                string msg = StringUtilities.ExceptionMessage( "StatusMessage 1b", ex );
                Console.WriteLine( msg );
            }
            catch ( Exception X2 )
            {
                Console.WriteLine( "StatusMessage 1b failure: " + X2.Message + Environment.NewLine + X2.InnerException != null ? X2.InnerException!.Message : "" );
            }
        }

        Logger.Log( caller, message );

        if ( maintainUI is not null )
        {
            maintainUI.StatusMessage.Text = message;
            maintainUI.Refresh( );
        }
    }

    /// <summary>
    /// Logs an exception to the log file when running in console mode and to the user interface when running in GUI mode
    /// </summary>
    /// <param name="caller">The name of the caller module or method</param>
    /// <param name="ex">The exception to log</param>
    /// <param name="maintainUI">The user interface (when applicable) by reference to enable status messages to be updated</param>
    internal static void StatusMessage( string caller, Exception ex, MaintainUI? maintainUI )
    {
        try
        {
            string message = StringUtilities.ExceptionMessage( caller, ex );
            Console.WriteLine( "StatusMessage 1: " + message );
        }
        catch ( Exception ex1 )
        {
            Console.WriteLine( $"In StatusMessage 2. From {caller}: " + ex.Message + Environment.NewLine + ex.InnerException != null ? ex.InnerException!.Message : "" );
            Console.WriteLine( $"In StatusMessage 3 exception: " + ex1.Message + Environment.NewLine + ex1.InnerException != null ? ex1.InnerException!.Message : "" );
        }

        try
        {
            string message = StringUtilities.ExceptionMessage( string.Empty, ex );
            StatusMessage( caller, message, maintainUI );
        }
        catch ( Exception X1 )
        {
            Console.WriteLine( "StatusMessage 4 failure" + X1.Message + Environment.NewLine + X1.InnerException != null ? X1.InnerException!.Message : "" );
        }
    }

    #region Not used: retuns the current culture's language and country codes, e.g. en-US, en-GB, fr-FR
    internal static string DeviceLocation( )
    {
        string cultureCountryISOCode2character = string.Empty; //, countryISOCode2character = string.Empty;
        string languageISOCode2character = string.Empty;
        var currentCulture = CultureInfo.CurrentCulture;
        languageISOCode2character = currentCulture.TwoLetterISOLanguageName;
        cultureCountryISOCode2character = currentCulture.Name.Split( '-' )[ 1 ];
        return cultureCountryISOCode2character;
    }
    #endregion Not used: retuns the current culture's language and country codes, e.g. en-US, en-GB, fr-FR
}
