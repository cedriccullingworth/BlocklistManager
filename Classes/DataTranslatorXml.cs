using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

namespace BlocklistManager.Classes;

/// <summary>
/// Tranlator for XML downloads
/// </summary>
internal sealed class DataTranslatorXml : IDataTranslator //, IDisposable
{
    /// <summary>
    /// The public method for translating XML downloaded data streams
    /// </summary>
    /// <param name="site">The downlod site's details</param>
    /// <param name="dataStream">A stream of the data in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
#pragma warning disable CA1822 // Mark members as static
    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        return site.ID switch
        {
            11 => TranslateShodan( site, dataStream, fileName ),
            17 => TranslateCyberCrimeTracker( site, dataStream, fileName ),
            _ => []
        };

    }
#pragma warning disable CA1822 // Mark members as static

    /// <summary>
    /// Translate a CyberCrime-Tracker file
    /// </summary>
    /// <param name="site">The downlod site's details</param>
    /// <param name="dataStream">The data stream to tranlate</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> TranslateCyberCrimeTracker( RemoteSite site, Stream dataStream, string fileName, string logFilePath = "" )
    {
        List<CandidateEntry> unsorted = [];
        try
        {
            XmlReader reader = new XmlTextReader( dataStream );
            XmlSerializer serializer = new( typeof( rss ) );
            var rssData = serializer.Deserialize( reader );
            if ( rssData is not null )
            {
                rss data = (rss)rssData;
                string[] addressesOnly = data.channel[ 0 ]
                                                        .item
                                                        .Select( s => System.Net.Dns.GetHostAddresses( s.title.Contains( '/' ) ? s.title[ ..s.title.IndexOf( '/' ) ] : s.title )
                                                                                                .First( )
                                                                                                .ToString( ) )
                                                        .Distinct( )
                                                        .ToArray( );

                // Remove duplicate IP addresses (as found in https://cybercrime-tracker.net/rss.xml
                unsorted = addressesOnly.Select( s => new CandidateEntry( site.Name, fileName, s.Trim( ), null, null, [] ) ).ToList( );
                Maintain.ValidateIPAddressesAndRanges( ref unsorted );
                return unsorted;
            }
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "TranslateCyberCrimeTracker", ex.Message );
        }

        return [];
    }

    /// <summary>
    /// Translate an Internet Storm Center Shodan file
    /// </summary>
    /// <param name="site">The downlod site's details</param>
    /// <param name="dataStream">The data stream to tranlate</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> TranslateShodan( RemoteSite site, Stream dataStream, string fileName, string logFilePath = "" )
    {
        try
        {
            XmlReader reader = new XmlTextReader( dataStream );
            XmlSerializer serializer = new( typeof( threatlist ) );
            threatlist threats = (threatlist)serializer.Deserialize( reader )!;
            //List<threatlistShodan> typed = threats.shodan!; //.Select( s => s );

            List<CandidateEntry> unsorted = threats.shodan!.Select( s => new CandidateEntry( site.Name, fileName, s.ipv4, null, null, []/*, [], FirewallProtocol.Any*/ ) )
                                                           .ToList( );
            Maintain.ValidateIPAddressesAndRanges( ref unsorted );
            return unsorted;
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "TranslateShodan", ex.Message );
        }

        return [];
    }

    /// <summary>
    /// Default disposing property
    /// </summary>
    private bool disposedValue;

    /// <summary>
    /// Default disposing method
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
    // ~XmlDataTranslator()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    /// <summary>
    /// Default disposing method
    /// </summary>
    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }

    /// <summary>
    /// Not in use
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data, string fileName )
    {
        throw new NotImplementedException( );
    }

    /// <summary>
    /// Not in use
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    List<CandidateEntry> IDataTranslator.TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new System.NotImplementedException( );
    }

    List<CandidateEntry> IDataTranslator.TranslateFileData( RemoteSite site, string data, string fileName ) => TranslateFileData( site, data, fileName );
}
