using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BlocklistManager.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlocklistManager.Context;

internal class BlocklistDbContext : DbContext
{
    private readonly string _connectionString = string.Empty;

    public BlocklistDbContext( ) : base( )
    {
//        DbContextOptions<BlocklistDbContext> options = new( );
        _connectionString = GetConnectionString( );
        this.Database.SetCommandTimeout( 30 );
    }

    internal BlocklistDbContext( DbContextOptions<BlocklistDbContext> options ) : base( options )
    {
//        options ??= new( );
        _connectionString = GetConnectionString( );
        this.Database.SetCommandTimeout( 30 );
    }

    private static string GetConnectionString( )
    {
        string configFilePath = Assembly.GetExecutingAssembly( ).Location;
        configFilePath = $"{configFilePath[ 0..configFilePath.LastIndexOf( '\\' ) ]}\\appsettings.json";
        IConfigurationBuilder configuration = new ConfigurationBuilder( ).AddJsonFile( configFilePath );
        IConfigurationRoot config = configuration.Build( );
        return config.GetConnectionString( "BlocklistDbContext" ) ?? string.Empty;
    }

    protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
    {
        optionsBuilder.UseSqlServer( _connectionString );
        base.OnConfiguring( optionsBuilder );
    }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        base.OnModelCreating( modelBuilder );
    }

    internal void EnsureDataExists( )
    {
        if ( !this.RemoteSites.Any( ) )
        {
            this.RemoteSites.Add( new RemoteSite( )
            {
                Name = "Feodo",
                SiteUrl = "https://feodotracker.abuse.ch",
                FileUrls = "https://feodotracker.abuse.ch/downloads/ipblocklist_recommended.json, https://feodotracker.abuse.ch/downloads/ipblocklist.json",
                FileType = this.FileTypes
                               .FirstOrDefault( f => f.Name == "JSON" ),
                FileTypeID = this.FileTypes
                               .First( f => f.Name == "JSON" )
                               .ID,
                LastDownloaded = null,
                Active = true
            } );

            this.SaveChanges( );
        }

    }

    internal IList<FileType> ListFileTypes( ) => [ .. this.FileTypes.OrderBy( o => o.Name ) ];

    /// <summary>
    /// NEW: Exclude sites processed less than half and hour ago from the list
    /// </summary>
    /// <returns>A list of blocklist download sites</returns>
    internal List<RemoteSite> ListRemoteSites( RemoteSite? remoteSite, bool showAll = false )
    {
        IQueryable<RemoteSite> query = this.RemoteSites.Include( i => i.FileType )
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
        site = this.RemoteSites.First( f => f.ID == site.ID );
        site.LastDownloaded = DateTime.UtcNow;
        this.SaveChanges( );
    }

    internal DbSet<RemoteSite> RemoteSites { get; set; } = null!;

    internal DbSet<FileType> FileTypes { get; set; } = null!;
}
