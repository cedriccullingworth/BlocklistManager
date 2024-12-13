using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

using BlocklistManager.Context;
using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using SBS.Utilities;

using SharpCompress.Common;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper.FirewallRules;

namespace BlocklistManager.Classes;
internal static class Maintain
{
    internal const int MAX_FIREWALL_BATCH_SIZE = 1000;
    private const char BATCH_DELIMITER = ';';
    private static readonly string _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;

    internal static IList<FileType> FILETYPES => ( new BlocklistDbContext( ) ).ListFileTypes( );

    internal static FirewallProfiles _AllProfiles = FirewallProfiles.Public | FirewallProfiles.Domain | FirewallProfiles.Private;


    internal enum IPAddressType
    {
        IPv4,
        IPv6,
        Invalid
    }

    internal static IList<RemoteSite> ListDownloadSites( RemoteSite? remoteSite, bool showAll = false ) =>
         new BlocklistDbContext( new DbContextOptions<BlocklistDbContext>( ) { } )
        .ListRemoteSites( remoteSite, showAll );

    public static IList<FileType> ListFileTypes( ) =>
         new BlocklistDbContext( new DbContextOptions<BlocklistDbContext>( ) { } )
        .ListFileTypes( );

    public static IList<FirewallRule> ProcessSite( RemoteSite site, MaintainUI? maintainUIForm = null )
    {
        List<CandidateEntry> candidateRules = [];
        if ( maintainUIForm is not null )
        {
            maintainUIForm.StatusMessage.Text = $"Downloading blocklist(s) from {site.Name} ...";
            maintainUIForm.Refresh( );
        }

        candidateRules.AddRange( DownloadBlocklists( maintainUIForm, site ) );
        ConvertIPAddressesToIPAddressSets( ref candidateRules, [ site ] );
        string ruleName = $"@(imported) {site!.Name}_Blocklist";
        if ( maintainUIForm is not null )
        {
            maintainUIForm.SetFirewallRuleColumnWidths( );
            maintainUIForm.FirewallEntryName.Text = ruleName;
            maintainUIForm.StatusMessage.Text = $"Removing existing rules for the {site!.Name} blocklist(s) ...";
        }

        DeleteExistingFirewallRulesFor( ruleName );
        if ( maintainUIForm is not null )
        {
            // We'[ve just deleted them!
            //maintainUIForm.StatusMessage.Text = $"Reading existing firewall rules for {site!.Name} ..."; ;
            //maintainUIForm.FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( site!.Name );
            //maintainUIForm.SetFirewallRuleColumnWidths( );

            // Add all of the rules that we've just imported
            // maintainUIForm.StatusMessage.Text = $"Creating new rules for " + ( _processAll ? "all blocklist download sites" : $"{_ruleName}" ) + " ..."; ;
            maintainUIForm.StatusMessage.Text = $"Creating new rules for the {site!.Name} blocklist(s) ...";
        }

        AddFirewallRulesFor( ruleName, site.Name, ref candidateRules );

        if ( maintainUIForm is not null )
            maintainUIForm.StatusMessage.Text = $"Reading updated firewall rules for {site!.Name} ...";

        //maintainUIForm.FirewallRulesData.DataSource = Maintain.FetchFirewallRulesFor( site!.Name );
        return FetchFirewallRulesFor( site!.Name );
        // Do this in the caller form
        // maintainUIForm.SetFirewallRuleColumnWidths( );
    }

    internal static void EnsureStartupDataExists(/*BlocklistContext store*/)
    {
        try
        {
            using BlocklistDbContext store = new( );
            store.Database.EnsureCreated( );
            if ( store.Database.GetService<IDatabaseCreator>( ) is RelationalDatabaseCreator databaseCreator )
            {
                try
                {
                    databaseCreator.CreateTables( );
                }
                catch
                {
                    //A SqlException will be thrown if tables already exist, so simply ignore it.
                }
            }
            else
            {
                throw new Exception( "Database creation service is null" );
            }

            store.EnsureDataExists( );
        }
        catch ( Exception ex )
        {
            MessageBox.Show( ex.Message ); // TODO: Improve message
        }
    }

    internal static RemoteSite? AddRemoteSite( RemoteSite remoteSite )
    {
        using BlocklistDbContext ctx = new( new DbContextOptions<BlocklistDbContext>( ) );
        RemoteSite? newSite = null;

        if ( remoteSite.ID > 0 || ctx.RemoteSites.Any( c => c.Name == remoteSite.Name ) )
        {
            return UpdateRemoteSite( remoteSite );
        }
        else
        {
            ctx.RemoteSites.Add( remoteSite );
            ctx.SaveChanges( );
            newSite = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );
        }

        return newSite;
    }

    internal static bool DeleteRemoteSite( RemoteSite remoteSite )
    {
        using BlocklistDbContext ctx = new( new DbContextOptions<BlocklistDbContext>( ) );
        bool deleted = false;

        try
        {
            ctx.RemoteSites.Remove( remoteSite );
            ctx.SaveChanges( );
            if ( !ctx.RemoteSites.Any( c => c.Name == remoteSite.Name ) )
            {
                deleted = true;
            }
        }
        catch ( Exception ex )
        {
            MessageBox.Show( ex.Message ); // TODO: Improve the message
        }

        return deleted;
    }

    internal static RemoteSite? UpdateRemoteSite( RemoteSite remoteSite )
    {
        using BlocklistDbContext ctx = new( new DbContextOptions<BlocklistDbContext>( ) );
        RemoteSite? existing = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );

        if ( existing != null )
        {
            existing.SiteUrl = remoteSite.SiteUrl;
            existing.FileUrls = remoteSite.FileUrls;
            existing.FileType = remoteSite.FileType;
            ctx.SaveChanges( );
            existing = ctx.RemoteSites.FirstOrDefault( f => f.Name == remoteSite.Name );
        }
        else
        {
            MessageBox.Show( $"No site matching '{remoteSite.Name}' was found." );
        }

        return existing;
    }

    internal static bool UpdateLastDownloaded( RemoteSite site )
    {
        try
        {
            using BlocklistDbContext ctx = new( );
            ctx.SetDownloadedDateTime( site );

            return true;
        }

        catch
        {
            return false;
        }
    }

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
                        using TextDataCollector collector = new TextDataCollector();
                        Stream? readAttempt = collector.ReadHtmlStreamFromUrl( site, site.FilePaths[ i ] );
                        valid = readAttempt is not null && readAttempt.Length > 0;
                    }
                    catch { }
                }
            }

            break;
        }

        return valid;
    }

    internal static IList<FirewallRule> FetchFirewallRulesFor( string? name = null )
    {
        if ( name is not null && !name.StartsWith( "@(imported) " ) )
            name = $"@(imported) {name}_Blocklist";

        IEnumerable <IFirewallRule> rules = FirewallManager.Instance
                .Rules
                .Where( w => w.Name.StartsWith( "@(imported) " )
                                     && w.Name.EndsWith( "_Blocklist" ) );

        if ( name is not null )
            rules = rules.Where( w => w.Name.Equals( name ) );

        return [ .. rules.Select( s => new FirewallRule( s.Name, s.Action, s.Direction, s.Profiles, s.RemoteAddresses )
        {
            Name = s.Name,
            //Description = s.de
            LocalAddresses = s.LocalAddresses,
            LocalPorts = s.LocalPorts,
            RemoteAddresses = s.RemoteAddresses,
            RemotePorts = s.RemotePorts,
            Action = s.Action,
            IsEnable = s.IsEnable,
            Direction = s.Direction,
            LocalPortType = s.LocalPortType,
            Protocol = s.Protocol,
            Scope = s.Scope,
            ServiceName = s.ServiceName,
            ApplicationName = s.ApplicationName,
        } )
            .OrderBy( o => o.Name )
            .ThenBy( o => o.SortValue[ 0 ] )
            .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 1 ] : 0 )
            .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 2 ] : 0 )
            .ThenBy( t => t.SortValue.Length > 0 ? t.SortValue[ 3 ] : 0 ) ];
    }

    internal static bool DeleteExistingFirewallRulesFor( string? ruleName = null, string logFilePath = "" )
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
            if ( logFilePath != null )
                Logger.Log( _appName, ex );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "DeleteExistingFirewallRulesFor", ex ) );
        }

        return deleted;
    }

    private static List<IFirewallRule> AddInboundAndOutboundRules
        (
            string ruleName,
            //string? ipAddress,
            IAddress[]? ipAddressBatch = null,
            IPRange? ipAddressRange = null,
            FirewallProtocol? protocol = null,
            ushort[]? ports = null, /// NB NB NB These are only set if a protocol was provided
            string logFilePath = ""
        )
    {
        const string description = "BlocklistManager";
        protocol ??= FirewallProtocol.Any;

        ports ??= [];

        List<IFirewallRule> rules = [];
        IAddress[] remoteIPRange = ipAddressRange is not null ? new IPRange[] { ipAddressRange } : [];

        // Delete any existing rules matching on name and IP ipAddress set
        IEnumerable<IFirewallRule> existing = [];
        if ( ipAddressBatch is not null && ipAddressBatch.Length > 0 )
            existing = FirewallManager.Instance
                                      .Rules
                                      .Where( f => f.Name == ruleName && f.RemoteAddresses == remoteIPRange );
        else if ( ipAddressRange is not null && ipAddressRange.StartAddress is not null && ipAddressRange.EndAddress is not null )
            existing = FirewallManager.Instance
                                      .Rules
                                      .Where( f => f.Name == ruleName && f.RemoteAddresses == ipAddressBatch );

        if ( existing is not null && existing.Any( ) )
        {
            foreach ( var item in existing )
                FirewallManager.Instance
                               .Rules
                               .Remove( item );
        }

        SingleIP[] local = [new( IPAddress.Any )];

        // Create and add the new rules
        // I have separated these because assigning property values to elements in a collection didn't work
        IFirewallRule ruleOut = CreateRule( ruleName, ipAddressBatch, ports, description, remoteIPRange, local, FirewallDirection.Outbound );
        IFirewallRule ruleIn = CreateRule( ruleName, ipAddressBatch, ports, description, remoteIPRange, local, FirewallDirection.Inbound );

        try
        {
            FirewallManager.Instance.Rules.Add( ruleOut );
            rules.Add( ruleOut ); // Succeeded
            FirewallManager.Instance.Rules.Add( ruleIn );
            rules.Add( ruleIn ); // Succeeded
        }
        catch ( Exception ex )
        {
            if ( logFilePath != null )
                Logger.Log( _appName, ex );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "AddInboundAndOutboundRules", ex ) );
        }

        return rules;
    }

    private static FirewallWASRule CreateRule( string ruleName, IAddress[]? ipAddressBatch, ushort[]? ports, string description, IAddress[] remoteIPRange, SingleIP[] local, FirewallDirection direction )
    {
        FirewallWASRule newRule = new( ruleName, FirewallAction.Block, direction, _AllProfiles )
        {
            Description = description,
            Grouping = description,
            Direction = direction, // Trying to force this as the above parameter didn't have an effect
            LocalAddresses = local,
            RemoteAddresses = remoteIPRange.Length > 0 ? remoteIPRange : ipAddressBatch,
            Protocol = FirewallProtocol.Any,
            IsEnable = true,
            //ApplicationName = null,
        };

        if ( ports is not null && ports.Length > 0 && ports[ 0 ] > 0 && ( newRule.Protocol == FirewallProtocol.TCP || newRule.Protocol == FirewallProtocol.UDP ) )
            newRule.RemotePorts = ports;

        return newRule;
    }

    //private static IAddress[] IPV6StringToIAddressArray( string ipV6AddressString )
    //{
    //    IPAddress ipAddress = IPAddress.Parse( ipV6AddressString );
    //    IAddress iAddress = new NetworkAddress( ipAddress );
    //    return [ iAddress ];
    //}

    //private static IAddress[] StringToIAddressArray( string addressDelimitedString )
    //{
    //    List<string> addresses = [];
    //    List<IAddress> addressSet = [];

    //    if ( addressDelimitedString.IndexOf( BATCH_DELIMITER ) > 0 )
    //        addresses = addressDelimitedString.Split( BATCH_DELIMITER )
    //                           .ToList( );
    //    else
    //        addresses.Add( addressDelimitedString );

    //    foreach ( string item in addresses ) // Individual string address entries
    //    {
    //        IPAddress? newAddress = null;
    //        newAddress = IPAddress.Parse( item );
    //        addressSet.Add( new SingleIP( newAddress ) );
    //    }

    //    return addressSet.ToArray( );
    //}

    //private static IAddress StringToIAddress( string address )
    //{
    //    var addressParts = address.Split( '.' )
    //        .Select( s => Convert.ToByte( s ) )
    //        .ToArray( );
    //    IAddress ipAddress = new SingleIP( new IPAddress( addressParts ) );
    //    return ipAddress;
    //}

    private static IAddress StringToIAddress( string address )
    {
        //byte[] addressBytes = address.Split( ':' )
        //                             .Select( s => Convert.ToByte( s ))
        //                             .ToArray();
        IAddress ipAddress = new SingleIP( IPAddress.Parse( address ) );
        return ipAddress;
    }

    //private static bool RuleExists( IAddress[] remoteIPAddresses, FirewallDirection direction )
    //{
    //    IEnumerable<IFirewallRule> existingRules = FirewallManager.Instance
    //                    .Rules
    //                    .Where( w => w.Name.StartsWith( "@(imported) " ) )
    //                    .Where( w => w.Name.EndsWith( "_Blocklist" ) )
    //                    .Where( w => w.Direction == direction )
    //                    .Where( w => IAddressesToString( w.RemoteAddresses ) == IAddressesToString( remoteIPAddresses ) );

    //    return existingRules.Any( );
    //}

    internal static string? IAddressesToString( IAddress[] addresses )
    {
        if ( addresses.Length > 0 )
            return string.Join( BATCH_DELIMITER, addresses.Select( s => s.ToString( ) ).ToArray( ) );
        else
            return string.Empty;
    }

    private static bool RuleExists( IPRange remoteIPAddressRange, FirewallDirection direction, string ruleName, string description )
    {
        IAddress[] remoteIPRange = [new SingleIP( remoteIPAddressRange.StartAddress ), new SingleIP( remoteIPAddressRange.EndAddress )];
        IFirewallRule newRule = CreateRule( ruleName, null, [], description, remoteIPRange, [], direction );
        int count = FirewallManager.Instance
                        .Rules
                        .Where( w => w.RemoteAddresses == remoteIPRange && w.Direction == direction )
                        .Count( );
        return count > 0;
    }

    internal static IList<IFirewallRule> AddFirewallRulesFor( string ruleName, string siteName, ref List<CandidateEntry> newEntries, string logFilePath = "" ) //
    {
        List<IFirewallRule> newRules = [];

        // SaveAddressBatches( ruleName, newEntries, ref newRules );
        try
        {
            SaveAddressSets( ruleName, siteName, ref newEntries, ref newRules );
            SaveAddressRanges( ruleName, siteName, ref newEntries, ref newRules );
        }
        catch ( Exception e )
        {
            if ( logFilePath != null )
                Logger.Log( _appName, e );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "AddFirewallRulesFor", e ) );
        }
        // SaveAddresses( ruleName, newEntries, ref newRules );

        return newRules;
    }

    private static void SaveAddressRanges( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    {
        foreach ( var entry in newEntries.Where( w => w.Name == siteName && w.IPAddressRange is not null )
                                                     .OrderBy( o => o.Sort[ 0 ] )
                                                     .ThenBy( o => o.Sort[ 1 ] )
                                                     .ThenBy( o => o.Sort[ 2 ] )
                                                     .ThenBy( o => o.Sort[ 3 ] ) )
        {
            if ( !RuleExists( entry.IPAddressRange!, FirewallDirection.Outbound, ruleName, entry.Description! )
                && !RuleExists( entry.IPAddressRange!, FirewallDirection.Inbound, ruleName, entry.Description! ) )
            {
                IPRange? addressRange = entry.IPAddressRange;
                newRules.AddRange( AddInboundAndOutboundRules( ruleName, null, entry.IPAddressRange, entry.Protocol, entry.Ports ) );
            }
        }
    }

    //private static bool SaveAddressBatches( string ruleName, IList<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    //{
    //    int savedCount = 0;

    //    // NOTE: IP V6 addresses are currently in the IPAddress property
    //    foreach ( var entry in newEntries.Where( w => !string.IsNullOrEmpty( w.IPAddressBatch ) ) ) // .Where( w => w.AddressType == IPAddressType.IPv4 )
    //                                                                                                //.OrderBy( o => o.Sort ) )
    //    {
    //        IAddress[]? addressBatch = StringToIAddressArray( entry.IPAddressBatch );
    //        if (entry.IPAddressSet is not null && entry.IPAddressSet.Length == addressBatch.Length ) // Rather let SaveAddessSets to the job
    //            return true;

    //        if ( addressBatch is not null
    //            && !RuleExists( addressBatch, FirewallDirection.Outbound )
    //            && !RuleExists( addressBatch, FirewallDirection.Inbound ) )
    //        {
    //            newRules.AddRange( AddInboundAndOutboundRules( ruleName, addressBatch, entry.IPAddressRange, entry.Protocol, entry.Ports ) );
    //            savedCount++;
    //        }
    //    }

    //    return savedCount == newEntries.Count;
    //}

    private static bool SaveAddressSets( string ruleName, string siteName, ref List<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    {
        int savedCount = 0;

        // NOTE: IP V6 addresses are currently in the IPAddress property
        foreach ( var entry in newEntries.Where( w => w.Name == siteName && w.IPAddressSet is not null && w.IPAddressSet.Length > 0 ) )
        {
            newRules.AddRange( AddInboundAndOutboundRules( ruleName, entry.IPAddressSet, entry.IPAddressRange, entry.Protocol, entry.Ports ) );
            savedCount++;
        }

        return savedCount == newEntries.Count;
    }

    //private static bool SaveAddresses( string ruleName, IList<CandidateEntry> newEntries, ref List<IFirewallRule> newRules )
    //{
    //    int savedCount = 0;

    //    // NOTE: IP V6 addresses are currently in the IPAddress property
    //    foreach ( var entry in newEntries.Where( w => !string.IsNullOrEmpty( w.IPAddress ) ) )
    //    {
    //        IAddress[] addressSet = StringToIAddressArray( entry.IPAddress );
    //        newRules.AddRange( AddInboundAndOutboundRules( ruleName, addressSet, entry.IPAddressRange, entry.Protocol, entry.Ports ) );
    //        savedCount++;
    //    }

    //    return savedCount == newEntries.Count;
    //}

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

        return addressType;
    }

    //internal static bool UpdateFirewallRulesFor( string ruleName, IList<CandidateEntry> newEntries )
    //{
    //    bool updated = false;

    //    var allExisting = FirewallManager.Instance.Rules
    //                    .Where( w => w.Name == ruleName )
    //                    .Where( w => ( (IPAddress)w.RemoteAddresses[ 0 ] ).Address > 0 ); // TODO: Better and more modern way than this

    //    var toUpdate = from n in newEntries
    //                   join e in allExisting
    //                   on n.IPAddress equals e.RemoteAddresses[ 0 ].ToString( )
    //                   select new { e, n };

    //    foreach ( var target in toUpdate )
    //    {
    //        byte[] ipAddressBytes = StringToBytes( target.n.IPAddress! );
    //        IAddress[] remoteAddr = new SingleIP[] { new SingleIP( ipAddressBytes ) };
    //        target.e.RemoteAddresses = remoteAddr;
    //        target.e.RemotePorts = target.n.Ports;
    //        target.e.Action = FirewallAction.Block;
    //        target.e.Direction = FirewallDirection.Outbound;
    //        target.e.IsEnable = true;
    //        target.e.Protocol = FirewallProtocol.Any;
    //    }

    //    return updated;
    //}

    internal static byte[] StringToBytes( string source )
    {
        System.Text.ASCIIEncoding enc = new( );
        return enc.GetBytes( source );
    }

    // Not in use - GOOD because some of the logic looks suspect
    //internal static IAddress[] StringToIAddresses( string address )
    //{
    //    if ( address.Contains( BATCH_DELIMITER ) )
    //    {
    //        string[] addresses = address.Split( BATCH_DELIMITER );
    //        var ints = addresses.Select( s => s.Split( '.' ) );
    //        //IAddress[] addressBatch = addresses.Select( s => s.Split( '.' ).Select( b => (byte)Convert.ToInt32( b ) ) );
    //    }
    //    else
    //    {
    //        string[] parts = address.Split( '.' );
    //        byte[] bytes = new byte[ parts.Length ];
    //        for ( int i = 0; i < parts.Length; i++ )
    //        {
    //            bytes[ i ] = (byte)Convert.ToInt32( parts[ i ] );
    //        }

    //        IPAddress ip = new IPAddress( bytes );
    //        return new SingleIP[] { new SingleIP( ip ) };
    //    }

    //    return [];
    //}

    private record StringAndType( string AddressString, IAddress IpAddress, CandidateEntry Owner );

    internal static List<CandidateEntry> DownloadBlocklists( MaintainUI? maintainUIForm, RemoteSite? selectedSite = null, string logPath = "" )
    {
        List<CandidateEntry> data = [];
        IList<RemoteSite> sites = [];

        if ( selectedSite is not null )
            sites = [ selectedSite ];
        else
            sites = ListDownloadSites( selectedSite );

        foreach ( RemoteSite site in sites )
        {
            IEnumerable<CandidateEntry> newData = DownloadSiteData( site, logPath );
            if ( newData.Count() > 0 )
            {
                data.AddRange( newData );

                if ( maintainUIForm is not null )
                    maintainUIForm.StatusMessage.Text = $"Updating {site!.Name} downloaded date and time ...";
                else
                    Logger.Log( "BlocklistManager", $"Downloaded {site.Name} blocklist(s)" );

                UpdateLastDownloaded( site );
            }
        }

        return data.Select( s => new CandidateEntry( )
            {
                Name = s.Name,
                IPAddress = s.IPAddress ?? "",
                IPAddressRange = s.IPAddressRange,
                IPAddressSet = s.IPAddressSet,
                AddressType = string.IsNullOrEmpty( s.IPAddress ) ? IPAddressType.Invalid : s.IPAddress.IndexOf( ':' ) > 0 ? IPAddressType.IPv6 : IPAddressType.IPv4,
                Ports = s.Ports,
                Protocol = s.Protocol,
                Status = s.Status,
                Description = s.Description,
                Country = s.Country,
                Malware = s.Malware,
            } )
            .OrderBy( o => o.Sort[ 0 ] )
            .ThenBy ( o => o.Sort[ 1 ] )
            .ThenBy ( o => o.Sort[ 2 ] )
            .ThenBy ( o => o.Sort[ 3 ] )
            .ToList( );
    }

    internal static void ConvertIPAddressesToIPAddressSets( ref List<CandidateEntry> data, List<RemoteSite> sites )
    {
        List<CandidateEntry> rationalised = [];

        // This needs to be run per download site
        foreach ( RemoteSite site in sites )
        {
            // TODO: The site name has already been modified to @(imported) etc. for the firewall
            // Rather only convert it just before saving to simply manipulation of the data sets before saving.
            // Delete and other activities will then also need to be reviewed.

            List<CandidateEntry> work = data.Where( w => w.Name == site.Name ) // Validate
                                            .Where( w => !string.IsNullOrEmpty( w.IPAddress ) && w.IPAddressRange is null )
                                            .ToList( );
            //            int sequence = 1;

            while ( work.Count > 0 && work.Count > MAX_FIREWALL_BATCH_SIZE )
            {
                List<CandidateEntry> candidates = work.Take( MAX_FIREWALL_BATCH_SIZE )
                                                      .ToList( );

                //            work.RemoveRange( work.IndexOf(candidates.First()), MAX_FIREWALL_BATCH_SIZE );
                rationalised.Add( BuildIPAddressSet( candidates/*, sequence */) );
                foreach ( var candidate in candidates ) // I'd love to find a better way to do this. I can't count on getting the correct arguments for RemoveRange
                {
                    work.Remove( candidate );
                }

                //                sequence++;
            }

            if ( work.Count > 0 )
                rationalised.Add( BuildIPAddressSet( work, /*sequence, */work.Count ) );
            //rationalised.AddRange( work );
            rationalised.AddRange( data.Where( w => w.Name == site.Name ) // Validate
                                       .Where( w => w.IPAddressRange is not null && w.IPAddressRange.StartAddress is not null && w.IPAddressRange.EndAddress is not null ) );
        }

        data.Clear( );
        data.AddRange( rationalised );
        rationalised.Clear();
    }

    internal static void RemovePrivateAddressesRanges( ref List<CandidateEntry> candidates, out int numberRemoved )
    {
        numberRemoved = candidates.Count;
        candidates = candidates.Where( x => !string.IsNullOrEmpty( x.IPAddress )
                                                    && !x.IPAddress!.StartsWith( "10." )
                                                    && !x.IPAddress!.StartsWith( "127." )
                                                    && !x.IPAddress!.StartsWith( "169.254." )
                                                    && !x.IPAddress!.StartsWith( "172.16." )
                                                    && !x.IPAddress!.StartsWith( "172.17." )
                                                    && !x.IPAddress!.StartsWith( "172.18." )
                                                    && !x.IPAddress!.StartsWith( "172.19." )
                                                    && !x.IPAddress!.StartsWith( "172.20." )
                                                    && !x.IPAddress!.StartsWith( "172.21." )
                                                    && !x.IPAddress!.StartsWith( "172.22." )
                                                    && !x.IPAddress!.StartsWith( "172.23." )
                                                    && !x.IPAddress!.StartsWith( "172.24." )
                                                    && !x.IPAddress!.StartsWith( "172.25." )
                                                    && !x.IPAddress!.StartsWith( "172.26." )
                                                    && !x.IPAddress!.StartsWith( "172.27." )
                                                    && !x.IPAddress!.StartsWith( "172.28." )
                                                    && !x.IPAddress!.StartsWith( "172.29." )
                                                    && !x.IPAddress!.StartsWith( "172.30." )
                                                    && !x.IPAddress!.StartsWith( "172.31." )
                                                    && !x.IPAddress!.StartsWith( "192.168." )
                                    )
                                .Union( candidates.Where( w => w.IPAddress == null && w.IPAddressRange != null ))
                                .ToList();

        numberRemoved -= candidates.Count;
    }

    internal static void RemoveInvalidAddresses( ref List<CandidateEntry> data, out int numberRemoved )
    {
        numberRemoved = data.Count;
        string[] invalidIPs = data.Where( w => w.AddressType == IPAddressType.IPv4 || w.AddressType == IPAddressType.Invalid )
                                      .Select( s => new { IPAddress = s.IPAddress, Parts = s.IPAddress.Split( '.' ) } )
                                      .Where( w => w.Parts.Length < 4 )
                                      .Select( s => s.IPAddress! )
                                      .ToArray( );

        foreach ( string ipAddress in invalidIPs )
            data.Remove( data.Find( f => f.IPAddress == ipAddress ) );

        numberRemoved -= data.Count;
    }

    internal static void RemoveDuplicates( ref List<CandidateEntry> data, out int numberRemoved )
    {
        numberRemoved = data.Count;
        // Address ranges; not attempting deduplication
        data = data.Where( w => w.IPAddress is null && w.IPAddressRange is not null)
                   .GroupBy( g => g.IPAddressRange )
                   .Select( s => new { IPAddressRange = s.Key, data = s.AsEnumerable( ) } )
                   .Select( s => new CandidateEntry( )
                        {
                            Name = s.data.Min( m => m.Name ),
                            IPAddress = null,
                            IPAddressRange = s.IPAddressRange,
                            IPAddressSet = s.data.Min( m => m.IPAddressSet ?? [] )!,
                            AddressType = s.data.Min( m => m.AddressType ),
                            Ports = s.data.Min( m => m.Ports ?? [] )!,
                            Protocol = s.data.Min( m => m.Protocol ?? FirewallProtocol.Any )!,
                            Status = s.data.Min( m => m.Status ),
                            Description = s.data.Min( m => m.Description ),
                            Country = s.data.Min( m => m.Country ?? "-" ),
                            Malware = s.data.Min( m => m.Malware ?? "-" ),
                        }
                    )
                   .Union( data.Where( w => !string.IsNullOrEmpty( w.IPAddress ) )
                               .GroupBy( g => g.IPAddress )
                               .Select( s => new { IPAddress = s.Key, data = s.AsEnumerable( ) } )
                               .Select( s => 
                                    new CandidateEntry( )
                                    {
                                        Name = s.data.Min( m => m.Name ),
                                        IPAddress = s.IPAddress,
                                        IPAddressRange = null,
                                        IPAddressSet = [],
                                        AddressType = s.data.Min( m => m.AddressType ),
                                        Ports = MergeShorts( s.data.Select( t => t.Ports ) ), // s.entriesForBatch.Min( m => m.Ports ),
                                        Protocol = s.data.Max( m => m.Protocol ?? FirewallProtocol.Any )!,
                                        Status = s.data.Max( m => m.Status ?? "-" ),
                                        Description = s.data.Max( m => m.Description ?? "-" ),
                                        Country = s.data.Max( m => m.Country ?? "-" ),
                                        Malware = s.data.Max( m => m.Malware ?? "-" ),
                                    }
                               ) 
                    )
                   .ToList();

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
                results = pre.Select( p => (ushort)Convert.ToInt16( p ) )
                             .Distinct( )
                             .ToList( );
            }
        }

        //results = test.Split( ';' ).Select( s => (ushort)Convert.ToInt16( s ) ).ToArray( );

        return [ .. results ];
    }

    //internal static List<CandidateEntry> RemoveDuplicates( List<CandidateEntry> entriesForBatch )
    //{
    //    try
    //    {
    //        // Remove duplicates
    //        /*List<IGrouping<string, CandidateEntry>> */
    //        var grouped = entriesForBatch
    //                 .GroupBy( g => ( string.IsNullOrEmpty( g.IPAddress ) ? g.IPAddressRange!.StartAddress.ToString( ) : g.IPAddress ),
    //                                        a => new CandidateEntry( )
    //                                        {
    //                                            AddressType = a.AddressType,
    //                                            Country = a.Country ?? "-",
    //                                            Description = a.Description,
    //                                            IPAddress = a.IPAddressRange is null ? a.IPAddress : string.Empty, // a.IPAddressRange.ToString( ),
    //                                            IPAddressRange = a.IPAddressRange,
    //                                            Malware = a.Malware ?? "-",
    //                                            Name = $"@(imported) All Blocklists", // a.Name,
    //                                            Ports = a.Ports is null ? [] : a.Ports,
    //                                            Protocol = a.Protocol is null ? FirewallProtocol.Any : a.Protocol,
    //                                            Status = a.Status is null ? "-" : a.Status
    //                                        } ).ToList();

    //        /*var inProgress = */ entriesForBatch = grouped
    //                 .Where( w => w.Min( m => m.IPAddressRange ) is not null )
    //                 .Select( s => new CandidateEntry( )
    //                 {
    //                     AddressType = s.Min( m => m.AddressType ),
    //                     Country = s.Max( m => m.Country ),
    //                     Description = s.Min( m => m.Description ),
    //                     IPAddress = null, // s.Count( c => c.IPAddressRange is null ) < 1 ? s.Key : s.Min( m => m.IPAddressRange!.ToString( ) ),
    //                     IPAddressRange = s.Min( m => m.IPAddressRange ),
    //                     Malware = s.Max( m => m.Malware ),
    //                     Name = s.Min( m => m.Name ),
    //                     Ports = [], // s.Max( m => m.Ports ),
    //                     Protocol = FirewallProtocol.Any, // s.Max( m => m.Protocol ),
    //                     Status = s.Min( m => m.Status )
    //                 } )
    //                 .Union
    //                 (
    //                    grouped.Where( w => !string.IsNullOrEmpty( w.Min( m => m.IPAddress ) ) )
    //                           .Select( s => new CandidateEntry( )
    //                           {
    //                               AddressType = s.Min( m => m.AddressType ),
    //                               Country = s.Max( m => m.Country ),
    //                               Description = s.Min( m => m.Description ),
    //                               IPAddress = s.Min( m => m.IPAddress ), // s.Count( c => c.IPAddressRange is null ) < 1 ? s.Key : s.Min( m => m.IPAddressRange!.ToString( ) ),
    //                               IPAddressRange = s.Min( m => m.IPAddressRange ),
    //                               Malware = s.Max( m => m.Malware ),
    //                               Name = s.Min( m => m.Name ),
    //                               Ports = [], // s.Max( m => m.Ports ),
    //                               Protocol = FirewallProtocol.Any, // s.Max( m => m.Protocol ),
    //                               Status = s.Min( m => m.Status )
    //                           } )
    //                 )
    //                 /*
    //                         .ToList( );


    //        entriesForBatch = inProgress*/.OrderBy( o => o.Name )
    //                         .ThenBy( o => o.Sort[ 0 ] )
    //                         .ThenBy( t => t.Sort[ 1 ] )
    //                         .ThenBy( t => t.Sort[ 2 ] )
    //                         .ThenBy( t => t.Sort[ 3 ] )
    //                         .ThenBy( u => u.AddressType == IPAddressType.IPv6 ? u.Sort[ u.Sort.Length - 3 ] : 0 )
    //                         .ThenBy( u => u.AddressType == IPAddressType.IPv6 ? u.Sort[ u.Sort.Length - 2 ] : 0 )
    //                         .ThenBy( u => u.AddressType == IPAddressType.IPv6 ? u.Sort[ u.Sort.Length - 1 ] : 0 )
    //                         .ToList( );
    //    }
    //    catch ( Exception ex )
    //    {
    //        Debug.Print( ex.Message );
    //    }

    //    return entriesForBatch;
    //}

    private static List<CandidateEntry> DownloadSiteData( RemoteSite site, string logPath = "" )
    {
        List<CandidateEntry> data = [];
        foreach ( string fileUrl in site.FilePaths )
        {
            string url = fileUrl.Replace( ",", "" ); //.ToLower();
            switch ( site.FileTypeID )
            {
                case 2: // Json
                    {
                        // TODO: Cater for and test Feodo as well
                        
                        //HttpHelper.PrepareEntriesFromUrl_Json( site, url, ref data );
                        //break;

                        using TextDataCollector collector = new ( );
                        string fileExtension = string.Empty;
                        string textData = collector.ReadData( site, out fileExtension!, url );
                        if ( textData.Length > 2 )
                        {
                            using DataTranslatorJson translator = new ( );
                            return translator.TranslateFileData( site, textData );
                        }
                        break;
                    }
                case 3: // Xml
                    {
                        // TODO: Test

                        //entriesForBatch = HttpHelper.PrepareEntriesFromUrl_Xml( BlocklistSource.FeodoSource, url );
                        // HttpHelper.PrepareEntriesFromUrl_Xml( site, url, ref data );
                        using TextDataCollector collector = new ( );
                        //string logFilePath = logPath.EndsWith( "\\" ) ? logPath : logPath + "\\";
                        //logFilePath += $"{_appName}.log";
                        Stream? downloaded = collector.ReadHtmlStreamFromUrl( site, fileUrl, logPath );
                        if ( downloaded is not null )
                        {
                            using IDataTranslator translator = new DataTranslatorXml( );
                            data = translator.TranslateDataStream( site, downloaded );
                        }
                        
                        //data = translator.
                        break;
                    }
                case 4: // Tab delimited
                case 9: // Comma delimited
                    {

                        // TODO: Test
                        //HttpHelper.PrepareEntriesFromUrl_Delimited( site, url, delimiter, ref data );
                        // Fetch the data
                        using TextDataCollector collector = new ( );
                        string downloaded = collector.ReadData( site, out string? fileExtension, url );

                        if ( !string.IsNullOrEmpty( downloaded ) )
                        {
                            using IDataTranslator translator = new DataTranslatorDelimited( );
                            data = translator.TranslateFileData( site, downloaded );
                        }

                        break;
                    }
                case 5: // Zip archive containing Json
                case 6: // Zip archive containing text
                case 7: // Zip archive containing delimited data
                    {
                        // Extract the data file from the ZIP archive
                        //string fileContents = HttpHelper.ReadZipFileContents( site, url, out string extension );
                        using TextDataCollector collector = new ( );
                        string fileContents = collector.ReadZipData( site, out string extension, url );

                        // Process the extracted data
                        data = site.FileTypeID switch
                        {
                            5 => [],
                            6 => [],
                            7 => [],
                            //".json" => HttpHelper.PrepareEntriesFromUrl_Json( site, ) // THIS WON'T WORK, going to need an override or alternative approach
                            _ => []
                        };

                        break;
                    }
                default:
                    {
                        //HttpHelper.PrepareEntriesFromUrl_Text( site, url, ref data, logPath );
                        using TextDataCollector collector = new ();
                        string fileExtension = string.Empty;
                        string textData = collector.ReadData( site, out fileExtension!, url );
                        if ( textData.Length > 0 )
                        {
                            using IDataTranslator translator = new DataTranslatorTextData();
                            return translator.TranslateFileData( site, textData );
                        }
                        break;
                    }
            }
        }

        return data;
    }

    internal static List<string> TextToStringList( string text )
    {
        string[] asArray = [];
        if ( text.Contains( Environment.NewLine ) )
            return text.Split( Environment.NewLine ).ToList( );
        else // if ( text.Contains( '\n' ) )
            return text.Split( '\n' ).ToList();
    }

    internal static bool UrlHostExists( string url )
    {
        string domain = url;
        if ( domain.Contains( ':' ) )
            domain = domain[ ( domain.IndexOf( "://" ) + 3 ).. ];

        if ( domain.Contains( '/' ) )
            domain = domain[ ..domain.IndexOf( '/' ) ];

        bool exists = false;
        try
        {
            exists = Dns.GetHostAddresses( domain ).Length > 0;
        }
        catch ( Exception ex ) { }

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
    /// <returns></returns>
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

        CandidateEntry result = new( );
        if ( addressSet.Count > 0 )
        {
            CandidateEntry newEntry = entriesForBatch.First( );
            result = new( )
            {
                AddressType = newEntry.AddressType,
                IPAddressRange = newEntry.IPAddressRange,
                IPAddress = null,
                IPAddressSet = addressSet.Select( s => s.IpAddress )
                                         .ToArray( ),
                Country = newEntry.Country,
                Description = newEntry.Description,
                Malware = newEntry.Malware,
                Name = newEntry.Name, // + $" {sequence}", // Nice thought but can make deleting existing rules problematic. If reinstated make sure that sequence restarts for each site
                Ports = newEntry.Ports,
                Protocol = newEntry.Protocol,
                Status = newEntry.Status,
            };
        }

        return result;
    }

    internal static void /* Return the new Task */ ScheduleAutoUpdates( string? siteName = null )
    {
        using UpdateScheduler scheduler = new( );
        scheduler.ShowDialog( );
    }

    //[GeneratedRegex( "dir.*?<a href=\"(http:)?(?<dir>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexDirectory( );

    //[GeneratedRegex( "[0-9] <a href=\"(http:)?(?<file>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexFile( );
}
