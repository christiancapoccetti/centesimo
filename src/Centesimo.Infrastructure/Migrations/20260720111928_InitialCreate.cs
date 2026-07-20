using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Centesimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    MonthlyBudget = table.Column<long>(type: "INTEGER", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceOccurrences",
                columns: table => new
                {
                    RecurringPaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceOccurrences", x => new { x.RecurringPaymentId, x.DueOn });
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                    table.ForeignKey(
                        name: "FK_Tags_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    ExpenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                    RecurringPaymentId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.ExpenseId);
                    table.ForeignKey(
                        name: "FK_Expenses_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringPayments",
                columns: table => new
                {
                    RecurringPaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    NextDueOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnchorMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    AnchorDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringPayments", x => x.RecurringPaymentId);
                    table.ForeignKey(
                        name: "FK_RecurringPayments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringPayments_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId_OccurredOn",
                table: "Expenses",
                columns: new[] { "CategoryId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OccurredOn",
                table: "Expenses",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TagId",
                table: "Expenses",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceOccurrences_RecurringPaymentId_DueOn",
                table: "RecurrenceOccurrences",
                columns: new[] { "RecurringPaymentId", "DueOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_CategoryId",
                table: "RecurringPayments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_NextDueOn",
                table: "RecurringPayments",
                column: "NextDueOn");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_TagId",
                table: "RecurringPayments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CategoryId_Name",
                table: "Tags",
                columns: new[] { "CategoryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "RecurrenceOccurrences");

            migrationBuilder.DropTable(
                name: "RecurringPayments");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
