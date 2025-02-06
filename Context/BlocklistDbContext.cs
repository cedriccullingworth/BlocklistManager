using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BlocklistManager.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using SBS.Utilities;

namespace BlocklistManager.Context;

public class BlocklistDbContext : DbContext
{
    private string _connectionString = string.Empty;

    public BlocklistDbContext( ) : base( )
    {
        _connectionString = GetConnectionString( );

        try
        {
            Database.SetConnectionString( _connectionString );
            Database.SetCommandTimeout( 30 );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( StringUtilities.ExceptionMessage( "BlocklistDbContext()", ex ) );
        }
    }

    public BlocklistDbContext( DbContextOptions<BlocklistDbContext> options ) : base( options )
    {
        _connectionString = GetConnectionString( );

        try
        {
            Database.SetConnectionString( _connectionString );
            Database.SetCommandTimeout( 30 );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( StringUtilities.ExceptionMessage( "BlocklistDbContext(options)", ex ) );
        }
    }

    public static string GetConnectionString( )
    {
        // When using efbundle, it looks like the path is empty, so I've changed the code to handle that

        string configFilePath = Assembly.GetExecutingAssembly( ).Location;
        if ( !string.IsNullOrEmpty( configFilePath ) )
            configFilePath = $"{configFilePath[ 0..configFilePath.LastIndexOf( '\\' ) ]}\\appsettings.json";
        else
            configFilePath = "appsettings.json";

        IConfigurationBuilder configuration = new ConfigurationBuilder( ).AddJsonFile( configFilePath );
        IConfigurationRoot config = configuration.Build( );
        return config.GetConnectionString( "BlocklistDbContext" ) ?? string.Empty;
    }

    protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
    {
        if ( string.IsNullOrEmpty( _connectionString ) )
            _connectionString = GetConnectionString( );

        try
        {
            optionsBuilder.UseSqlServer( _connectionString );
            base.OnConfiguring( optionsBuilder );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( StringUtilities.ExceptionMessage( "OnConfiguring", ex ) );
        }
    }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        base.OnModelCreating( modelBuilder );
    }

    internal void EnsureDataExists( )
    {
        if ( !RemoteSites.Any( ) )
        {
            RemoteSites.Add( new RemoteSite( )
            {
                Name = "Feodo",
                SiteUrl = "https://feodotracker.abuse.ch",
                FileUrls = "https://feodotracker.abuse.ch/downloads/ipblocklist_recommended.json, https://feodotracker.abuse.ch/downloads/ipblocklist.json",
                FileType = FileTypes
                               .FirstOrDefault( f => f.Name == "JSON" ),
                FileTypeID = FileTypes
                               .First( f => f.Name == "JSON" )
                               .ID,
                LastDownloaded = null,
                Active = true
            } );

            SaveChanges( );
        }

    }

    internal List<FileType> ListFileTypes( )
    {
        return [ .. FileTypes.OrderBy( o => o.Name ) ];
    }

    /// <summary>
    /// NEW: Exclude sites processed less than half and hour ago from the list
    /// </summary>
    /// <returns>A list of blocklist download sites</returns>
    internal List<RemoteSite> ListRemoteSites( RemoteSite? remoteSite, bool showAll = false )
    {
        IQueryable<RemoteSite> query = RemoteSites.Include( i => i.FileType )
                                                       .Where( w => showAll || w.Active )
                                                       .Where( w => remoteSite == null || w.ID == remoteSite.ID );

        if ( !showAll )
            query = query.Where( w => w.LastDownloaded == null
                            || w.MinimumIntervalMinutes == 0
                            || ( w.LastDownloaded ?? DateTime.MinValue ).AddMinutes( w.MinimumIntervalMinutes ) < DateTime.UtcNow );

        return [ .. query.OrderBy( o => o.Name ) ];
    }

    internal void SetDownloadedDateTime( RemoteSite site )
    {
        site = RemoteSites.First( f => f.ID == site.ID );
        site.LastDownloaded = DateTime.UtcNow;
        SaveChanges( );
    }

    internal DbSet<RemoteSite> RemoteSites { get; set; }

    internal DbSet<FileType> FileTypes { get; set; }

    internal bool EnsureStartupDataExists( )
    {
        bool dataExists = FileTypes.Count( ) >= 9 && RemoteSites.Count( ) >= 23;
        if ( !dataExists )
        {
            dataExists = LoadFileTypes( );
            if ( dataExists )
            {
                dataExists = LoadRemoteSites( );
            }
        }

        return dataExists;
    }

    private bool LoadFileTypes( )
    {
        bool result = FileTypes.Count( ) >= 9;
        if ( !result )
        {
            try { FileTypes.Add( new FileType( ) { ID = 1, Name = "TXT", Description = "Single column text file listing IP addresses" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 2, Name = "JSON", Description = "Json" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 3, Name = "XML", Description = "XML" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 4, Name = "TAB", Description = "Tab delimited" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 5, Name = "JSONZIP", Description = "Zip archive containing Json" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 6, Name = "TXTZIP", Description = "Zip archive containing text" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 7, Name = "DELIMZIP", Description = "Zip archive containing delimited data" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 8, Name = "TXTALIEN", Description = "AlienVault text layout" } ); } catch { }
            try { FileTypes.Add( new FileType( ) { ID = 9, Name = "CSV", Description = "Comma delimited" } ); } catch { }

            try
            {
                Database.OpenConnection( );
                Database.ExecuteSqlRaw( "SET IDENTITY_INSERT dbo.FileType ON" );
                SaveChanges( );
                Database.ExecuteSqlRaw( "SET IDENTITY_INSERT dbo.FileType OFF" );
                result = FileTypes.Count( ) == 9;
            }
            catch ( DbUpdateException ex1 )
            {
                Console.WriteLine( StringUtilities.ExceptionMessage( "LoadFileTypes", ex1 ) );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( StringUtilities.ExceptionMessage( "LoadFileTypes", ex ) );
            }
            finally
            {
                Database.CloseConnection( );
            }
        }

        return result;
    }

    private bool LoadRemoteSites( )
    {
        bool result = RemoteSites.Count( ) >= 23;
        if ( !result )
        {
            try { RemoteSites.Add( new RemoteSite( ) { ID = 1, Name = "Feodo", SiteUrl = "https://feodotracker.abuse.ch", FileUrls = "https://feodotracker.abuse.ch/downloads/ipblocklist_recommended.json, https://feodotracker.abuse.ch/downloads/ipblocklist.json", FileTypeID = 2, LastDownloaded = new DateTime( 2024, 12, 13, 12, 58, 41 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 2, Name = "MyIP", SiteUrl = "https://myip.ms", FileUrls = "https://myip.ms/files/blacklist/general/latest_blacklist.txt", FileTypeID = 9, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 57 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 3, Name = "FireHOL Level 3", SiteUrl = "https://raw.githubusercontent.com/firehol", FileUrls = "https://raw.githubusercontent.com/firehol/blocklist-ipsets/master/firehol_level3.netset", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 43 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 4, Name = "GreenSnow", SiteUrl = "https://greensnow.co/", FileUrls = "https://blocklist.greensnow.co/greensnow.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 45 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 5, Name = "AlienVault", SiteUrl = "https://reputation.alienvault.com", FileUrls = "https://reputation.alienvault.com/reputation.generic", FileTypeID = 8, LastDownloaded = new DateTime( 2024, 12, 15, 10, 48, 18 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 6, Name = "Binary Defense Systems Artillery Threat Intelligence Feed and Banlist Feed", SiteUrl = "https://www.binarydefense.com", FileUrls = "https://www.binarydefense.com/banlist.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 13 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 7, Name = "CI Army", SiteUrl = "https://cinsscore.com", FileUrls = "https://cinsscore.com/list/ci-badguys.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 19 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 8, Name = "dan.me.uk torlist", SiteUrl = "https://www.dan.me.uk", FileUrls = "https://www.dan.me.uk/torlist/index.html", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 40 ), Active = true, MinimumIntervalMinutes = 45 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 9, Name = "Emerging Threats Compromised and Firewall Block List", SiteUrl = "https://www.emergingthreats.net", FileUrls = "https://rules.emergingthreats.net/blockrules/compromised-ips.txt, https://rules.emergingthreats.net/fwrules/emerging-Block-IPs.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 43 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 10, Name = "Internet Storm Center DShield", SiteUrl = "https://feeds.dshield.org", FileUrls = "https://feeds.dshield.org/block.txt", FileTypeID = 4, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 47 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 11, Name = "Internet Storm Center Shodan", SiteUrl = "https://isc.sans.edu", FileUrls = "https://isc.sans.edu/api/threatlist/shodan/shodan.txt", FileTypeID = 3, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 48 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 12, Name = "IBM X-Force Exchange", SiteUrl = "https://exchange.xforce.ibmcloud.com/", FileUrls = "https://iplists.firehol.org/files/xforce_bccs.ipset", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 46 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 13, Name = "pgl.yoyo.org AdServers", SiteUrl = "https://pgl.yoyo.org", FileUrls = "https://pgl.yoyo.org/adservers/iplist.php?ipformat=&showintro=0&mimetype=plaintext", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 01, 00 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 14, Name = "ScriptzTeam", SiteUrl = "https://github.com/scriptzteam/IP-BlockList-v4/blob", FileUrls = "https://raw.githubusercontent.com/scriptzteam/IP-BlockList-v4/refs/heads/main/ips.txt", FileTypeID = 4, LastDownloaded = new DateTime( 2024, 12, 18, 10, 01, 02 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 15, Name = "PAllebone", SiteUrl = "https://github.com/pallebone/StrictBlockPAllebone", FileUrls = "https://raw.githubusercontent.com/pallebone/StrictBlockPAllebone/master/BlockIP.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 59 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 16, Name = "Blocklist.de", SiteUrl = "http://lists.blocklist.de/lists/", FileUrls = "http://lists.blocklist.de/lists/all.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 15 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 17, Name = "CyberCrime-Tracker", SiteUrl = "https://cybercrime-tracker.net/fuckerz.php", FileUrls = "https://cybercrime-tracker.net/rss.xml", FileTypeID = 3, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 20 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 18, Name = "DigitalSide Threat-Intel Repository", SiteUrl = "https://osint.digitalside.it/", FileUrls = "https://osint.digitalside.it/Threat-Intel/lists/latestips.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 42 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 19, Name = "abuse.ch", SiteUrl = "https://sslbl.abuse.ch/blacklist/", FileUrls = "https://sslbl.abuse.ch/blacklist/sslipblacklist.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 13 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 22, Name = "Miroslav Stampar", SiteUrl = "https://github.com/stamparm", FileUrls = "https://raw.githubusercontent.com/stamparm/ipsum/master/ipsum.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 55 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 24, Name = "James Brine", SiteUrl = "https://jamesbrine.com.au", FileUrls = "https://jamesbrine.com.au/csv", FileTypeID = 9, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 55 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 25, Name = "NoThink!", SiteUrl = "https://www.nothink.org", FileUrls = "https://www.nothink.org/honeypots/honeypot_ssh_blacklist_2019.txt, https://www.nothink.org/honeypots/honeypot_telnet_blacklist_2019.txt", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 00, 58 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }
            try { RemoteSites.Add( new RemoteSite( ) { ID = 26, Name = "Rutgers Blacklisted IPs", SiteUrl = "https://report.cs.rutgers.edu/mrtg/drop/dropstat.cgi?start=-86400", FileUrls = "https://report.cs.rutgers.edu/DROP/attackers", FileTypeID = 1, LastDownloaded = new DateTime( 2024, 12, 18, 10, 01, 01 ), Active = true, MinimumIntervalMinutes = 0 } ); } catch { }

            try
            {
                Database.OpenConnection( );
                Database.ExecuteSqlRaw( "SET IDENTITY_INSERT dbo.RemoteSite ON" );
                SaveChanges( );
                Database.ExecuteSqlRaw( "SET IDENTITY_INSERT dbo.RemoteSite OFF" );
                result = RemoteSites.Count( ) == 23;
            }
            catch ( DbUpdateException ex1 )
            {
                Console.WriteLine( StringUtilities.ExceptionMessage( "LoadRemoteSites", ex1 ) );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( StringUtilities.ExceptionMessage( "LoadRemoteSites", ex ) );
            }
            finally
            {
                Database.CloseConnection( );
            }
        }

        return result;
    }
}
