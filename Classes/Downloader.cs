using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Windows.Forms;

using BlocklistManager.Models;

using SBS.Utilities;

using SharpCompress.Readers;
using SharpCompress.Readers.Zip;

namespace BlocklistManager.Classes;

internal sealed class Downloader : IDisposable
{
    private const int TIMEOUT_SECONDS = 30;

    private static readonly string _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;

    public string ReadData( string filePath ) => throw new NotImplementedException( );
    public static string ReadData( RemoteSite site, out string? fileExtension, string url )
    {
        string textData = "";
        fileExtension = string.Empty;
        return DownloadText( site, url, ref textData );
    }

    public static Stream? ReadHtmlStreamFromUrl( RemoteSite site, string url )
    {
        HttpClient client = new( )
        {
            BaseAddress = new Uri( url ),
            Timeout = TimeSpan.FromSeconds( TIMEOUT_SECONDS )
        };

        try
        {
            return client.GetStreamAsync( new Uri( url ) ).Result;
        }
        catch ( Exception e )
        {
            if ( Maintain.LogFileFullname == string.Empty )
                MessageBox.Show( e.Message );
            else
            {
                Logger.Log( "ReadHtmlStreamFromUrl", StringUtilities.ExceptionMessage( $"Stream download attempt from {site.Name} failed", e ) );
            }

            return null;
        }
    }

    /// <summary>
    /// Downloads and returns unaltered text
    /// </summary>
    /// <param name="site">The download site's RemoteSite model</param>
    /// <param name="url">The download Url</param>
    /// <param name="textData">The downloaded text</param>
    /// <param name="logFilePath">Optional path of the application's log file</param>
    /// <returns>The unchanged downloaded text</returns>
    private static string DownloadText( RemoteSite site, string url, ref string textData )
    {
        string extension = string.Empty;
        textData = string.Empty;

        HttpClient client = new( )
        {
            BaseAddress = new Uri( url ),
            Timeout = TimeSpan.FromSeconds( TIMEOUT_SECONDS )
        };

        try
        {
            Stream? stream = client.GetStreamAsync( new Uri( url ) ).Result;
            using StreamReader reader = new( stream );
            textData = reader.ReadToEnd( );
        }
        catch // ( Exception e )
        {
            //if ( Maintain.LogFileFullname != string.Empty )
            //{
            //    Logger.LogPath = Maintain.LogFileFullname;
            //    Logger.Log( "DownloadText", StringUtilities.ExceptionMessage( $"Text download attempt from {site.Name} failed", e ) );
            //    Logger.Log( _appName, "Trying a different approach now" );
            //}

            // Try to open the data directly
            client.CancelPendingRequests( );

            try
            {
                // Trying WebRequest because HttpClient failed
                // Is this ever used? YES IT IS for sites where other approaches didn't work. Try again sometime.
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create( url );
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                req.UserAgent = "Free firewall update utility";
                req.AuthenticationLevel = AuthenticationLevel.None;
                req.ContentType = "application/text";
                req.Date = DateTime.UtcNow;
                req.UseDefaultCredentials = true;
                // req.Host = site.SiteUrl;  // TEST TO ENSURE THAT THIS DOESN'T CAUSE ANY PROBLEMS
                req.Referer = "https://rodneylab.com/firewall-block-lists-compared/";

                WebResponse response = req.GetResponse( );
                Stream stream = response.GetResponseStream( );
                using StreamReader reader = new( stream );
                textData = reader.ReadToEnd( );
            }
            catch ( Exception ex )
            {
                Logger.Log( "DownloadText", StringUtilities.ExceptionMessage( $"Text download attempt from {site.Name} failed", ex ) );
            }
        }

        return textData;
    }

    public static string ReadZipData( RemoteSite site, out string fileExtension, string url )
    {
        fileExtension = string.Empty;
        string dateTimeIdentifier = $@"{DateTime.UtcNow:yyyyMMddHHmmss}";
        // string path = "https://urlhaus.abuse.ch/downloads/json/";
        string outputFolder = ( Environment.GetEnvironmentVariable( "TEMP" ) ?? Environment.GetEnvironmentVariable( "TMP" ) ) ?? string.Empty;
        outputFolder += $@"\{dateTimeIdentifier}";

        // Create a new work folder if necessary
        if ( !Directory.Exists( outputFolder ) )
            Directory.CreateDirectory( outputFolder );

        // Download the zip
        // Stream? stream = HttpHelper.ReadHtmlStreamFromUrl( url );
        Stream? stream = ReadHtmlStreamFromUrl( site, url );
        if ( stream is not null )
            return ReadFromStream( ref fileExtension, outputFolder, stream );

        return string.Empty;
    }

    private static string ReadFromStream( ref string fileExtension, string outputFolder, Stream? stream )
    {
        if ( stream is not null )
        {
            ZipReader reader = ZipReader.Open( stream );
            reader.WriteAllToDirectory( outputFolder );

            // TODO: lIMIT TO .JSON, .TXT, .CSV ETC
            string[] extensions = [ ".txt", ".csv", ".json", ".xml" ];
            IEnumerable<FileInfo> files = new DirectoryInfo( outputFolder ).GetFiles( )
                                                                           .Where( w => extensions.Contains( w.Extension ) );
            //.Where( w => DateTime.UtcNow - w.CreationTimeUtc < TimeSpan.FromMinutes( 1 ) );
            if ( files.Count( ) == 1 )
            {
                fileExtension = files.First( )
                                     .Extension;
                return new StreamReader( files.First( ).FullName ).ReadToEnd( );
            }
        }

        return string.Empty;
    }

    private bool disposedValue;

    private void Dispose( bool disposing )
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
    // ~TextDataCollector()
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
