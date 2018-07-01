using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modix.Data.Migrations
{
    public class InfractionsMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Infractions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                       .Annotation("Sqlite:Autoincrement", true),
                    Active = table.Column<bool>(nullable: false),
                    CreatorId = table.Column<long>(nullable: false),
                    GuildId = table.Column<int>(nullable: true),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<long>(nullable: false),
                    Severity = table.Column<int>(nullable: false),
                    DeactivatedBy = table.Column<long>(nullable: true),
                    Begins = table.Column<DateTime>(nullable: false),
                    Ends = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Infractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Infractions_Guilds_GuildId",
                         column: x => x.GuildId,
                       principalTable: "Guilds",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Infractions_GuildId",
                table: "Infractions",
                column: "GuildId");

            migrationBuilder.Sql(
                "INSERT INTO [Infractions] (" +
                    "[Active], [CreatorId], [GuildId], [Reason], [UserId], [Severity], [DeactivatedBy], [Begins], [Ends]" +
                ") " +
                "SELECT " +
                    "[Active], [CreatorId], [GuildId], [Reason], [UserId], 3, NULL, GETDATE(), '2099-12-31' " +
                "FROM [Bans]");

            migrationBuilder.DropTable("Bans");
        }


        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
               name: "Bans",
               columns: table => new
               {
                   Id = table.Column<int>(nullable: false)
                       .Annotation("Sqlite:Autoincrement", true),
                   Active = table.Column<bool>(nullable: false),
                   CreatorId = table.Column<long>(nullable: false),
                   GuildId = table.Column<int>(nullable: true),
                   Reason = table.Column<string>(nullable: true),
                   UserId = table.Column<long>(nullable: false)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Bans", x => x.Id);
                   table.ForeignKey(
                       name: "FK_Bans_Guilds_GuildId",
                       column: x => x.GuildId,
                       principalTable: "Guilds",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Restrict);
               });

            migrationBuilder.CreateIndex(
                name: "IX_Bans_GuildId",
                table: "Bans",
                column: "GuildId");

            migrationBuilder.DropTable("Infractions");
        }
    }
}
