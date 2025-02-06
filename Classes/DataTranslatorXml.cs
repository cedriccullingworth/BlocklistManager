using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using SBS.Utilities;

using WindowsFirewallHelper;

namespace BlocklistManager.Classes;

internal sealed class DataTranslatorXml : IDataTranslator //, IDisposable
{
    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream )
    {
        return site.ID switch
        {
            11 => TranslateShodan( site, dataStream ),
            17 => TranslateCyberCrimeTracker( site, dataStream ),
            _ => []
        };

    }

#pragma warning disable CA1822 // Mark members as static
    private List<CandidateEntry> TranslateCyberCrimeTracker( RemoteSite site, Stream dataStream, string logFilePath = "" )
#pragma warning restore CA1822 // Mark members as static
    {
        try
        {
            XmlReader reader = new XmlTextReader( dataStream );
            XmlSerializer serializer = new( typeof( rss ) );
            var rssData = serializer.Deserialize( reader );
            if ( rssData is not null )
            {
                rss data = (rss)rssData;
                IEnumerable<string> addressesOnly = data.channel[ 0 ]
                                                        .item
                                                        .Select( s => System.Net.Dns.GetHostAddresses( s.title.Contains( '/' ) ? s.title[ ..s.title.IndexOf( '/' ) ] : s.title )
                                                                                                .First( )
                                                                                                .ToString( ) )
                                                        .Distinct( );

                // Remove duplicate IP addresses (as found in https://cybercrime-tracker.net/rss.xml
                return addressesOnly.Select( s => new CandidateEntry( site.Name, s.Trim( ), null, [], [], FirewallProtocol.Any ) )
                                    .ToList( );
            }
        }
        catch ( Exception ex )
        {
            if ( logFilePath != "" )
                Logger.Log( "TranslateShodan", ex );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "TranslateShodan", ex ) );
        }

        return [];
    }

#pragma warning disable CA1822 // Mark members as static
    private List<CandidateEntry> TranslateShodan( RemoteSite site, Stream dataStream, string logFilePath = "" )
#pragma warning restore CA1822 // Mark members as static
    {
        try
        {
            XmlReader reader = new XmlTextReader( dataStream );
            XmlSerializer serializer = new( typeof( threatlist ) );
            threatlist threats = (threatlist)serializer.Deserialize( reader )!;
            //IEnumerable<threatlistShodan> typed = threats.shodan!; //.Select( s => s );

            return threats.shodan!.Select( s => new CandidateEntry( site.Name, s.ipv4, null, [], [], FirewallProtocol.Any ) )
                          .ToList( );
        }
        catch ( Exception ex )
        {
            if ( logFilePath != "" )
                Logger.Log( "TranslateShodan", ex );
            else
                MessageBox.Show( StringUtilities.ExceptionMessage( "TranslateShodan", ex ) );
        }

        return [];
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
    // ~XmlDataTranslator()
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

    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data )
    {
        throw new NotImplementedException( );
    }

    List<CandidateEntry> IDataTranslator.TranslateDataStream( RemoteSite site, Stream dataStream )
    {
        throw new System.NotImplementedException( );
    }
}
