﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;

using BlocklistManager.Models;

using SharpCompress.Readers;
using SharpCompress.Readers.Zip;

namespace BlocklistManager.Classes;

/// <summary>
/// The Downloader class is used to download data from the different blocklist data sources
/// </summary>
internal sealed class Downloader : IDisposable
{
    /// <summary>
    /// Download timeout in seconds
    /// </summary>
    private const int TIMEOUT_SECONDS = 30;

    /// <summary>
    /// Not in use
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string ReadData( string filePath )
    {
        throw new NotImplementedException( );
    }

    /// <summary>
    /// Read text data from a remote site
    /// </summary>
    /// <param name="site">The site name</param>
    /// <param name="fileExtension">The file extension</param>
    /// <param name="url">The Url of the file</param>
    /// <returns>The downloaded data as a string</returns>
    public static string ReadData( RemoteSite site, out string? fileExtension, string url )
    {
        string textData = "";
        fileExtension = string.Empty;
        return DownloadText( site, url, ref textData );
    }

    /// <summary>
    /// Read a data stream from a remote site
    /// </summary>
    /// <param name="site">The site name</param>
    /// <param name="url">The Url of the data stream</param>
    /// <returns>The downloaded stream</returns>
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
            Maintain.StatusMessage( url, StringUtilities.ExceptionMessage( "ReadHtmlStreamFromUrl", e ) );
            return null;
        }
    }

    /// <summary>
    /// Downloads and returns unaltered text
    /// </summary>
    /// <param name="site">The download site's RemoteSite model</param>
    /// <param name="url">The download Url</param>
    /// <param name="textData">The downloaded text</param>
    /// <returns>The unchanged downloaded text</returns>
    private static string DownloadText( RemoteSite site, string url, ref string textData )
    {
        // string extension; // = string.Empty;
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
        catch
        {
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
                Maintain.StatusMessage( url, StringUtilities.ExceptionMessage( "DownloadText", ex ) );
            }
        }

        return textData;
    }

    /// <summary>
    /// Read zipped data from a remote site
    /// </summary>
    /// <param name="site">The site name</param>
    /// <param name="fileExtension">The file extension</param>
    /// <param name="url">The Url of the file</param>
    /// <returns>The string of data extracted from the stream stream</returns>
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

    /// <summary>
    /// Extracts data from the zipped data in 'stream'
    /// </summary>
    /// <param name="fileExtension">The file extension</param>
    /// <param name="outputFolder">A location to temporarily store the extracted data</param>
    /// <param name="stream">The zipped data stream to be extracted from</param>
    /// <returns>The string of data extracted from the stream stream</returns>
    private static string ReadFromStream( ref string fileExtension, string outputFolder, Stream? stream )
    {
        if ( stream is not null )
        {
            ZipReader reader = ZipReader.Open( stream );
            reader.WriteAllToDirectory( outputFolder );

            string[] extensions = [ ".txt", ".csv", ".json", ".xml" ];
            IEnumerable<FileInfo> files = new DirectoryInfo( outputFolder ).GetFiles( )
                                                                           .Where( w => extensions.Contains( w.Extension ) );
            if ( files.Count( ) == 1 )
            {
                fileExtension = files.First( )
                                     .Extension;
                return new StreamReader( files.First( ).FullName ).ReadToEnd( );
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Default disposal property
    /// </summary>
    private bool disposedValue;

    /// <summary>
    /// Default disposal method
    /// </summary>
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

    /// <summary>
    /// Default disposal method
    /// </summary>
    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
