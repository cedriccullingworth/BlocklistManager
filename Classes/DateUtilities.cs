using System;
using System.Globalization;
using System.Linq;

using static BlocklistManager.Classes.DateTimeRoutines;

namespace BlocklistManager.Classes;

public static class DateUtilities
{
    private static readonly int firstValidYear = DateTime.Today.Year - 101;
    private static readonly int lastvalidYear = DateTime.Today.Year + 10;
    private static readonly int firstMonth = 1;
    private static readonly int lastMonth = 12;
    private static readonly int firstDay = 1;
    private static readonly int lastDay = 31;
    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

    //public static IList<int> ValidYears = new List<int>() [firstValidYear..lastvalidYear];

    //public static System.Linq.Enumerable;// = new System.Linq.Enumerable.Range(firstValidYear, lastvalidYear);

    //        public static IList<int> ValidYears => validYears.;

    public static readonly Range ValidMonths = new( firstMonth, lastMonth );

    public static readonly Range ValidDays = new( firstDay, lastDay );

    public static bool ValidYear( int year )
    {
        // TODO: TEST
        if ( year < 100 ) // 2 digit year
        {
            if ( year > Convert.ToInt32( DateTime.Today.Year.ToString( culture )[ 2..3 ], culture ) ) // If the last 2 digits are later than those of this year, then assume that it's in the current century
                year = Convert.ToInt32( DateTime.Today.Year.ToString( culture )[ 0..1 ] + year.ToString( culture ), culture );
            else // it's in the last century, so e.g. 79 becomes 1979
                year = Convert.ToInt32( DateTime.Today.Year.ToString( culture )[ 0..1 ] + "00", culture ) + year;
        }

        return year >= firstValidYear && year <= lastvalidYear;
    }

    public static bool ValidMonth( int month )
    {
        return month >= firstMonth && month <= lastMonth;
    }

    public static bool ValidDay( int day, int? month, int? year )
    {
        bool valid = day >= firstDay && day <= lastDay;
        if ( month is not null )
        {
            int[] shortMonths = [ 2, 4, 6, 9, 11 ];
            if ( year is not null )
                valid = day >= firstDay && day <= DateTime.DaysInMonth( (int)year, (int)month );
            else if ( shortMonths.Any( c => c == month ) )
            {
                valid = day >= firstDay && month <= 30;
                if ( month == 2 )
                    valid = day >= firstDay && day <= 28;
            }
        }

        return valid;
    }

    public static bool DateIsValid( int year, int month, int day )
    {
        if ( ValidYear( year ) )
            if ( ValidMonth( month ) )
                return ValidDay( day, month, year );

        return false;
    }

    public static DateTime DateFromString( string dateString )
    {
        DateTime result = new DateTime( );
        //int year = 1900, month = 0, day = 0;

        //if (!StringUtilities.IsDate( dateString ) || !StringUtilities.IsNumeric( dateString ))
        //    return result;

        //if (DateTime.TryParse(dateString, out result))
        //    return result;
        //else if (dateString.Length == 8)
        //{
        //    try
        //    {
        //        // First attempt a straight conversion
        //        try
        //        {
        //            DateTime date = Convert.ToDateTime( dateString );
        //            if (DateIsValid( date.Year, date.Month, date.Day ))
        //                return date;
        //        }
        //        catch // If that didn't work, try do identify the component parts
        //        {
        //            year = Convert.ToInt32( dateString[..3] );
        //            if (!ValidYear( year ))
        //            {
        //                year = Convert.ToInt32( dateString[4..5] );

        //            }
        //        }

        //    }
        //    catch
        //    {

        //    }
        //}
        //else
        //{
        //}
        if ( DateTimeRoutines.TryParseDate( dateString, DateTimeFormat.USADateFormat, out result ) )
            return result;
        else if ( DateTimeRoutines.TryParseDate( dateString, DateTimeFormat.USADateFormat, out result ) )
            return result;
        else
            return DateTime.MinValue;

    }

    //public static bool IsDate(string date)
}
