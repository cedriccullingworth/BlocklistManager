using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

namespace BlocklistManager.Classes;

internal sealed class DataTranslatorText : IDataTranslator
{
    /// <summary>
    /// Convert downloaded text to a List of CandidateEntry
    /// </summary>
    /// <param name="site">The download site's RemoteSite model</param>
    /// <param name="data">Simply the raw text content that was downloaded</param>
    /// <returns>The standardised structured list built fro the file contents</returns>
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data, string fileName )
    {
        return site.FileTypeID switch
        {
            8 => TranslateAlien( site, data, fileName ),
            _ => TranslateSingleColumn( site, data, fileName )
        };
    }

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new NotImplementedException( );
    }

    private static List<CandidateEntry> TranslateAlien( RemoteSite site, string textData, string fileName )
    {
        string[] textLines = [];
        if ( textData.Contains( Environment.NewLine ) )
        {
            textLines = textData.Split( Environment.NewLine );
        }
        else
        {
            textLines = textData.Split( '\n' );
        }

        // Concat doesn't work
        var allText = textLines.Where( w => !w.StartsWith( '#' ) )
                             .Where( w => !w.StartsWith( ';' ) )
                             .Where( w => !string.IsNullOrEmpty( w ) )
                             .Select( s => s.Replace( '#', ',' ) )
                             .ToArray( )
                             .Select( s => s.Split( ',' ) )
                             .ToList( );

        //var allText = textLines.Select( s => s.Split( ',' ) )
        //                                    .ToList( );

        return allText.Select( s => new CandidateEntry( site.Name, fileName, s[ 0 ].Trim( ), null, null, []/*, [], FirewallProtocol.Any*/ ) )
                      .ToList( );
    }

    private static List<CandidateEntry> TranslateSingleColumn( RemoteSite site, string textData, string fileName )
    {
        List<CandidateEntry> remoteData = [];
        string[] textLines = [];
        if ( textData.Contains( Environment.NewLine ) )
        {
            textLines = textData.Split( Environment.NewLine );
        }
        else
        {
            textLines = textData.Split( '\n' );
        }

        textLines = textLines.Where( w => !w.StartsWith( '#' ) )
                             .Where( w => !w.StartsWith( ';' ) )
                             .Where( w => !string.IsNullOrEmpty( w ) )
                             .Select( s => s.IndexOf( '\t' ) > -1 ? s[ ..s.IndexOf( '\t' ) ] : s ) // Specifically so that the 2nd column of Miroslav Stampar data can be ignored
                             .ToArray( );

        if ( textLines is not null && textLines.Length > 0 )
        {
            AddSingleColumnEntries( site, fileName, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv4 );
            AddSingleColumnEntries( site, fileName, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv6 );
        }

        return remoteData;
    }

    private static void AddSingleColumnEntries( RemoteSite site, string fileName, ref List<CandidateEntry> remoteData, List<string> allText, Maintain.IPAddressType addressType )
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        //char ipDelimiter = '.';
        //if ( addressType == Maintain.IPAddressType.IPv6 )
        //    ipDelimiter = ':';

        var unsorted = allText.Select( s => new CandidateEntry
                                                                            (
                                                                                site.Name,
                                                                                fileName,
                                                                                ( s.Contains( '/' ) ? s[ ..s.IndexOf( '/' ) ] : s ).Trim( ),
                                                                                ( s.Contains( '/' )
                                                                                    ? Convert.ToInt32( s[ ( s.IndexOf( '/' ) + 1 ).. ], CultureInfo.InvariantCulture )
                                                                                    : null ),
                                                                                null,
                                                                                []// ,
                                                                                /* We can stop carrying ports */ // [], // s.Contains( '/' ) ? [ Convert.ToUInt16( s[ ( s.IndexOf( '/' ) + 1 ).. ], culture ) ] : [],
                                                                                /* We can stop carrying protocol */ // FirewallProtocol.Any
                                                                            ) )
                                                     .Where( w => w.AddressType == addressType );

        if ( addressType == Maintain.IPAddressType.IPv4 )
        {
            remoteData.AddRange( unsorted );
        }
        else
        {
            remoteData.AddRange( unsorted.ToList( ) );
        }
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
    // ~TextDataTranslator()
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

    List<CandidateEntry> IDataTranslator.TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new NotImplementedException( );
    }
}
