using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using WindowsFirewallHelper.Addresses;
using WindowsFirewallHelper;

namespace BlocklistManager.Classes;

internal class DataTranslatorDelimited : IDataTranslator
{
    public List<CandidateEntry> TranslateFileData( RemoteSite site, string data )
    { 
        List<string> lineData = Maintain.TextToStringList( data );
        lineData = lineData.Where( w => !w.StartsWith( '#' ) )
                           .Where( w => !w.StartsWith( ';' ) )
                           .Where( w => !string.IsNullOrEmpty( w ) )
                           .ToList( );

        return site.FileType.Name switch
        {
            // Only James Brine at this stage, will need splitting out when another CSV format arrives
            "CSV" => lineData.Select( s => s.Split( ',', StringSplitOptions.TrimEntries ) )
                             .Where( w => Maintain.InternetAddressType( w[ 0 ] ) != Maintain.IPAddressType.Invalid )
                             .Select( s => new CandidateEntry( )
                             {
                                 IPAddress = s[ 0 ],
                                 Name = site.Name,
                                 Description = site.Name,
                                 Ports = [],
                                 Protocol = FirewallProtocol.Any,
                                 AddressType = Maintain.InternetAddressType( s[ 0 ] ),
                                 Country = "-",
                                 Malware = s.Length > 1 ? s[ 1 ] : "-",
                                 Status = "-",
                             } )
                             .ToList( ),
            "TAB" => TranslateTabDelimited( site, lineData ),
            _ => []
        };
    }

    public List<CandidateEntry> TranslateDataStream( RemoteSite site, Stream dataStream ) => throw new NotImplementedException( );

    private List<CandidateEntry> TranslateTabDelimited( RemoteSite site, List<string> dataLines )
    {
        return site.Name switch
        {
            "ScriptzTeam" => ReadDelimitedDataScriptzTeam( '\t', dataLines, site ),
            "Internet Storm Center DShield" => ReadDelimitedDataDShield( '\t', dataLines, site ),
            _  => []
        };
    }

    private List<CandidateEntry> ReadDelimitedDataScriptzTeam( char delimiter, List<string> allText, RemoteSite site ) =>
        [ .. allText.Select( s => s.Split( delimiter ) )
                                    .Select( s => new CandidateEntry( )
                                    {
                                        //                                        Name = site.Name,
                                        //Name = $"@(imported) {site.Name}_Blocklist",
                                        Name = site.Name,
                                        IPAddress = s[ 0 ],
                                        Country = "-",
                                        Description = site.Name,
                                        AddressType = Maintain.InternetAddressType( s[ 0 ] ),
                                    } )
                                    .OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
                                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
                                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
                                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) ) ];

    private List<CandidateEntry> ReadDelimitedDataDShield( char delimiter, List<string> allText, RemoteSite site )
    {
        char period = '.';
        List<CandidateEntry> candidates = [];
        var entries = allText.Select( s => s.Split( delimiter ) )
                .Select( s => new DShieldEntry( )
                {
                    rangeStart = s[ 0 ],
                    rangeEnd = s[ 1 ],
                    Subnet = (ushort)( string.IsNullOrEmpty( s[ 2 ] ) ? 0 : Convert.ToUInt16( s[ 2 ] ) ),
                    TargetsScanned = Convert.ToInt64( s[ 3 ] ),
                    NetworkName = s[ 4 ],
                    Country = s[ 5 ],
                    EmailAddress = s[ 6 ],
                } );

        int count = entries.Count( );
        foreach ( DShieldEntry entry in entries )
        {
            byte[] startBytes = entry.rangeStart.Split( period )
                                                .Select( s => (byte)Convert.ToInt32( s ) )
                                                .ToArray( ),
                   endBytes = entry.rangeEnd.Split( period )
                                            .Select( s => (byte)Convert.ToInt32( s ) )
                                            .ToArray( );
            SingleIP rangeStart = new( startBytes ), rangeEnd = new( endBytes );
            IPRange addressRange = new( rangeStart, rangeEnd );

            candidates.Add( new CandidateEntry( )
            {
                //IPAddress = newAddress,
                IPAddressRange = addressRange,
                //                Name = site.Name,
                //Name = $"@(imported) {site.Name}_Blocklist",
                Name = site.Name,
                Description = site.Name,
                Country = entry.Country,
                AddressType = Maintain.InternetAddressType( addressRange.StartAddress.ToString() ),
            } );
        }

        return candidates; /* [ .. candidates.OrderBy( o => Convert.ToInt32( o.IPAddressRange!.StartAddress.ToString( ).Split( period )[ 0 ] ) )
                .ThenBy( o => Convert.ToInt32( o.IPAddressRange!.StartAddress.ToString( ).Split( period )[ 1 ] ) )
                .ThenBy( o => Convert.ToInt32( o.IPAddressRange!.StartAddress.ToString( ).Split( period )[ 2 ] ) )
                .ThenBy( o => Convert.ToInt32( o.IPAddressRange!.StartAddress.ToString( ).Split( period )[ 3 ] ) ) ];*/
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
