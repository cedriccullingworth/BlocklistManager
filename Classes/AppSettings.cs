using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Classes;

internal static class AppSettings
{
    [UnconditionalSuppressMessage( "SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "<Pending>" )]
    static readonly string configFilePath = Assembly.GetExecutingAssembly( )
                                                    .Location[ 0..Assembly.GetExecutingAssembly( ).Location.LastIndexOf( '\\' ) ] + "\\appsettings.json";

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
