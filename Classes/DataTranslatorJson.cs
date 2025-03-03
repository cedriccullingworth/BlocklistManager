using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

namespace BlocklistManager.Classes;

public sealed class DataTranslatorJson : IDataTranslator
{
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data, string fileName )
    {
        return site.Name switch
        {
            "Feodo" => TranslateFeodo( site, JsonDocument.Parse( data ), fileName ),
            _ => []
        };
    }

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new System.NotImplementedException( );
    }

    private static List<CandidateEntry> TranslateFeodo( RemoteSite site, JsonDocument doc, string fileName )
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        List<FeodoEntry> processed = [];
        var jsonArray = doc.RootElement
                                    .EnumerateArray( );

        foreach ( var item in jsonArray )
        {
            // Convert the JsonElement to a string for processing ... sad, but at least there's more control.
            var itemString = item.ToString( );
            FeodoEntry feodoEntry = new( );

            try
            {
                // Convert the itemString to a string array of property entries
                var props = itemString.Replace( "{", "" )
                                        .Replace( "}", "" )
                                        .Split( ",\n" );
                foreach ( var prop in props )
                {
                    string[] split = prop.Replace( "\n", "" )
                                         .Replace( "\"", "" )
                                         .Split( ':' )
                                         .Select( s => s.Trim( ) )
                                         .ToArray( );

                    KeyValuePair<string, string> detail = new( split[ 0 ], split[ 1 ] );
                    // process each component separately as a KeyValuePair ... sad, but at least there's more control.
                    switch ( detail.Key )
                    {
                        case "ip_address":
                            {
                                feodoEntry.ip_address = detail.Value;
                                break;
                            }
                        case "port":
                            {
                                ushort[] ushorts = !string.IsNullOrEmpty( detail.Value )
                                                    ? detail.Value
                                                            .Split( ',', StringSplitOptions.TrimEntries )
                                                            .Select( s => Convert.ToUInt16( s, culture ) )
                                                            .ToArray( )
                                                    : [ 0 ];
                                feodoEntry.ports = ushorts;
                                break;
                            }
                        case "status":
                            {
                                feodoEntry.status = detail.Value;
                                break;
                            }
                        case "hostname":
                            {
                                feodoEntry.hostname = detail.Value;
                                break;
                            }
                        case "as_number":
                            {
                                feodoEntry.as_number = Convert.ToInt32( detail.Value, culture );
                                break;
                            }
                        case "as_name":
                            {
                                feodoEntry.as_name = detail.Value;
                                break;
                            }
                        case "country":
                            {
                                feodoEntry.country = detail.Value;
                                break;
                            }
                        case "first_seen":
                            {
                                feodoEntry.first_seen = detail.Value;
                                break;
                            }
                        case "last_online":
                            {
                                feodoEntry.last_online = detail.Value;
                                break;
                            }
                        case "malware":
                            {
                                feodoEntry.malware = detail.Value;
                                break;
                            }
                    }
                }

                processed.Add( feodoEntry );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message );
                Maintain.StatusMessage( "TranslateFeodo", ex.Message );
            }
        }

        // Removed: // Number = Convert.ToString(s.as_number),
        return processed.Select( s => new CandidateEntry( site.Name, fileName, s.ip_address, null, null, []/*, s.ports, FirewallProtocol.Any*/ ) )
                        .ToList( );
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
    // ~JsonDataTranslator()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        System.GC.SuppressFinalize( this );
    }

    List<CandidateEntry> IDataTranslator.TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new System.NotImplementedException( );
    }
}
