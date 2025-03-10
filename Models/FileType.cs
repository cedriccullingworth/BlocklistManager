using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//using Microsoft.EntityFrameworkCore;

namespace BlocklistManager.Models;

//[Table( "FileType" )]
//[DisplayColumn( "Name" )]
//[PrimaryKey( "ID" )]
//[Index( "Name", IsUnique = true, Name = "IX_FileType_Name" )]
public partial class FileType
{
    //    [Key]
    //    [Column( TypeName = "int" )]
    public int ID { get; set; } = 1;

    //    [Length( 2, 50 )]
    //    [Column( TypeName = "nvarchar(50)" )]
    public string Name { get; set; } = "TXT";

    //    [Length( 2, 255 )]
    //    [Column( TypeName = "nvarchar(255)" )]
    public string Description { get; set; } = "Text";

    public ICollection<RemoteSite> RemoteSites { get; set; } = [];
}

