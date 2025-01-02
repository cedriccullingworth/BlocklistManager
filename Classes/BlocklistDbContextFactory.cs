using System;

using BlocklistManager.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using SBS.Utilities;

namespace BlocklistManager.Classes;

public class BlocklistDbContextFactory : IDesignTimeDbContextFactory<BlocklistDbContext>
{
    public BlocklistDbContext CreateDbContext( string[] args )
    {
        string connectionString = BlocklistDbContext.GetConnectionString( );
        var optionsBuilder = new DbContextOptionsBuilder<BlocklistDbContext>( );
        optionsBuilder.UseSqlServer( connectionString );
        DbContextOptions<BlocklistDbContext> options = optionsBuilder.Options;

        try
        {
            BlocklistDbContext context = new BlocklistDbContext( optionsBuilder.Options );

            context.Database.EnsureCreated( );
            context.EnsureStartupDataExists( );
            context.Database.OpenConnection( );
            return context;
        }
        catch ( Exception ex ) // Thrown by context.Database.OpenConnection( ); CanConnect() suppresses exceptions
        {
            Console.WriteLine( StringUtilities.ExceptionMessage( "BlocklistDbContextFactory", ex ) );
        }

        return new BlocklistDbContext( options );
    }
}
