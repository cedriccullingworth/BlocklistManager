using System;

namespace BlocklistManager;

internal sealed class FeodoEntry : IDisposable
{
    //internal FeodoEntry()
    //{

    //}

    internal string ip_address { get; set; } = string.Empty;

    internal ushort[] ports { get; set; } = [];

    internal string? status { get; set; }

    internal string? hostname { get; set; }

    internal int? as_number { get; set; } = 0;

    internal string? as_name { get; set; }

    internal string? country { get; set; }

    internal string? first_seen { get; set; }

    internal string? last_online { get; set; }

    internal string? malware { get; set; }

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
    // ~FeodoEntry()
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
