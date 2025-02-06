using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

using BlocklistManager.Models;

using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Classes;

internal class BlocklistData : IDisposable
{
    private bool disposedValue;
    private BlocklistDataConfig _config;
    private HttpClientHandler _handler;
    private Device? connectedDevice;

    internal BlocklistData( )
    {
        string configFilePath = Assembly.GetExecutingAssembly( ).Location;
        if ( !string.IsNullOrEmpty( configFilePath ) )
            configFilePath = $"{configFilePath[ 0..configFilePath.LastIndexOf( '\\' ) ]}\\appsettings.json";
        else
            configFilePath = "appsettings.json";

        ConfigurationManager configurationManager = new ConfigurationManager( );
        configurationManager.AddJsonFile( configFilePath );
        IConfigurationSection settings = configurationManager.GetSection( "BlocklistDataConfig" );
        _config = new BlocklistDataConfig( )
        {
            HostAddress = settings[ "HostAddress" ] ?? "",
            HostPort = settings[ "HostPort" ] ?? ""
        };

        //using SBS.Encryption2022.Encrypter encrypter = new( );
        //X509Certificate2? certificate = GetCertificate( out string error );
        //if ( certificate is not null )
        //{
        //    HttpRequestMessage requestMessage = new HttpRequestMessage( HttpMethod.Get, $"https://{_config.HostAddress}:{_config.HostPort}" );
        //    HttpRequestHeaders headers = requestMessage.Headers;

        //    _handler = new HttpClientHandler( )
        //    {
        //        ClientCertificateOptions = ClientCertificateOption.Manual,
        //        ClientCertificates = { certificate },
        //        ServerCertificateCustomValidationCallback = ( msg, certificate, chain, errors ) => true
        //    };
        //}
    }

    internal List<FileType> ListFileTypes( )
    {
        List<FileType> sites = [];
        try
        {
            using HttpClient client = new HttpClient( )
            {
                BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                Timeout = TimeSpan.FromSeconds( 10 )
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
            var results = client.GetAsync( "/FileTypes/Index" )
                                            .Result;

            string json = new StreamReader( results.Content.ReadAsStream( ) ).ReadToEnd( );
            sites = System.Text.Json.JsonSerializer.Deserialize<List<FileType>>( json ) ?? [];
        }
        catch ( Exception ex )
        {
            Console.WriteLine( SBS.Utilities.StringUtilities.ExceptionMessage( "ListFileTypes", ex ) );
        }

        return sites;
    }

    /// <summary>
    /// Extract remote sites as a list
    /// TESTED 1) without arguments
    /// </summary>
    /// <param name="remoteSite">A remote site if only fetching one site</param>
    /// <param name="showAll">If true, liust all sites, including those that have been processed recently, otherwise only those whic weren't downloaded in the past 30 minutes</param>
    /// <returns>A list of blocklist download sites</returns>
    internal List<RemoteSite> ListDownloadSites( int deviceID, RemoteSite? remoteSite, bool showAll = false )
    {
        try
        {
            using HttpClient client = new HttpClient( )
            {
                BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                Timeout = TimeSpan.FromSeconds( 10 )
            };

            // With remoteSite:
            // /RemoteSites/ListRemoteSites?deviceID=1&remoteSiteID=15&showAll=false
            // Without remoteSite:
            // /RemoteSites/ListRemoteSites?deviceID=1&showAll=false

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
            string endpointUrl = $"/RemoteSites/ListRemoteSites?deviceID={deviceID}&showAll={showAll}";
            if ( remoteSite is not null )
            {
                endpointUrl = $"/RemoteSites/ListRemoteSites?deviceID={deviceID}&remoteSiteID={remoteSite!.ID}&showAll={showAll}";
            }

            var response = client.GetAsync( endpointUrl ).Result;
            string json = new StreamReader( response.Content.ReadAsStream( ) ).ReadToEnd( );
            return System.Text.Json.JsonSerializer.Deserialize<List<RemoteSite>>( json ) ?? [];
        }
        catch ( Exception ex )
        {
            Console.WriteLine( SBS.Utilities.StringUtilities.ExceptionMessage( "ListDownloadSites", ex ) );
            return [];
        }
    }

    internal Device? GetDevice( string macAddress )
    {
        if ( connectedDevice is null )
        {
            try
            {
                using HttpClient client = new HttpClient( )
                {
                    BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                    Timeout = TimeSpan.FromSeconds( 10 )
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
                var results = client.GetAsync( $"/Devices/Details/{macAddress}" )
                                                    .Result;

                string json = new StreamReader( results.Content.ReadAsStream( ) ).ReadToEnd( );
                connectedDevice = System.Text.Json.JsonSerializer.Deserialize<Device>( json );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( SBS.Utilities.StringUtilities.ExceptionMessage( "GetDevice", ex ) );
            }
        }

        return connectedDevice;
    }

    internal DateTime SetLastDownloaded( int deviceID, int remoteSiteID )
    {
        DateTime now = DateTime.UtcNow;
        try
        {
            using HttpClient client = new HttpClient( )
            {
                BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                Timeout = TimeSpan.FromSeconds( 10 )
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
            var results = client.PostAsync( $"/DeviceRemoteSites/SetLastDownloaded/{deviceID},{remoteSiteID}", null )
                                                .Result;
            string json = new StreamReader( results.Content.ReadAsStream( ) ).ReadToEnd( );
            if ( json is not null )
                now = System.Text.Json.JsonSerializer.Deserialize<DeviceRemoteSite>( json )!.LastDownloaded ?? DateTime.UtcNow.ToLocalTime( );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( SBS.Utilities.StringUtilities.ExceptionMessage( "SetLastDownloaded", ex ) );
        }
        return now;
    }

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
    // ~BlocklistData()
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
