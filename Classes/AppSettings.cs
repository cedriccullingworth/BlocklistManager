using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Classes;

internal static class AppSettings
{
    static string configFilePath = Assembly.GetExecutingAssembly( ).Location[ 0..Assembly.GetExecutingAssembly( ).Location.LastIndexOf( '\\' ) ] + "\\appsettings.json";
    private static ConfigurationSection[] sections = [];

    internal static ConfigurationSection[] Sections
    {
        get
        {
            if ( sections.Length == 0 )
                sections = GetSections( configFilePath );
            return sections;
        }
    }

    private static ConfigurationSection[] GetSections( string configFilePath )
    {
        using ConfigurationManager configurationManager = new ConfigurationManager( );
        configurationManager.AddJsonFile( configFilePath );
        return configurationManager.GetChildren( ).Cast<ConfigurationSection>( ).ToArray( );
    }


}
