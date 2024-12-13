using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BlocklistManager.Interfaces;
using BlocklistManager.Models;

namespace BlocklistManager.Classes;

internal class HtmlDataCollector : IDisposable
{
    public string ReadData( string filePath )
    {
        using StreamReader reader = new StreamReader( filePath );
        return reader.ReadToEnd();
    }

    //public string ReadData( RemoteSite site, out string? fileExtension, string url )
    //{
    //    fileExtension = null; // N/A here
    //    string html = string.Empty;

    //    try
    //    {
    //        HttpClient client = new( ) { BaseAddress = new Uri( site.SiteUrl ) }; // url ) };
    //        var streamTemp = client.GetStreamAsync( new Uri( url ) ); // client.GetStreamAsync( new Uri( url ) );

    //        if ( streamTemp.Status == TaskStatus.RanToCompletion )
    //        {
    //            //Stream? stream = streamTemp.Result;

    //            //if ( stream is null && url is not null )
    //            //{
    //            //    stream = ReadHtmlStreamFromUrl( url );
    //            //}

    //            //if ( stream is not null )
    //            //{
    //            //    using StreamReader reader = new( stream );
    //            //    html = reader.ReadToEnd( );
    //            //}
    //        }
    //    }
    //    catch ( Exception ex )
    //    {
    //        MessageBox.Show( ex.Message ); // TODO: Improve the message
    //    }

    //    return html;

    //}

    public Stream? ReadHtmlStreamFromUrl( string url )
    {
        Stream? stream = null;
        string html = string.Empty;
        try
        {
            HttpClient client = new( ) { BaseAddress = new Uri( url ) };
            stream = client.GetStreamAsync( new Uri( url ) ).Result;
        }
        catch ( Exception ex )
        {
            MessageBox.Show( ex.Message ); // TODO: Improve the message
        }

        return stream;
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
    // ~HtmlDataCollector()
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
