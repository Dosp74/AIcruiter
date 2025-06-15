using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AIcruiter.Migrations
{
    public partial class AddSharedAnswer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestionId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Score = table.Column<int>(nullable: false),
                    AnswerContent = table.Column<string>(nullable: true),
                    SubmittedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedAnswers", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedAnswers");
        }
    }
}
