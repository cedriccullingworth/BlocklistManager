using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

namespace BlocklistManager;

internal static class Program
{
    //private static readonly string _logFile = string.Empty;
    private static string _appName = string.Empty;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main( string[] args )
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        /* 
         * args examples:
         * /Sites:AllCurrent /LogPath:D:\Projects\BlocklistManager\bin\Debug\net8.0-windows\Log
         * /Sites:1;2;3;4 /LogPath:D:\Projects\BlocklistManager\bin\Debug\net8.0-windows\Log
        */

        ApplicationConfiguration.Initialize( );
        _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;

        if ( args.Length == 0 )
        {
            Application.Run( new MaintainUI( ) );
            return;
        }
        else
        {
            bool allActive = true;
            List<RemoteSite> sites = [ .. Maintain.ListDownloadSites( null ) ];
            foreach ( string arg in args )
            {
                if ( arg.StartsWith( "/sites:", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    ReadSitesArgument( ref allActive, ref sites, arg );
                }
                else if ( arg.StartsWith( "/logpath:", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    ReadLogPathArgument( arg );
                }
                else // Show command line help
                {
                    Console.WriteLine( $"{_appName} command line arguments:" );
                    Console.WriteLine( $"/sites:allCurrent OR /sites:[list of remote site IDs separated by semicolons(;)]" );
                    Console.WriteLine( $"Example: {_appName}.exe /sites:1;2;3" );
                    Console.WriteLine( );
                    Console.WriteLine( $"/logpath:<full directory name of the log files>" );
                    Console.WriteLine( $@"Example: {_appName}.exe /sites:1;2;3 /logPath:C:\Program Files\{_appName}\Logs" );
                    return;
                }
            }

            DateTime now = new DateTime( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            CultureInfo culture = CultureInfo.CurrentCulture;
            string startedAt = now.ToLocalTime( ).ToString( "F", culture.DateTimeFormat );
            Logger.Log( string.Empty, string.Empty );
            Logger.Log( _appName!, $"................. BLOCKLIST UPDATES STARTED AT {startedAt} .................\r\n" );

            if ( allActive )
            {
                ProcessAllActiveSites( sites );
            }
            else if ( sites.Count > 0 )
            {
                ProcessSitesList( sites );
            }
            else
            {
                Logger.Log( _appName!, "No Firewall updates can be executed before 30 minutes after their last updated time... or no defined download sites were found" );
            }

            now = new DateTime( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            string endedAt = now.ToLocalTime( ).ToString( "T", culture.DateTimeFormat );
            Logger.Log( _appName!, $"................. END OF BLOCKLIST UPDATES (STARTED AT {startedAt}, ENDED AT {endedAt}) ................." );
            Logger.Log( string.Empty, string.Empty );
        }
    }

    private static void ReadLogPathArgument( string arg )
    {
        string _logFileName = $"{_appName}.log";
        string path = arg.ToLower( )
                         .Replace( "/logpath:", string.Empty );
        Logger.LogPath = ( path.EndsWith( '\\' ) ? path : path + "\\" ) + _logFileName;
    }

    private static void ReadSitesArgument( ref bool allActive, ref List<RemoteSite> sites, string arg )
    {
        if ( !arg.ToLower( )
                .Replace( "/sites:", "" )
                .Split( ';' )[ 0 ]
                .Equals( "allcurrent", StringComparison.CurrentCultureIgnoreCase )
           )
        {
            allActive = false;
            string[] ids = arg.ToLower( )
                              .Replace( "/sites:", "" )
                              .Split( ';' );
            sites = (
                        from s in sites
                        join i in ids.Where( w => StringUtilities.IsNumeric( w ) )
                        on s.ID equals Convert.ToInt32( i )
                        select s
                    ).ToList( );
        }
    }

    private static void ProcessSitesList( List<RemoteSite> sites )
    {
        Logger.Log( _appName!, "Firewall update started... " );
        foreach ( RemoteSite site in sites )
        {
            var entries = Maintain.DownloadBlocklists( null, site, Logger.LogPath );
            if ( entries.Count > 0 )
            {
                Logger.Log( _appName, "Removing private address ranges ..." );
                Maintain.RemovePrivateAddressesRanges( ref entries, out int numberRemoved );
                Logger.Log( _appName, $"Removed {numberRemoved} private address ranges" );

                Logger.Log( _appName, "Removing duplicates ..." );
                Maintain.RemoveDuplicates( ref entries, out numberRemoved );
                Logger.Log( _appName, $"Removed {numberRemoved} duplicates" );

                Logger.Log( _appName, "Removing any invalid addresses ..." );
                Maintain.RemoveInvalidAddresses( ref entries, out numberRemoved );
                Logger.Log( _appName, $"Removed {numberRemoved} invalid addresses" );

                Logger.Log( _appName, $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ..." );
                Maintain.ConvertIPAddressesToIPAddressSets( ref entries, [ site ] );
                Logger.Log( _appName, $"Consolidation completed succesfully" );

                if ( entries is not null && entries.Count > 0 )
                {
                    //Logger.Log( _appName, $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ..." );
                    //entries = Maintain.ConvertIPAddressesToIPAddressSets( entries, [site] );

                    //Logger.Log( _appName, $"{site.Name} blocklist was retrieved successfully" );
                    string ruleName = $"@(imported) {site.Name}_Blocklist";
                    if ( Maintain.DeleteExistingFirewallRulesFor( ruleName ) )
                    {
                        Logger.Log( _appName, $"Existing firewall rules for {site.Name} were removed" );
                        var rules = Maintain.AddFirewallRulesFor( ruleName, site.Name, ref entries );
                        if ( rules.Count > 0 )
                        {
                            Logger.Log( _appName, $"Firewall rules for {site.Name} blocklist(s) were created\r\n" );
                            if ( !Maintain.UpdateLastDownloaded( site ) )
                                Logger.Log( _appName, $"Updating last processed time of {site.Name} failed" );
                        }
                        else
                            Logger.Log( _appName, $"Creation of firewall rules for {site.Name} failed\r\n" );
                    }
                    else
                        Logger.Log( _appName, $"Removal of existing firewall rules for {site.Name} failed" );
                }
                else
                {
                    Logger.Log( _appName, $"Retrieval of {site.Name} blocklist failed or the blocklist was empty" );
                }
            }
        }
    }

    private static void ProcessAllActiveSites( List<RemoteSite> sites )
    {
        List<CandidateEntry> candidateRules =  Maintain.DownloadBlocklists( null, null, Logger.LogPath );

        Logger.Log( _appName, "Removing private address ranges ..." );
        Maintain.RemovePrivateAddressesRanges( ref candidateRules, out int numberRemoved );
        Logger.Log( _appName, $"Removed {numberRemoved} private address ranges" );


        Logger.Log( _appName, "Removing duplicates ..." );
        Maintain.RemoveDuplicates( ref candidateRules, out numberRemoved );
        Logger.Log( _appName, $"Removed {numberRemoved} duplicates" );

        Logger.Log( _appName, "Removing any invalid addresses ..." );
        Maintain.RemoveInvalidAddresses( ref candidateRules, out numberRemoved );
        Logger.Log( _appName, $"Removed {numberRemoved} invalid addresses" );

        Logger.Log( _appName, $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ..." );
        Maintain.ConvertIPAddressesToIPAddressSets( ref candidateRules, sites );
        Logger.Log( _appName, $"Consolidation completed succesfully" );

        foreach ( RemoteSite site in sites )
        {
            try
            {
                ReplaceSiteRules( site, candidateRules, Logger.LogPath );
            }
            catch ( Exception ex )
            {
                Logger.Log( _appName, ex );
            }
        }
    }

    private static void ReplaceSiteRules( RemoteSite site, List<CandidateEntry> candidateRules, string logFilePath = "" )
    {
        string ruleName = $"@(imported) {site.Name}_Blocklist";

        // Delete all rules added by this program
        Logger.Log( _appName, $"Removing existing rules for the {site!.Name} blocklist(s) ..." );
        Maintain.DeleteExistingFirewallRulesFor( ruleName, logFilePath );
        Logger.Log( _appName, $"Existing firewall rules for {site.Name} were removed" );

        // Add all of the rules that we've just imported
        Logger.Log( _appName, $"Creating new rules for the {site!.Name} blocklist(s) ..." );
        Maintain.AddFirewallRulesFor( ruleName, site.Name, ref candidateRules, logFilePath );
        Logger.Log( _appName, $"Firewall rules for {site.Name} blocklist(s) were created\r\n" );
    }
}