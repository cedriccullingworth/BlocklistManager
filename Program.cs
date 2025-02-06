using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

using SBS.Utilities;

namespace BlocklistManager;

internal static class Program
{
    /*
        PM> dotnet tool install --global dotnet-ef
        Tool 'dotnet-ef' was successfully updated from version '8.0.2' to version '9.0.0'.
        PM> dotnet ef migrations bundle
        Build started...
        Build succeeded.
        Building bundle...
        Done. Migrations Bundle: D:\Projects\BlocklistManager\efbundle.exe
        Don't forget to copy appsettings.json alongside your bundle if you need it to apply migrations.

        dotnet ef migrations bundle --project BlocklistManager/BlocklistManager.csproj --output efbundle
    */

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

        // Create the database if it doesn't exist yet
        // BlocklistDbContextFactory factory = new( );
        // using BlocklistDbContext context = factory.CreateDbContext( args );

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

            DateTime now = new( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            CultureInfo culture = CultureInfo.CurrentCulture;
            string startedAt = now.ToLocalTime( ).ToString( "F", culture.DateTimeFormat );
            Logger.Log( string.Empty, string.Empty );
            Logger.Log( _appName!, $"................. BLOCKLIST UPDATES STARTED AT {startedAt} .................\r\n" );

            int processedCount = 0;
            Maintain.ProcessDownloads( sites, null, true, out processedCount, out int ipAddressCount );

            now = new DateTime( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            string endedAt = now.ToLocalTime( ).ToString( "T", culture.DateTimeFormat );
            Logger.Log( _appName!, $"................. END OF BLOCKLIST UPDATES (STARTED AT {startedAt}, ENDED AT {endedAt}) .................\r\n" );

            Logger.Log( _appName!, $"Processed {ipAddressCount} IP addresses/ranges and created {processedCount * 2} ({processedCount} inbound, {processedCount} outbound) firewall rules" );
        }
    }

    private static void ReadLogPathArgument( string arg )
    {
        string _logFileName = $"{_appName}.log";
        CultureInfo culture = CultureInfo.InvariantCulture;
        string path = arg.ToLower( culture )
                         .Replace( "/logpath:", string.Empty );
        Maintain.LogFileFullname = ( path.EndsWith( '\\' ) ? path : path + "\\" ) + _logFileName;
        Logger.LogPath = Maintain.LogFileFullname;
    }

    private static void ReadSitesArgument( ref bool allActive, ref List<RemoteSite> sites, string arg )
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        if ( !arg.ToLower( culture )
                .Replace( "/sites:", "" )
                .Split( ';' )[ 0 ]
                .Equals( "allcurrent", StringComparison.Ordinal )
           )
        {
            allActive = false;
            string[] ids = arg.ToLower( culture )
                              .Replace( "/sites:", "" )
                              .Split( ';' );
            sites = (
                        from s in sites
                        join i in ids.Where( w => StringUtilities.IsNumeric( w ) )
                        on s.ID equals Convert.ToInt32( i, culture )
                        select s
                    ).ToList( );
        }
    }

    //private static void ProcessAllActiveSites( List<RemoteSite> sites )
    //{
    //    List<CandidateEntry> candidateRules = Maintain.DownloadBlocklists( null, null, Logger.LogPath );

    //    Logger.Log( _appName, "Removing private address ranges ..." );
    //    Maintain.RemovePrivateAddressesRanges( ref candidateRules, out int numberRemoved );
    //    Logger.Log( _appName, $"Removed {numberRemoved} private address ranges" );


    //    Logger.Log( _appName, "Removing duplicates ..." );
    //    Maintain.RemoveDuplicates( ref candidateRules, out numberRemoved );
    //    Logger.Log( _appName, $"Removed {numberRemoved} duplicates" ); 111

    //    Logger.Log( _appName, "Removing any invalid addresses ..." );
    //    Maintain.RemoveInvalidAddresses( ref candidateRules, out numberRemoved );
    //    Logger.Log( _appName, $"Removed {numberRemoved} invalid addresses" );

    //    Logger.Log( _appName, $"Consolidating addresses into sets of {Maintain.MAX_FIREWALL_BATCH_SIZE} ..." );
    //    Maintain.ConvertIPAddressesToIPAddressSets( ref candidateRules, sites );
    //    Logger.Log( _appName, $"Consolidation completed succesfully\r\n" );

    //    foreach ( RemoteSite site in sites )
    //    {
    //        try
    //        {
    //            ReplaceSiteRules( site, candidateRules, Logger.LogPath );
    //        }
    //        catch ( Exception ex )
    //        {
    //            Logger.Log( _appName, ex );
    //        }
    //    }
    //}
}