using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Forms;

using BlocklistManager.Models;

using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Classes;

/// <summary>
/// This class does all HttpClient work for BlocklistManager
/// </summary>
internal sealed class BlocklistData : IDisposable
{
    private bool _disposedValue;
    private readonly BlocklistDataConfig _config;
    private Device? _connectedDevice;

    /// <summary>
    /// Constructor: _config is initialized with default testing values then updated with values from appsettings.json
    /// </summary>
    internal BlocklistData( )
    {
        _config = new BlocklistDataConfig( )
        {
            HostAddress = "localhost",
            HostPort = "44318"
        };

        try
        {
            ConfigurationSection settings = AppSettings.Sections.First( f => f.Key == "BlocklistDataConfig" );
            _config = new BlocklistDataConfig( )
            {
                HostAddress = settings[ "HostAddress" ] ?? "",
                HostPort = settings[ "HostPort" ] ?? ""
            };
            #region Commented out for Versions 1.x
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
            #endregion
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "BlocklistManager", StringUtilities.ExceptionMessage( "BlocklistData", ex ) );
            MessageBox.Show( $"BlocklistManager: Settings not found." );
        }
    }

    /// <summary>
    /// HttpClient for listing file types
    /// </summary>
    /// <returns>A list of available file types</returns>
    [RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
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
            sites = JsonSerializer.Deserialize<List<FileType>>( json ) ?? [];
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "ListFileTypes", StringUtilities.ExceptionMessage( "", ex ) );
        }

        return sites;
    }

    /// <summary>
    /// Extract remote sites as a list
    /// </summary>
    /// <param name="remoteSite">A remote site if only fetching one site</param>
    /// <param name="showAll">If true, list all sites, including those that have been processed recently, otherwise only those whic weren't downloaded in the past 30 minutes</param>
    /// <returns>A list of blocklist download sites</returns>
    [RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
    internal List<RemoteSite> ListDownloadSites( int deviceID, RemoteSite? remoteSite, bool showAll = false )
    {
        try
        {
            using HttpClient client = new HttpClient( )
            {
                BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                Timeout = TimeSpan.FromSeconds( 100 )
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
            string endpointUrl = $"/RemoteSites/ListRemoteSites?deviceID={deviceID}&showAll={showAll}";
            if ( remoteSite is not null )
            {
                endpointUrl = $"/RemoteSites/ListRemoteSites?deviceID={deviceID}&remoteSiteID={remoteSite!.ID}&showAll={showAll}";
            }

            var response = client.GetAsync( endpointUrl ).Result;
            string json = new StreamReader( response.Content.ReadAsStream( ) ).ReadToEnd( );
            return JsonSerializer.Deserialize<List<RemoteSite>>( json ) ?? [];
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "ListDownloadSites", StringUtilities.ExceptionMessage( "", ex ) );
            return [];
        }
    }

    /// <summary>
    /// Get the device details matching the MAC address provided. (The table only has DeviceID and MACAddress)
    /// Note that a new device is created if the MAC address is not found.
    /// </summary>
    /// <param name="macAddress">The MAC address to look up</param>
    /// <returns>The Device entry matching the ID provided</returns>
    [RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
    internal Device? GetDevice( string macAddress )
    {
        if ( _connectedDevice is null )
        {
            try
            {
                using HttpClient client = new HttpClient( )
                {
                    BaseAddress = new Uri( $"https://{_config.HostAddress}:{_config.HostPort}" ),
                    Timeout = TimeSpan.FromSeconds( 30 )
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "localhost" );
                var results = client.GetAsync( $"/Devices/Details/{macAddress}" )
                                                    .Result;

                string json = new StreamReader( results.Content.ReadAsStream( ) ).ReadToEnd( );
                _connectedDevice = JsonSerializer.Deserialize<Device>( json );
            }
            catch ( Exception ex )
            {
                Maintain.StatusMessage( "GetDevice", StringUtilities.ExceptionMessage( "", ex ) );
            }
        }

        return _connectedDevice;
    }

    /// <summary>
    /// Sets the current download time for a device and remote site
    /// </summary>
    /// <param name="deviceID">The device ID</param>
    /// <param name="remoteSiteID">The download site's ID</param>
    /// <returns>The time which was recorded</returns>
    [RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
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
                now = JsonSerializer.Deserialize<DeviceRemoteSite>( json )!.LastDownloaded ?? DateTime.UtcNow.ToLocalTime( );
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "SetLastDownloaded", StringUtilities.ExceptionMessage( "SetLastDownloaded", ex ) );
        }
        return now;
    }

    /// <summary>
    /// Dispose method, currently default code
    /// </summary>
    /// <param name="disposing"></param>
    internal void Dispose( bool disposing )
    {
        if ( !_disposedValue )
        {
            if ( disposing )
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~BlocklistData()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    /// <summary>
    /// Dispose method, currently default code
    /// </summary>
    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
