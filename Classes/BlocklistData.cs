using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Forms;

using BlocklistManager.Models;

using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Classes;

internal sealed class BlocklistData : IDisposable
{
    private bool _disposedValue;
    private BlocklistDataConfig _config;
    private Device? _connectedDevice;

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
            Maintain.StatusMessage( "BlocklistManager", SBS.Utilities.StringUtilities.ExceptionMessage( "BlocklistData", ex ) );
            MessageBox.Show( $"BlocklistManager: Settings not found." );
        }
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
            Maintain.StatusMessage( "ListFileTypes", SBS.Utilities.StringUtilities.ExceptionMessage( "", ex ) );
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
            return System.Text.Json.JsonSerializer.Deserialize<List<RemoteSite>>( json ) ?? [];
        }
        catch ( Exception ex )
        {
            Maintain.StatusMessage( "ListDownloadSites", SBS.Utilities.StringUtilities.ExceptionMessage( "", ex ) );
            return [];
        }
    }

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
                _connectedDevice = System.Text.Json.JsonSerializer.Deserialize<Device>( json );
            }
            catch ( Exception ex )
            {
                Maintain.StatusMessage( "GetDevice", SBS.Utilities.StringUtilities.ExceptionMessage( "", ex ) );
            }
        }

        return _connectedDevice;
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
            Maintain.StatusMessage( "SetLastDownloaded", SBS.Utilities.StringUtilities.ExceptionMessage( "SetLastDownloaded", ex ) );
        }
        return now;
    }

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

    public void Dispose( )
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose( disposing: true );
        GC.SuppressFinalize( this );
    }
}
