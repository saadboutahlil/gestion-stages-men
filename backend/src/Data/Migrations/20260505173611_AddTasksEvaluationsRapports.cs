using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionStagesMEN.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTasksEvaluationsRapports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupervisorId",
                table: "Internships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Internships_SupervisorId",
                table: "Internships",
                column: "SupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Internships_Supervisors_SupervisorId",
                table: "Internships",
                column: "SupervisorId",
                principalTable: "Supervisors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Internships_Supervisors_SupervisorId",
                table: "Internships");

            migrationBuilder.DropIndex(
                name: "IX_Internships_SupervisorId",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "Internships");
        }
    }
}
