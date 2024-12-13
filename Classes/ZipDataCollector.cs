using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using SharpCompress.Readers;
using SharpCompress.Readers.Zip;

namespace BlocklistManager.Classes;

internal class ZipDataCollector : IDisposable
{
    private bool disposedValue;

    protected virtual void Dispose( bool disposing )
    {
        if ( !disposedValue )
        {
            if ( disposing )
            {
                // TODO: dispose managed state (managed objects)
                this.Dispose( );
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ZipDataCollector()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    internal void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }

    //public string ReadData( string filePath )
    //{
    //    try
    //    {
    //        using StreamReader reader = new StreamReader( filePath );
    //        return reader.ReadToEnd( );
    //    }
    //    catch ( Exception ex )
    //    {
    //        return "";
    //    }
    //}

    //public string ReadData( RemoteSite site, out string fileExtension, string? url )
    //{
    //    fileExtension = string.Empty;
    //    string dateTimeIdentifier = $@"{DateTime.UtcNow:yyyyMMddHHmmss}";
    //    // string path = "https://urlhaus.abuse.ch/downloads/json/";
    //    string outputFolder = ( Environment.GetEnvironmentVariable( "TEMP" ) ?? Environment.GetEnvironmentVariable( "TMP" ) ) ?? string.Empty;
    //    outputFolder += $@"\{dateTimeIdentifier}";

    //    // Create a new work folder if necessary
    //    if ( !Directory.Exists( outputFolder ) )
    //        Directory.CreateDirectory( outputFolder );

    //    // Download the zip
    //    Stream? stream = HttpHelper.ReadHtmlStreamFromUrl( url );
    //    if ( stream is not null )
    //        return ReadFromStream( ref fileExtension, outputFolder, stream );

    //    return string.Empty;
    //}

    //private static string ReadFromStream( ref string fileExtension, string outputFolder, Stream? stream )
    //{
    //    if ( stream is not null )
    //    {
    //        ZipReader reader = ZipReader.Open( stream );
    //        reader.WriteAllToDirectory( outputFolder );

    //        // TODO: lIMIT TO .JSON, .TXT, .CSV ETC
    //        string[] extensions = [ ".txt", ".csv", ".json", ".xml" ];
    //        IEnumerable<FileInfo> files = new DirectoryInfo( outputFolder ).GetFiles( )
    //                                                                       .Where( w => extensions.Contains( w.Extension ) );
    //        //.Where( w => DateTime.UtcNow - w.CreationTimeUtc < TimeSpan.FromMinutes( 1 ) );
    //        if ( files.Count( ) == 1 )
    //        {
    //            fileExtension = files.First( )
    //                                 .Extension;
    //            return new StreamReader( files.First( ).FullName ).ReadToEnd( );
    //        }
    //    }

    //    return string.Empty;
    //}

    void IDisposable.Dispose( )
    {

    }

    public Stream? ReadHtmlStreamFromUrl( string url ) => throw new NotImplementedException( );
}
