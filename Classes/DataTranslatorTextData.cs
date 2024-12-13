using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

namespace BlocklistManager.Classes;

internal class DataTranslatorTextData : IDataTranslator
{
    /// <summary>
    /// Convert downloaded text to a List of CandidateEntry
    /// </summary>
    /// <param name="site">The download site's RemoteSite model</param>
    /// <param name="data">Simply the raw text content that was downloaded</param>
    /// <returns>The standardised structured list built fro the file contents</returns>
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data )
    {
        return site.FileTypeID switch
        {
            8 => TranslateAlien( site, data ),
            _ => TranslateSingleColumn( site, data )
        };
    }

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream ) => throw new NotImplementedException( );

    private List<CandidateEntry> TranslateAlien( RemoteSite site, string textData )
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
        textLines = textLines.Where( w => !w.StartsWith( '#' ) )
                             .Where( w => !w.StartsWith( ';' ) )
                             .Where( w => !string.IsNullOrEmpty( w ) )
                             .ToArray( );

        if ( textLines.Length > 0 && textLines.FirstOrDefault( f => f.IndexOf( '#' ) > 0 ) is not null )
        {
            textLines = textLines.Select( s => s.Replace( '#', ',' ) )
                                    .ToArray( );
        }

        var allText = textLines.Select( s => s.Split( ',' ) )
                                            .ToList( );

        return [ .. allText.Select( s => new CandidateEntry( )
                            {
                                IPAddress   = s[ 0 ].Trim( ),
                                Country     = s[ 1 ].LastIndexOf( ' ' ) > 0 ? s[ 1 ].TrimEnd( )[ s[ 1 ].LastIndexOf( ' ' ).. ].Trim( ) : null,
                                Description = site.Name,
                                Malware     = s[ 1 ].TrimEnd( ), // s[1].LastIndexOf( ' ' ) > 0 ? s[1].TrimEnd( ).Substring( 0, s[1].LastIndexOf( ' ' )).Replace(" ", string.Empty) : null,
                                                             //Name = $"@(imported) {site.Name}_Blocklist",
                                Name = site.Name,
                                AddressType = Maintain.InternetAddressType( s[ 0 ].Trim() ),
                            } )
                            //.OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
                            //.ThenBy ( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
                            //.ThenBy ( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
                            //.ThenBy ( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) ) 
                            ];
    }

    private List<CandidateEntry> TranslateSingleColumn( RemoteSite site, string textData )
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
                             .Select( s => s.IndexOf( '\t' ) > -1 ? s[..s.IndexOf('\t')] : s ) // Specifically so that the 2nd column of Miroslav Stampar data can be ignored
                             .ToArray( );

        if ( textLines is not null && textLines.Count( ) > 0 )
        {
            AddSingleColumnEntries( site, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv4 );
            AddSingleColumnEntries( site, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv6 );
        }

        return remoteData;
    }

    private void AddSingleColumnEntries( RemoteSite site, ref List<CandidateEntry> remoteData, List<string> allText, Maintain.IPAddressType addressType )
    {
        char ipDelimiter = '.';
        if ( addressType == Maintain.IPAddressType.IPv6 )
            ipDelimiter = ':';

        var unsorted = allText.Select( s => new CandidateEntry( )
        {
            IPAddress = ( s.Contains( '/' ) ? s[ ..s.IndexOf( '/' ) ] : s ).Trim( ),
            // Catered for port numbers provided in Emerging Threats...but perhaps better to leave at 'Any'
            Ports = s.Contains( '/' ) ? [ Convert.ToUInt16( s[ ( s.IndexOf( '/' ) + 1 ).. ] ) ] : [],
            Country = "-",
            Description = site.Name,
            Malware = "-",
            Name = site.Name,
            AddressType = Maintain.InternetAddressType( ( s.Contains( '/' ) ? s[ ..s.IndexOf( '/' ) ] : s ).Trim( ) ),
        } )
            .Where( w => w.AddressType == addressType );

        if ( addressType == Maintain.IPAddressType.IPv4 )
        {
            remoteData.AddRange( [ .. unsorted
                //.OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 0 ] ) )
                //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 1 ] ) )
                //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 2 ] ) )
                //.ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 3 ] ) ) 
                ] );
        }
        else
        {
            remoteData.AddRange( unsorted.ToList( ) );
        }
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
}
