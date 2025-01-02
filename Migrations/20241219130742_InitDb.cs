using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlocklistManager.Migrations
{
    /// <inheritdoc />
    public partial class InitDb : Migration
    {
        /// <inheritdoc />
        protected override void Up( MigrationBuilder migrationBuilder )
        {
            migrationBuilder.CreateTable(
                name: "FileType",
                columns: table => new
                {
                    ID = table.Column<int>( type: "int", nullable: false )
                        .Annotation( "SqlServer:Identity", "1, 1" ),
                    Name = table.Column<string>( type: "nvarchar(50)", nullable: false ),
                    Description = table.Column<string>( type: "nvarchar(255)", nullable: false )
                },
                constraints: table =>
                {
                    table.PrimaryKey( "PK_FileType", x => x.ID );
                } );

            migrationBuilder.CreateTable(
                name: "RemoteSite",
                columns: table => new
                {
                    ID = table.Column<int>( type: "int", nullable: false )
                        .Annotation( "SqlServer:Identity", "1, 1" ),
                    Name = table.Column<string>( type: "nvarchar(50)", nullable: false ),
                    LastDownloaded = table.Column<DateTime>( type: "datetime2", nullable: true ),
                    SiteUrl = table.Column<string>( type: "nvarchar(255)", nullable: false ),
                    FileUrls = table.Column<string>( type: "nvarchar(4000)", nullable: false ),
                    FileTypeID = table.Column<int>( type: "int", nullable: true ),
                    Active = table.Column<bool>( type: "bit", nullable: false ),
                    MinimumIntervalMinutes = table.Column<int>( type: "int", nullable: false )
                },
                constraints: table =>
                {
                    table.PrimaryKey( "PK_RemoteSite", x => x.ID );
                    table.ForeignKey(
                        name: "FK_RemoteSite_FileType_FileTypeID",
                        column: x => x.FileTypeID,
                        principalTable: "FileType",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict );
                } );

            migrationBuilder.CreateIndex(
                name: "IX_FileType_Name",
                table: "FileType",
                column: "Name",
                unique: true );

            migrationBuilder.CreateIndex(
                name: "IX_RemoteSite_FileTypeID",
                table: "RemoteSite",
                column: "FileTypeID" );

            migrationBuilder.CreateIndex(
                name: "IX_RemoteSite_Name",
                table: "RemoteSite",
                columns: new[] { "Name", "SiteUrl" },
                unique: true );
        }

        /// <inheritdoc />
        protected override void Down( MigrationBuilder migrationBuilder )
        {
            migrationBuilder.DropTable(
                name: "RemoteSite" );

            migrationBuilder.DropTable(
                name: "FileType" );
        }
    }
}
