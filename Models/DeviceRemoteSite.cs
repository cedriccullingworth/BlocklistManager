using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace BlocklistManager.Models;

[Table( "DeviceRemoteSite" )]
[PrimaryKey( "ID" )]
[Index( "DeviceID", IsUnique = true, Name = "IX_DeviceRemoteSite_DeviceID" )]
[Index( "RemoteSiteID", IsUnique = false, Name = "IX_DeviceRemoteSite_RemoteSiteID" )]
[Index( "DeviceID", [ "RemoteSiteID" ], AllDescending = false, IsUnique = true, Name = "IX_DeviceRemoteSite_DeviceID_RemoteSiteID" )]
public partial class DeviceRemoteSite
{
    [Key]
    [Column( TypeName = "int" )]
    public int ID { get; set; } = 1;

    [Column( TypeName = "int" )]
    [ForeignKey( "DeviceID" )]
    [DeleteBehavior( DeleteBehavior.Restrict )]
    public Device Device { get; set; }

    [Column( TypeName = "int" )]
    [ForeignKey( "RemoteSiteID" )]
    [DeleteBehavior( DeleteBehavior.Restrict )]
    public RemoteSite RemoteSite { get; set; }

    [Column( "LastDownloaded" )]
    public DateTime? LastDownloaded { get; set; }
}

