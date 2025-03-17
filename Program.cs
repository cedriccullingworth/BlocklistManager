using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Classes;
using BlocklistManager.Models;

// .snk file produced using "sn -k BlocklistManager.snk" in Developer Command Prompt for VS 2022

namespace BlocklistManager;

internal static class Program
{
    private static string _appName = string.Empty;
    // [ProgramFiles64Folder][ProductName]


    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main( string[] args )
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        /* 
         * args examples:
         * /Sites:AllCurrent /LogPath:D:\Projects\BlocklistManager\Log
         * /Sites:1;2;3;4 /LogPath:D:\Projects\BlocklistManager\Log
        */

        ApplicationConfiguration.Initialize( );

        _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;
        var settings = AppSettings.Sections;
        string? logSetting = settings.FirstOrDefault( settings => settings.Key.Equals( "LogFolder", StringComparison.Ordinal ) )?.Value;
        if ( logSetting is not null )
        {
            if ( !Directory.Exists( logSetting ) )
                Directory.CreateDirectory( logSetting );

            if ( !logSetting.EndsWith( "\\", StringComparison.InvariantCultureIgnoreCase ) )
                logSetting += "\\";
            Maintain.LogFileFullname = $"{logSetting}{_appName}.log";
        }
        else
            Maintain.LogFileFullname = Assembly.GetEntryAssembly( )!.FullName!.Replace( ".exe", ".log" );

        if ( args.Length > 0 )
        {
            if ( args.Length > 1 )
            {
                string arg = args[ 1 ];
                if ( args.Length > 2 ) // The second argument has been split by a space
                {
                    // Append the third argument to the second and add back the space which separated them
                    // I haven't attempted to cater for the log folder containing additional spaces
                    arg += $" {args[ 2 ]}";
                    // Lose the third argument
                    args = new string[] { args[ 0 ], arg };
                }
                ReadLogPathArgument( arg );
            }
        }

        string logPath = Maintain.LogFileFullname[ 0..Maintain.LogFileFullname.LastIndexOf( '\\' ) ];
        if ( !Directory.Exists( logPath ) )
            Directory.CreateDirectory( logPath );

        Logger.LogPath = Maintain.LogFileFullname;

        if ( args.Length == 0 )
        {
            string appNameAndVersion = $"{_appName} v{Maintain.ApplicationVersion}";
            MaintainUI ui = new MaintainUI( ) { Text = appNameAndVersion };
            ui.StatusMessage.Text = $"The log folder location is {Maintain.LogFileFullname}";
            Application.Run( ui );
            return;
        }
        else
        {
            bool allActive = true;
            Logger.Log( _appName, string.Empty );
            Logger.Log( _appName, "BlocklistManager starting..." );
            ICollection<RemoteSite> sites = new BlocklistData( ).ListDownloadSites( Maintain.ConnectedDevice!.ID, null );

            foreach ( string arg in args )
            {
                if ( arg.ToLower( CultureInfo.InvariantCulture ).StartsWith( "/sites:", StringComparison.CurrentCultureIgnoreCase ) )
                {
                    ReadSitesArgument( ref allActive, ref sites, arg );
                }
                else if ( arg.ToLower( CultureInfo.InvariantCulture ).StartsWith( "/logpath:", StringComparison.CurrentCultureIgnoreCase ) )
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

            //Tests.Execute( sites );
            //return;

            DateTime now = new( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            CultureInfo culture = CultureInfo.CurrentCulture;
            string startedAt = now.ToLocalTime( ).ToString( "F", culture.DateTimeFormat );
            Logger.Log( string.Empty, string.Empty );
            Logger.Log( _appName!, $"................. START OF BLOCKLIST UPDATES AT {startedAt} .................\r\n" );

            int processedCount = 0;
            Maintain.ProcessDownloads( sites, null, true, out processedCount, out int ipAddressCount, out int allAddressCount );

            now = new DateTime( DateTime.UtcNow.Ticks, DateTimeKind.Utc );
            string endedAt = now.ToLocalTime( ).ToString( "T", culture.DateTimeFormat );
            Logger.Log( _appName!, $"................. END OF BLOCKLIST UPDATES (STARTED AT {startedAt}, ENDED AT {endedAt}) .................\r\n" );

            Logger.Log( _appName!, $"Processed {ipAddressCount} IP addresses/ranges from {allAddressCount} downloaded, and created {processedCount * 2} ({processedCount} inbound, {processedCount} outbound) firewall rules{Environment.NewLine}{Environment.NewLine}" );
        }
    }

    private static void ReadLogPathArgument( string arg )
    {
        string _logFileName = $"{_appName}.log";
        string path = arg.ToLower( CultureInfo.InvariantCulture )
                         .Replace( "/logpath:", string.Empty );
        if ( !Directory.Exists( path ) )
            Directory.CreateDirectory( path );

        Maintain.LogFileFullname = ( path.EndsWith( '\\' ) ? path : path + "\\" ) + _logFileName;
        Logger.LogPath = Maintain.LogFileFullname;
    }

    private static void ReadSitesArgument( ref bool allActive, ref ICollection<RemoteSite> sites, string arg )
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
}