using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

/// <summary>
/// Tranlator for delimited data downloads
/// </summary>
internal sealed class DataTranslatorDelimited : IDataTranslator
{
    /// <summary>
    /// The public method for translating delimited downloaded data
    /// </summary>
    /// <param name="site">The downlod site's details</param>
    /// <param name="data">A list of the lines in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data, string fileName )
    {
        List<string> lineData = Maintain.TextToStringList( data );
        lineData = lineData.Where( w => !w.StartsWith( '#' ) )
                           .Where( w => !w.StartsWith( ';' ) )
                           .Where( w => !string.IsNullOrEmpty( w ) )
                           //                       .Where( x => x.Contains( "207.63.218" ) )
                           .ToList( );

        return site.FileType!.Name switch
        {
            // Only James Brine at this stage, will probably need some separation when another CSV format comes along
            "CSV" => /*site.ID == 24 ? */TranslateCommaDelimited( site, lineData, fileName ), // : TranslateMyIP( site, lineData ),
            "TAB" => TranslateTabDelimited( site, lineData, fileName ),
            _ => throw new InvalidOperationException( )
        };
    }

    /// <summary>
    /// Translate a comma-delimited file
    /// </summary>
    /// <param name="site">The downlod site's details</param>
    /// <param name="lineData">A list of the lines in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> TranslateCommaDelimited( RemoteSite site, List<string> lineData, string fileName )
    {
        List<CandidateEntry> unsorted = lineData
                    .Select( s => s.Replace( "# ", "," ).Replace( "#", string.Empty ) )
                    .Select( s => s.Split( ',', StringSplitOptions.TrimEntries ) )
                    //                    .Where( w => Maintain.InternetAddressType( w[ 0 ] ) != IPAddressType.Invalid )
                    .Select( s => new CandidateEntry( site.Name, fileName, s[ 0 ], null, null, []/*, [], FirewallProtocol.Any*/ ) )
                    .ToList( );
        Maintain.ValidateIPAddressesAndRanges( ref unsorted );
        return unsorted;
    }

    //private static List<CandidateEntry> TranslateMyIP( RemoteSite site, List<string> lineData ) => lineData.Take( 10 )
    //                             .Select( s => s.Replace( "# ", "," ).Replace( "#", string.Empty ) )
    //                             .Select( s => s.Split( ",", StringSplitOptions.TrimEntries ) )
    //                             .Where( w => Maintain.InternetAddressType( w[ 0 ] ) != Maintain.IPAddressType.Invalid )
    //                             .Select( s => new CandidateEntry( site.Name, s[ 0 ], null, [], [], FirewallProtocol.Any ) )
    //                             .ToList( );

    /// <summary>
    /// Not in use
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream, string fileName )
    {
        throw new NotImplementedException( );
    }

    /// <summary>
    /// Translate a tab-delimited file
    /// </summary>
    /// <param name="site">The download site's details</param>
    /// <param name="dataLines">An array of the lines in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> TranslateTabDelimited( RemoteSite site, List<string> dataLines, string fileName )
    {
        return site.Name switch
        {
            "ScriptzTeam" => ReadDelimitedDataScriptzTeam( '\t', dataLines, site, fileName ),
            "Internet Storm Center DShield" => ReadDelimitedDataDShield( '\t', dataLines, site, fileName ),
            _ => []
        };
    }

    /// <summary>
    /// Translate a Scriptzteam file
    /// </summary>
    /// <param name="site">The download site's details</param>
    /// <param name="allText">A list of the lines in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> ReadDelimitedDataScriptzTeam( char delimiter, List<string> allText, RemoteSite site, string fileName )
    {
        List<CandidateEntry> unsorted = allText.Select( s => s.Split( delimiter ) )
                                    .Select( s => new CandidateEntry( site.Name, fileName, s[ 0 ], null, null, []/*, [], FirewallProtocol.Any*/ ) )
                                    .ToList( );
        Maintain.ValidateIPAddressesAndRanges( ref unsorted );
        return unsorted;
    }

    /// <summary>
    /// Translate an Internet Storm Center DShield file
    /// </summary>
    /// <param name="site">The download site's details</param>
    /// <param name="allText">A list of the lines in the file</param>
    /// <param name="fileName">The file name</param>
    /// <returns>The data translated from the download</returns>
    private static List<CandidateEntry> ReadDelimitedDataDShield( char delimiter, List<string> allText, RemoteSite site, string fileName )
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        char period = '.';
        List<CandidateEntry> candidates = [];
        var entries = allText.Select( s => s.Split( delimiter ) )
                .Select( s => new DShieldEntry( )
                {
                    rangeStart = s[ 0 ],
                    rangeEnd = s[ 1 ],
                    Subnet = (ushort)( string.IsNullOrEmpty( s[ 2 ] ) ? 0 : Convert.ToUInt16( s[ 2 ], culture ) ),
                    TargetsScanned = Convert.ToInt64( s[ 3 ], culture ),
                    NetworkName = s[ 4 ],
                    Country = s[ 5 ],
                    EmailAddress = s[ 6 ],
                } );

        int count = entries.Count( );
        foreach ( DShieldEntry entry in entries )
        {
            byte[] startBytes = entry.rangeStart.Split( period )
                                                .Select( s => (byte)Convert.ToInt32( s, culture ) )
                                                .ToArray( ),
                   endBytes = entry.rangeEnd.Split( period )
                                            .Select( s => (byte)Convert.ToInt32( s, culture ) )
                                            .ToArray( );
            SingleIP rangeStart = new( startBytes ), rangeEnd = new( endBytes );
            IPRange addressRange = new( rangeStart, rangeEnd );

            candidates.Add( new CandidateEntry( site.Name, fileName, null, null, addressRange, []/*, [], FirewallProtocol.Any*/ ) );
        }

        Maintain.ValidateIPAddressesAndRanges( ref candidates );
        return candidates;
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
    // ~DelimitedDataTranslator()
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
}
