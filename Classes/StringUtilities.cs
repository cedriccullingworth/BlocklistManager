using System;
using System.Globalization;

namespace BlocklistManager.Classes;

/// <summary>
/// General string utilities
/// </summary>
public static class StringUtilities
{
    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

    public static string ExceptionMessage( string caller, Exception ex )
    {
        return caller + " Exception: " + ex.Message +
                                ( ex.InnerException != null ? "\r\n" + ex.InnerException.Message +
                                    ( ex.InnerException.InnerException != null ? "\r\n" + ex.InnerException.InnerException.Message
                                    : string.Empty )
                                : string.Empty );
    }

    public static string IsolateStringPart( string stringToSearch, string startOfPart, string endOfPart, bool excludeStartStringFromResult = true )
    {
        int startPos = stringToSearch.IndexOf( startOfPart, StringComparison.InvariantCulture );
        string result = stringToSearch.Substring( startPos, stringToSearch[ startPos.. ].IndexOf( endOfPart, StringComparison.InvariantCulture ) );
        if ( excludeStartStringFromResult )
            result = result.Replace( startOfPart, string.Empty );

        return result;
    }

    public static bool IsDate( string input )
    {
        if ( input.Length < 8 )
            return false;

        try
        {
            DateTime dt = DateTime.Today;
            if ( DateTime.TryParse( input, out dt ) )
            {
                return DateUtilities.DateIsValid( dt.Year, dt.Month, dt.Day );
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determine if string is numeric [0-9]+
    /// </summary>
    /// <param name="rhs">string to test</param>
    /// <returns>true iff rhs is numeric</returns>
    public static bool IsNumeric( string rhs )
    {
        bool result;
        if ( ( rhs != null ) && ( rhs.Length > 0 ) )
        {
            result = true;
            for ( int i = 0; result && ( i < rhs.Length ); ++i )
            {
                if ( !char.IsDigit( rhs[ i ] ) )
                {
                    result = false;
                }
            }
        }
        else
        {
            result = false;
        }
        return result;
    }

    public static bool IsDecimal( ref string text, int decimalPlaces )
    {
        decimal deci = 0.00M;
        bool isNumber, isDecimal = false;

        IFormatProvider localFormat = System.Globalization.NumberFormatInfo.CurrentInfo;

        try
        {
            if ( text != null )
            {
                isNumber = int.TryParse( text, System.Globalization.NumberStyles.AllowDecimalPoint, localFormat, out int int32 );
                //if (!isNumber && text.Contains("."))
                if ( isNumber )
                {
                    isDecimal = Decimal.TryParse( text, System.Globalization.NumberStyles.AllowDecimalPoint, localFormat, out deci );

                }
                else if ( !isNumber && text.Contains( '.' ) )
                {
                    isDecimal = Decimal.TryParse( text, out deci );
                }
            }

            if ( isDecimal )
            {
                isDecimal = true;
                text = String.Format( localFormat, String.Format( culture, "{0}{1}{2}", "{0:F", decimalPlaces, "}" ), deci, culture );
            }
        }
        catch ( Exception ex )
        {
            System.Diagnostics.Debug.Print( ex.Message );
        }

        return isDecimal;
    }

    public static string NumberFromString( string stringToAnalyse )
    {
        string result = string.Empty;
        char[] stringAsChars = stringToAnalyse.ToCharArray( );
        bool numberStarted = false;

        for ( int i = 0; i < stringAsChars.Length; i++ )
        {
            if ( char.IsNumber( stringAsChars[ i ] ) )
            {
                if ( !numberStarted )
                    numberStarted = true;
                result += stringAsChars[ i ];
            }
            else if ( numberStarted && !string.IsNullOrEmpty( result ) )
                break;
        }

        return result;
    }

}