using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace BlocklistManager.Models;

[Table( "RemoteSite" )]
[DisplayColumn( "Name" )]
[PrimaryKey( "ID" )]
[Index( "Name", additionalPropertyNames: [ "SiteUrl" ], IsUnique = true, Name = "IX_RemoteSite_Name" )]
[Index( "FileTypeID", Name = "IX_RemoteSite_FileTypeID" )]
public class RemoteSite
{
    [Key]
    [DatabaseGenerated( DatabaseGeneratedOption.Identity )]
    [Column( TypeName = "int" )]
    public int ID { get; set; } = 0;

    [Length( 2, 50 )]
    [Column( TypeName = "nvarchar(128)" )]
    public required string Name { get; set; }

    public DateTime? LastDownloaded { get; set; } = DateTime.UtcNow;

    [Length( 2, 255 )]
    [Column( TypeName = "nvarchar(255)" )]
    public string SiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// A comma-separated array of download file paths
    /// </summary>
    [Required]
    [Length( 2, 4000 )]
    [Column( TypeName = "nvarchar(4000)" )]
    public required string FileUrls { get; set; }

    public IList<string> FilePaths => FileUrls.Split( ',' )
                           .Select( s => s.Trim( ) )
                           .ToList( );

    public int? FileTypeID { get; set; } = 1;

    [ForeignKey( "FileTypeID" )]
    [DeleteBehavior( DeleteBehavior.Restrict )]
    public FileType? FileType { get; set; }

    public bool Active { get; set; } = true;

    public int MinimumIntervalMinutes { get; set; } = 30;
}
