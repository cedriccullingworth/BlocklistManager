using System;
using System.IO;
using System.Reflection;

namespace BlocklistManager.Classes;

/// <summary>
/// A general purpose logger that writes to a file
/// </summary>
public static class Logger
{
    /// <summary>
    /// Default full path for the log file
    /// </summary>
    private static string _logPath = $"{Assembly.GetEntryAssembly( )!.Location[ 0..Assembly.GetEntryAssembly( )!.Location.LastIndexOf( '.' ) ]}{/*DateTime.UtcNow:yyyy-MM-ddTHHZ*/string.Empty}.log"; // FIX WHILE TESTING

    /// <summary>
    /// The property to read or set the full path of the log file
    /// </summary>
    public static string LogPath
    {
        get
        {
            if ( _logPath == null ) // Shouldn't ever get here
            {
                // TODO: Make the path rewritable so that the caller can also read it from settings
                int endOfFolderPath = Assembly.GetEntryAssembly( )!.Location.LastIndexOf( '\\' );
                string location = Assembly.GetEntryAssembly( )!.Location[ 0..endOfFolderPath ] + @"\";
                string date = $"{DateTime.UtcNow:yyyy-MM-ddTHHZ}";
                int period = location.LastIndexOf( '.' );
                if ( period > 0 )
                    location = location[ ..period ];
                _logPath = location + $"{date}.log";
            }

            return _logPath;
        }

        set
        {
            _logPath = value;
        }
    }

    /// <summary>
    /// Writes a log entry to the log file
    /// </summary>
    /// <param name="callerMethod">The name of the caller - can be application, module or method name</param>
    /// <param name="text">The text to write, which will be preceded by the UTC value of the date and time written. The file will be created if it doesn't exist</param>
    /// <returns>True when done successfully</returns>
    public static bool Log( string callerMethod, string text )
    {
        bool succeeded = false;
        int retries = 3, retry = 0;

        if ( !File.Exists( LogPath ) )
        {
            using ( StreamWriter writer = new( LogPath, false, System.Text.Encoding.UTF8 ) )
            {
                writer.Close( );
            }
        }

        while ( !succeeded && retry < retries )
        {
            try
            {
                using ( StreamWriter writer = new( LogPath, true, System.Text.Encoding.UTF8 ) )
                {
                    string timeStamp = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss:fff}UTC ";
                    writer.WriteLine( $"{timeStamp}\t{callerMethod}\t{text}" );
                    writer.Close( );
                }

                succeeded = true;
            }
            catch ( Exception ex )
            {
                retry++;
                if ( retry == retries )
                    Log( callerMethod, ex );
            }
        }

        return succeeded;
    }

    /// <summary>
    /// Writes an array of log entries to the log file
    /// </summary>
    /// <param name="callerMethod">The name of the caller - can be application, module or method name</param>
    /// <param name="text">The array of text to write, each entry of which will be preceded by the UTC value of the date and time written. The file will be created if it doesn't exist</param>
    /// <returns>True when done successfully</returns>
    public static bool Log( string callerMethod, string[] text )
    {
        bool succeeded = false;
        int retries = 3, retry = 0;
        while ( !succeeded && retry < retries )
        {
            try
            {
                Span<string> span = text.AsSpan<string>( );
                using ( StreamWriter writer = new( LogPath, true, System.Text.Encoding.UTF8 ) )
                {
                    foreach ( string line in span )
                    {
                        string timeStamp = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss:fff}UTC ";
                        writer.WriteLine( $"{timeStamp}\t{callerMethod}\t{line}" );
                    }

                    writer.Close( );
                }

                succeeded = true;
            }
            catch
            {
                retry++;
            }
        }

        return succeeded;
    }

    /// <summary>
    /// Writes an exception to the log file
    /// </summary>
    /// <param name="callerMethod">The name of the caller - can be application, module or method name</param>
    /// <param name="exception">The exception to write, which will be preceded by the UTC value of the date and time written. The file will be created if it doesn't exist</param>
    /// <returns>True when done successfully</returns>
    public static bool Log( string callerMethod, Exception exception )
    {
        bool result = Log( callerMethod, exception.Message ?? "null" );
        if ( exception.InnerException != null )
        {
            result = Log( callerMethod, exception.InnerException.Message );
            if ( exception.InnerException.InnerException is not null )
            {
                result = Log( callerMethod, exception.InnerException.InnerException.Message );
            }
        }

        return result;
    }
}
