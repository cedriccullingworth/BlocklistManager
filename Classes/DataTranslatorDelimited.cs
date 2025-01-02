using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

internal sealed class DataTranslatorDelimited : IDataTranslator
{
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data )
    {
        List<string> lineData = Maintain.TextToStringList( data );
        lineData = lineData.Where( w => !w.StartsWith( '#' ) )
                           .Where( w => !w.StartsWith( ';' ) )
                           .Where( w => !string.IsNullOrEmpty( w ) )
                           .ToList( );

        return site.FileType!.Name switch
        {
            // TODO: Separate James Brine and MyIP, test both 
            // Only James Brine at this stage, will need splitting out when another CSV format arrives
            "CSV" => /*site.ID == 24 ? */TranslateCommaDelimited( site, lineData ), // : TranslateMyIP( site, lineData ),
            "TAB" => TranslateTabDelimited( site, lineData ),
            _ => throw new InvalidOperationException( )
        };
    }

    private static List<CandidateEntry> TranslateCommaDelimited( RemoteSite site, List<string> lineData ) => lineData
                                 .Select( s => s.Replace( "# ", "," ).Replace( "#", string.Empty ) )
                                 .Select( s => s.Split( ',', StringSplitOptions.TrimEntries ) )
                                 .Where( w => Maintain.InternetAddressType( w[ 0 ] ) != Maintain.IPAddressType.Invalid )
                                 .Select( s => new CandidateEntry( site.Name, s[ 0 ], null, [], [], FirewallProtocol.Any ) )
                                 .ToList( );

    //private static List<CandidateEntry> TranslateMyIP( RemoteSite site, List<string> lineData ) => lineData.Take( 10 )
    //                             .Select( s => s.Replace( "# ", "," ).Replace( "#", string.Empty ) )
    //                             .Select( s => s.Split( ",", StringSplitOptions.TrimEntries ) )
    //                             .Where( w => Maintain.InternetAddressType( w[ 0 ] ) != Maintain.IPAddressType.Invalid )
    //                             .Select( s => new CandidateEntry( site.Name, s[ 0 ], null, [], [], FirewallProtocol.Any ) )
    //                             .ToList( );

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream ) => throw new NotImplementedException( );

    private static List<CandidateEntry> TranslateTabDelimited( RemoteSite site, List<string> dataLines )
    {
        return site.Name switch
        {
            "ScriptzTeam" => ReadDelimitedDataScriptzTeam( '\t', dataLines, site ),
            "Internet Storm Center DShield" => ReadDelimitedDataDShield( '\t', dataLines, site ),
            _ => []
        };
    }

    private static List<CandidateEntry> ReadDelimitedDataScriptzTeam( char delimiter, List<string> allText, RemoteSite site ) =>
        allText.Select( s => s.Split( delimiter ) )
                                    .Select( s => new CandidateEntry( site.Name, s[ 0 ], null, [], [], FirewallProtocol.Any ) )
                                    .ToList( );

    private static List<CandidateEntry> ReadDelimitedDataDShield( char delimiter, List<string> allText, RemoteSite site )
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

            candidates.Add( new CandidateEntry( site.Name, null, addressRange, [], [], FirewallProtocol.Any ) );
        }

        return candidates;
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
    // ~DelimitedDataTranslator()
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
