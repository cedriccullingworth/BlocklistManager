using System;

namespace BlocklistManager;

/// <summary>
/// Class to help with deserialization of DShield data.
/// </summary>
internal sealed class DShieldEntry : IDisposable
{
    internal string rangeStart { get; set; } = string.Empty;

    internal string rangeEnd { get; set; } = string.Empty;

    internal ushort Subnet { get; set; }

    internal long TargetsScanned { get; set; }

    internal string? NetworkName { get; set; }

    internal string? Country { get; set; }

    internal string? EmailAddress { get; set; }

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
    // ~DShieldEntry()
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
