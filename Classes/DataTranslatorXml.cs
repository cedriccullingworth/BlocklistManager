using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using BlockListManager;

using SBS.Utilities;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

public class DataTranslatorXml : IDataTranslator, IDisposable
{
    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream )
    {
        List<CandidateEntry> results = [];

        return site.ID switch
        {
            11 => this.TranslateShodan( site, dataStream ),
            17 => this.TranslateCyberCrimeTracker( site, dataStream ),
            _ => []
        };

    }

    private List<CandidateEntry> TranslateCyberCrimeTracker( RemoteSite site, Stream dataStream, string logFilePath = "" )
    {
        try
        { 
            XmlSerializer serializer = new XmlSerializer( typeof( rss ) );
            var rssData = serializer.Deserialize( dataStream );
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
                return addressesOnly.Select( s => new CandidateEntry( )
                {
                    IPAddress = s,
                    Name = site.Name,
                    Description = site.Name,
                    Country = "-",
                    Malware = "-",
                    Ports = [],
                    Protocol = FirewallProtocol.Any,
                    Status = "online",
                    AddressType = Maintain.InternetAddressType( s ),
                } )
                //.OrderBy( o => o.Sort[ 0 ] )
                //.ThenBy( o => o.Sort[ 1 ] )
                //.ThenBy( o => o.Sort[ 2 ] )
                //.ThenBy( o => o.Sort[ 3 ] )
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

    private List<CandidateEntry> TranslateShodan( RemoteSite site, Stream dataStream, string logFilePath = "" )
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer( typeof( threatlist ) );
            threatlist threats = (threatlist)serializer.Deserialize( dataStream )!;
            IEnumerable<threatlistShodan> typed = threats.shodan!.Select( s => s );

            var processing = typed.Select( s => new CandidateEntry( )
            {
                IPAddress = s.ipv4,
                //                            Name = site.Name,
                //Name = $"@(imported) {site.Name}_Blocklist",
                Name = site.Name,
                Description = site.Name,
                Malware = "-",
                AddressType = Maintain.InternetAddressType( s.ipv4 ),
            } );
            //.OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
            //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
            //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
            //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) );

            return processing.ToList( );
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

    protected virtual void Dispose( bool disposing )
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

    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data ) => throw new NotImplementedException( );
}
