using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Text.Json;

using System.Text.RegularExpressions;
using System.Windows.Forms;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

using SBS.Utilities;

using WindowsFirewallHelper.Addresses;

namespace BlocklistManager.Classes;

/// <summary>
/// Various utilities involved in downloading blocklists
/// No longer downloading Feodo as it's entries are included in other lists
/// </summary>
internal static partial class HttpHelper
{
    private const int TIMEOUT_SECONDS = 30;

    private static readonly string _appName = Assembly.GetEntryAssembly( )!.GetName( )!.Name!;

    internal static string ReadHtmlContentFromUrl( RemoteSite site, string url )
    {
        string extension = string.Empty;
        //using HtmlDataCollector collector = new HtmlDataCollector();
        using TextDataCollector collector = new( );
        return collector.ReadData( site, out extension!, url );

        //string html = string.Empty;
        //try
        //{
        //    HttpClient client = new( ) { BaseAddress = new Uri( url ) };
        //    var stream = client.GetStreamAsync( new Uri( url ) ).Result;

        //    using StreamReader reader = new( stream );
        //    html = reader.ReadToEnd( );
        //}
        //catch ( Exception ex )
        //{
        //    MessageBox.Show( ex.Message ); // TODO: Improve the message
        //}

        //return html;
    }

    //internal static Stream? ReadHtmlStreamFromUrl( string url )
    //{
    //    Stream? stream = null;
    //    string html = string.Empty;
    //    try
    //    {
    //        HttpClient client = new( ) { BaseAddress = new Uri( url ) };
    //        stream = client.GetStreamAsync( new Uri( url ) ).Result;
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return stream;
    //}

    /// <summary>
    /// UNTESTED
    /// </summary>
    /// <param name="site"></param>
    /// <param name="url"></param>
    /// <param name="fileExtension"></param>
    /// <returns></returns>
    //internal static string ReadZipFileContents( RemoteSite site, string url, out string fileExtension )
    //{
    //    fileExtension = ".txt";
    //    using TextDataCollector zipCollector = new ( );
    //    return zipCollector.ReadZipData( site, out fileExtension!, url );
    //}

    //internal static IList<CandidateEntry> PrepareEntriesFromUrl_Xml( RemoteSite source, string url, ref List<CandidateEntry> remoteData )
    //{
    //    remoteData = [];
    //    using TextDataCollector collector = new ();
    //    Stream? xmlStream = collector.ReadHtmlStreamFromUrl( url ) as Stream;
    //    if ( xmlStream is not null )
    //    {
    //        using IDataTranslator translator = new XmlDataTranslator( );
    //        // TODO: Step in to test the basics, then add the translation
    //        remoteData = translator.TranslateDataStream( source, xmlStream );
    //    }

    //    return remoteData;
    //}

    //internal static IList<CandidateEntry> PrepareEntriesFromUrl_Json( RemoteSite source, string url, ref List<CandidateEntry> remoteData )
    //{
    //    string json = string.Empty;

    //    try
    //    {
    //        using HttpClient client = new( ) { BaseAddress = new Uri( url ) };
    //        var stream = client.GetStreamAsync( new Uri( url ) ).Result;

    //        using ( StreamReader reader = new( stream ) )
    //        {
    //            json = reader.ReadToEnd( );
    //        }

    //        JsonDocument doc = JsonDocument.Parse( json );
    //        switch ( source.Name )
    //        {
    //            case "Feodo":
    //                {

    //                    IList<FeodoEntry>? feodoData = TranslateFeodo( doc );

    //                    remoteData.AddRange( [ .. feodoData.Select( s => new CandidateEntry( )
    //                    {
    //                        IPAddress = s.ip_address,
    //                        Ports = s.ports,
    //                        Status = s.status,
    //                        // Name = s.as_name,
    //                        Name = $"@(imported) {s.as_name}_Blocklist",
    //                        // Number = Convert.ToString(s.as_number),
    //                        Description = s.hostname,
    //                        Country = s.country,
    //                        Malware = s.malware,
    //                    } )
    //                    .OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
    //                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
    //                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
    //                    .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) ) ] );
    //                    break;
    //                }
    //        }
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return remoteData.Where( w => !string.IsNullOrEmpty( w.IPAddress ) )
    //                     .ToList( );
    //}

    //internal static IList<CandidateEntry> PrepareEntriesFromUrl_Delimited( RemoteSite source, string url, char delimiter, ref List<CandidateEntry> remoteData )
    //{
    //    string text = string.Empty;
    //    string[] textLines = [];

    //    try
    //    {
    //        // First try with HttpClient
    //        HttpClient client = new( )
    //        {
    //            BaseAddress = new Uri( url ),
    //            Timeout = TimeSpan.FromSeconds( 30 )
    //        };

    //        try
    //        {
    //            var stream = client.GetStreamAsync( new Uri( url ) ).Result;

    //            using StreamReader reader = new( stream );
    //            text = reader.ReadToEnd( );
    //        }
    //        catch
    //        {
    //            // Move this to it's own method, also used by DownloadText
    //            // Try to open the data directly
    //            client.CancelPendingRequests( );
    //            try
    //            {
    //                // Using WebRequest because HttpClient failed
    //                // Is this ever used?
    //                HttpWebRequest req = (HttpWebRequest)WebRequest.Create( url );
    //                req.UserAgent = "Free firewall update utility";
    //                req.AuthenticationLevel = AuthenticationLevel.None;
    //                req.ContentType = "application/text";
    //                req.Date = DateTime.UtcNow;
    //                req.UseDefaultCredentials = true;
    //                req.Referer = "https://rodneylab.com/firewall-block-lists-compared/";
    //                WebResponse response = req.GetResponse( );
    //                Stream stream = response.GetResponseStream( );

    //                //                Stream stream = client.GetStreamAsync( new Uri( url ) ).Result;
    //                StreamReader reader = new( stream );
    //                text = reader.ReadToEnd( );
    //            }
    //            catch ( Exception ex )
    //            {
    //                MessageBox.Show( ex.Message );
    //            }
    //        }

    //        if ( text.Contains( Environment.NewLine ) )
    //        {
    //            textLines = text.Split( Environment.NewLine );
    //        }
    //        else
    //        {
    //            textLines = text.Split( '\n' );
    //        }

    //        textLines = textLines.Where( w => !w.StartsWith( '#' ) )
    //                                .Where( w => !string.IsNullOrEmpty( w ) )
    //                                .ToArray( );
    //        if ( textLines.FirstOrDefault( f => f.IndexOf( '#' ) > 0 ) is not null )
    //        {
    //            textLines = textLines.Select( s => s.Replace( '#', ',' ) )
    //                                    .ToArray( );
    //        };

    //        text = string.Join( Environment.NewLine, textLines );

    //        var allText = textLines.Select( s => s )
    //                                            .ToList( );

    //        if ( source.Name == "Internet Storm Center DShield" )
    //            remoteData.AddRange( ReadDelimitedDataDShield( delimiter, allText, source ) );
    //        else if ( source.Name == "ScriptzTeam" )
    //            remoteData.AddRange( ReadDelimitedDataScriptzTeam( delimiter, allText, source ) );
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return remoteData;
    //}

    //internal static IList<CandidateEntry> PrepareEntriesFromUrl_Xml( RemoteSite site, string url, ref List<CandidateEntry> remoteData )
    //{
    //    try
    //    {
    //        using HttpClient client = new( ) { BaseAddress = new Uri( url ) };
    //        using var stream = client.GetStreamAsync( new Uri( url ) ).Result;
    //        var serializer = new XmlSerializer( typeof( threatlist ) );
    //        try
    //        {
    //            threatlist threats = (threatlist)serializer.Deserialize( stream )!;
    //            //threatlist threats = test as threatlist;
    //            IEnumerable<threatlistShodan> typed = threats.shodan!.Select( s => s );
    //            var processing = typed.Select( s => new CandidateEntry( )
    //            {
    //                IPAddress = s.ipv4,
    //                //                            Name = site.Name,
    //                //Name = $"@(imported) {site.Name}_Blocklist",
    //                Name = site.Name,
    //                Description = site.Name,
    //                Malware = "-",
    //            } )
    //            .OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) );

    //            remoteData.AddRange( processing );
    //        }
    //        catch { }
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message );
    //    }
    //    return remoteData;
    //}

    /// <summary>
    /// No longer used, replaced by TextDataCollector & TextDataTranslator
    /// </summary>
    /// <returns>No longer used, replaced by TextDataCollector & TextDataTranslator</returns>
    //internal static IList<CandidateEntry> PrepareEntriesFromUrl_Text( RemoteSite site, string url, ref List<CandidateEntry> remoteData/* = new List<CandidateEntry>( )*/, string logFilePath = "" )
    //{
    //    string text = string.Empty;
    //    string[] textLines = [];

    //    try
    //    {
    //        // First try with HttpClient
    //        textLines = DownloadText( site, url, ref text, logFilePath ); // There could be issues with the latest changes

    //        if ( textLines.Length > 0 )
    //        {
    //            switch ( site.FileTypeID )
    //            {
    //                case 8:
    //                    {
    //                        var allText = textLines.Select( s => s.Split( ',' ) )
    //                                                            .ToList( );
    //                        // Concat doesn't work
    //                        remoteData.AddRange( [ .. allText.Select( s => new CandidateEntry( )
    //                        {
    //                            IPAddress = s[ 0 ].Trim( ),
    //                            Country = s[ 1 ].LastIndexOf( ' ' ) > 0 ? s[ 1 ].TrimEnd( )[ s[ 1 ].LastIndexOf( ' ' ).. ].Trim( ) : null,
    //                            Description = site.Name,
    //                            Malware = s[ 1 ].TrimEnd( ), // s[1].LastIndexOf( ' ' ) > 0 ? s[1].TrimEnd( ).Substring( 0, s[1].LastIndexOf( ' ' )).Replace(" ", string.Empty) : null,
    //                            //Name = $"@(imported) {site.Name}_Blocklist",
    //                            Name = site.Name,
    //                        } )
    //                        .OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( '.' )[ 0 ] ) )
    //                        .ThenBy( t =>  Convert.ToInt32( t.IPAddress!.Split( '.' )[ 1 ] ) )
    //                        .ThenBy( t =>  Convert.ToInt32( t.IPAddress!.Split( '.' )[ 2 ] ) )
    //                        .ThenBy( t =>  Convert.ToInt32( t.IPAddress!.Split( '.' )[ 3 ] ) ) ] );
    //                        break;
    //                    }
    //                /* 
    //            //case "Feodo":
    //            //    {
    //            //        var allText = textLines.Select( s => s.Split( ',' ) )
    //            //                                            .ToList( );
    //            //        var remote = textLines.Select( s => s.Split( "," ) );
    //            //        remoteData = remote.Select( s => new CandidateEntry( )
    //            //                                                        {

    //            //                                                        };
    //            //        break;
    //            //    }
    //                */
    //                default:
    //                    {
    //                        if ( textLines is not null && textLines.Count() > 0 )
    //                        {
    //                            AddSingleColumnEntries( site, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv4 );
    //                            AddSingleColumnEntries( site, ref remoteData, textLines.ToList( ), Maintain.IPAddressType.IPv6 );
    //                        }

    //                        break;
    //                    }

    //            }
    //        }
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return remoteData;
    //}

    //private static void AddSingleColumnEntries( RemoteSite site, ref List<CandidateEntry> remoteData, List<string> allText, Maintain.IPAddressType addressType )
    //{
    //    char ipDelimiter = '.';
    //    if ( addressType == Maintain.IPAddressType.IPv6 )
    //        ipDelimiter = ':';

    //    var unsorted = allText.Select( s => new CandidateEntry( )
    //    {
    //        IPAddress = ( s.Contains( '/' ) ? s[ ..s.IndexOf( '/' ) ] : s ).Trim( ),
    //        // Catered for port numbers provided in Emerging Threats...but perhaps better to leave at 'Any'
    //        Ports = s.Contains( '/' ) ? [ Convert.ToUInt16( s[ ( s.IndexOf( '/' ) + 1 ).. ] ) ] : [],
    //        Country = "-",
    //        Description = site.Name,
    //        Malware = "-",
    //        Name = site.Name,
    //        //Name = $"@(imported) {site.Name}_Blocklist",
    //    } )
    //        .Where( w => Maintain.InternetAddressType( w.IPAddress! ) == addressType );

    //    if ( addressType == Maintain.IPAddressType.IPv4 )
    //    {
    //        remoteData.AddRange( [ .. unsorted
    //            .OrderBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 0 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 1 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 2 ] ) )
    //            .ThenBy( t => Convert.ToInt32( t.IPAddress!.Split( ipDelimiter )[ 3 ] ) ) ] );
    //    }
    //    else
    //    {
    //        remoteData.AddRange( unsorted.ToList( ) );
    //    }
    //}

    /// <summary>
    /// No longer used, replaced by TextDataCollector & TextDataTranslator
    /// </summary>
    /// <returns>No longer used, replaced by TextDataCollector & TextDataTranslator</returns>
    //private static string[] DownloadText( RemoteSite site, string url, ref string textData, string logFilePath = "" )
    //{
    //    string extension = string.Empty;
    //    string[] textLines = [];
    //    textData = string.Empty;

    //    HttpClient client = new( )
    //    {
    //        BaseAddress = new Uri( url ),
    //        Timeout = TimeSpan.FromSeconds( TIMEOUT_SECONDS )
    //    };

    //    try
    //    {
    //        Stream? stream = client.GetStreamAsync( new Uri( url ) ).Result;
    //        using StreamReader reader = new( stream );
    //        textData = reader.ReadToEnd( );
    //    }
    //    catch ( Exception e )
    //    {
    //        if ( logFilePath != string.Empty )
    //        {
    //            Logger.LogPath = logFilePath;
    //            if ( e.InnerException is not null )
    //                Logger.Log( _appName, $"First download attempt from {site.Name} failed, {e.InnerException.Message}" );
    //            else
    //                Logger.Log( _appName, $"First download attempt from {site.Name} failed, {e.Message}" );
    //            Logger.Log( _appName, "Trying a different approach now" );
    //        }

    //        // Try to open the data directly
    //        client.CancelPendingRequests( );

    //        try
    //        {
    //            // Using WebRequest because HttpClient failed
    //            // Is this ever used? YES IT IS
    //            HttpWebRequest req = (HttpWebRequest)WebRequest.Create( url );
    //            req.UserAgent = "Free firewall update utility";
    //            req.AuthenticationLevel = AuthenticationLevel.None;
    //            req.ContentType = "application/text";
    //            req.Date = DateTime.UtcNow;
    //            req.UseDefaultCredentials = true;
    //            req.Referer = "https://rodneylab.com/firewall-block-lists-compared/";

    //            WebResponse response = req.GetResponse( );
    //            Stream stream = response.GetResponseStream( );
    //            using StreamReader reader = new( stream );
    //            textData = reader.ReadToEnd( );
    //        }
    //        catch ( Exception ex )
    //        {
    //            if ( logFilePath == string.Empty )
    //                MessageBox.Show( ex.Message );
    //            else
    //            {
    //                if ( e.InnerException is not null )
    //                    Logger.Log( _appName, $"Alternative download attempt from {site.Name} failed, {e.InnerException.Message}" );
    //                else
    //                    Logger.Log( _appName, $"Alternative download attempt from {site.Name} failed, {e.Message}" );
    //            }
    //        }
    //    }

    //    if ( textData.Contains( Environment.NewLine ) )
    //    {
    //        textLines = textData.Split( Environment.NewLine );
    //    }
    //    else
    //    {
    //        textLines = textData.Split( '\n' );
    //    }

    //    textLines = textLines.Where( w => !w.StartsWith( '#' ) )
    //                         .Where( w => !w.StartsWith( ';' ) )
    //                         .Where( w => !string.IsNullOrEmpty( w ) )
    //                         .ToArray( );
    //    if ( textLines.Length > 0 &&  textLines.FirstOrDefault( f => f.IndexOf( '#' ) > 0 ) is not null )
    //    {
    //        textLines = textLines.Select( s => s.Replace( '#', ',' ) )
    //                                .ToArray( );
    //    }

    //    //text = string.Join( Environment.NewLine, textLines );
    //    return textLines;
    //}

    //private static List<FeodoEntry> TranslateFeodo( JsonDocument doc )
    //{
    //    List<FeodoEntry> processed = [];
    //    var jsonArray = doc.RootElement
    //                                .EnumerateArray( );

    //    foreach ( var item in jsonArray )
    //    {
    //        // Convert the JsonElement to a string for processing ... sad, but at least there's more control.
    //        var itemString = item.ToString( );
    //        FeodoEntry feodoEntry = new( );

    //        try
    //        {
    //            // Convert the itemString to a string array of property entries
    //            var props = itemString.Replace( "{", "" )
    //                                    .Replace( "}", "" )
    //                                    .Split( ",\n" );
    //            foreach ( var prop in props )
    //            {
    //                string[] split = prop.Replace( "\n", "" )
    //                                     .Replace( "\"", "" )
    //                                     .Split( ':' )
    //                                     .Select( s => s.Trim( ) )
    //                                     .ToArray( );

    //                KeyValuePair<string, string> detail = new( split[ 0 ], split[ 1 ] );
    //                // process each component separately as a KeyValuePair ... sad, but at least there's more control.
    //                switch ( detail.Key )
    //                {
    //                    case "ip_address":
    //                        {
    //                            feodoEntry.ip_address = detail.Value;
    //                            break;
    //                        }
    //                    case "port":
    //                        {
    //                            ushort[] ushorts = !string.IsNullOrEmpty( detail.Value )
    //                                                ? detail.Value
    //                                                        .Split( ',', StringSplitOptions.TrimEntries )
    //                                                        .Select( s => Convert.ToUInt16( s ) )
    //                                                        .ToArray( )
    //                                                : [ 0 ];
    //                            feodoEntry.ports = ushorts;
    //                            break;
    //                        }
    //                    case "status":
    //                        {
    //                            feodoEntry.status = detail.Value;
    //                            break;
    //                        }
    //                    case "hostname":
    //                        {
    //                            feodoEntry.hostname = detail.Value;
    //                            break;
    //                        }
    //                    case "as_number":
    //                        {
    //                            feodoEntry.as_number = Convert.ToInt32( detail.Value );
    //                            break;
    //                        }
    //                    case "as_name":
    //                        {
    //                            feodoEntry.as_name = detail.Value;
    //                            break;
    //                        }
    //                    case "country":
    //                        {
    //                            feodoEntry.country = detail.Value;
    //                            break;
    //                        }
    //                    case "first_seen":
    //                        {
    //                            feodoEntry.first_seen = detail.Value;
    //                            break;
    //                        }
    //                    case "last_online":
    //                        {
    //                            feodoEntry.last_online = detail.Value;
    //                            break;
    //                        }
    //                    case "malware":
    //                        {
    //                            feodoEntry.malware = detail.Value;
    //                            break;
    //                        }
    //                }
    //            }

    //            processed.Add( feodoEntry );
    //        }
    //        catch ( Exception ex )
    //        {
    //            MessageBox.Show( ex.Message );
    //        }
    //    }

    //    return processed;
    //}

    // I haven't got this working satisfactorily yet
    //internal static void GetAllFilePathAndSubDirectory( string baseUrl, List<PathInfo> pathInfos )
    //{
    //    RemoteSite emptySite = new( ) { Name = "", FileUrls = string.Empty };
    //    Uri baseUri = new( baseUrl.EndsWith( '/' ) ? baseUrl : baseUrl.TrimEnd( '/' ) );
    //    string rootUrl = baseUri.GetLeftPart( UriPartial.Authority );

    //    Regex regexFile = RegexFile( );
    //    Regex regexDir = RegexDirectory( );

    //    string html = ReadHtmlContentFromUrl( emptySite, baseUrl );

    //    //Files
    //    MatchCollection matchesFile = regexFile.Matches( html );
    //    if ( matchesFile.Count != 0 )
    //        foreach ( Match match in matchesFile )
    //            if ( match.Success )
    //                pathInfos.Add(
    //                    new PathInfo( rootUrl + match.Groups[ "file" ], false ) );
    //    //Dir
    //    MatchCollection matchesDir = regexDir.Matches( html );
    //    if ( matchesDir.Count != 0 )
    //        foreach ( Match match in matchesDir )
    //            if ( match.Success )
    //            {
    //                var dirInfo = new PathInfo( rootUrl + match.Groups[ "dir" ], true );
    //                GetAllFilePathAndSubDirectory( dirInfo.AbsoluteUrlStr, dirInfo.Children );
    //                pathInfos.Add( dirInfo );
    //            }

    //}

    //internal static void PrintAllPathInfo( List<PathInfo> pathInfos )
    //{
    //    pathInfos.ForEach( f =>
    //    {
    //        Console.WriteLine( f.AbsoluteUrlStr );
    //        PrintAllPathInfo( f.Children );
    //    } );
    //}

    //[GeneratedRegex( "dir.*?<a href=\"(http:)?(?<dir>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexDirectory( );

    //[GeneratedRegex( "[0-9] <a href=\"(http:)?(?<file>.*?)\"", RegexOptions.IgnoreCase, "en-GB" )]
    //internal static partial Regex RegexFile( );
}
