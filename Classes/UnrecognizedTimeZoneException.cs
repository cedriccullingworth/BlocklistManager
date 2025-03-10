//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//        27 February 2007
//********************************************************************************************
using System;

namespace BlocklistManager.Classes
{
    [Serializable]
    internal sealed class UnrecognizedTimeZoneException : Exception
    {
        public UnrecognizedTimeZoneException( )
        {
        }

        public UnrecognizedTimeZoneException( string? message ) : base( message )
        {
        }

        public UnrecognizedTimeZoneException( string? message, Exception? innerException ) : base( message, innerException )
        {
        }
    }
}