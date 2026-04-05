using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FaG.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addimoextable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPostEvaluations");

            migrationBuilder.DropColumn(
                name: "ScoreInt",
                table: "FearGreedIndices");

            migrationBuilder.RenameColumn(
                name: "TotalPosts",
                table: "FearGreedIndices",
                newName: "TotalRelevantPosts");

            migrationBuilder.RenameColumn(
                name: "ScoreNormalized",
                table: "FearGreedIndices",
                newName: "SmoothedIndex");

            migrationBuilder.RenameColumn(
                name: "DateUtc",
                table: "FearGreedIndices",
                newName: "Date");

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CumulativeIndex",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "InertialCumulative",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "NeutralRatio",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "NormalizedCumulative",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RawIndex",
                table: "FearGreedIndices",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "IMOEX",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Open = table.Column<double>(type: "double precision", nullable: false),
                    Close = table.Column<double>(type: "double precision", nullable: false),
                    Low = table.Column<double>(type: "double precision", nullable: false),
                    High = table.Column<double>(type: "double precision", nullable: false),
                    Volume = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IMOEX", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InnerId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorNickname = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Lenght = table.Column<int>(type: "integer", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalReactions = table.Column<int>(type: "integer", nullable: false),
                    ReactionsJson = table.Column<string>(type: "text", nullable: false),
                    Tickers = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Longiness = table.Column<long>(type: "bigint", nullable: false),
                    Emotion = table.Column<int>(type: "integer", nullable: false),
                    Evaluator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostEvaluations_UserPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "UserPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostEvaluations_PostId",
                table: "PostEvaluations",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IMOEX");

            migrationBuilder.DropTable(
                name: "PostEvaluations");

            migrationBuilder.DropTable(
                name: "UserPosts");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "FearGreedIndices");

            migrationBuilder.DropColumn(
                name: "CumulativeIndex",
                table: "FearGreedIndices");

            migrationBuilder.DropColumn(
                name: "InertialCumulative",
                table: "FearGreedIndices");

            migrationBuilder.DropColumn(
                name: "NeutralRatio",
                table: "FearGreedIndices");

            migrationBuilder.DropColumn(
                name: "NormalizedCumulative",
                table: "FearGreedIndices");

            migrationBuilder.DropColumn(
                name: "RawIndex",
                table: "FearGreedIndices");

            migrationBuilder.RenameColumn(
                name: "TotalRelevantPosts",
                table: "FearGreedIndices",
                newName: "TotalPosts");

            migrationBuilder.RenameColumn(
                name: "SmoothedIndex",
                table: "FearGreedIndices",
                newName: "ScoreNormalized");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "FearGreedIndices",
                newName: "DateUtc");

            migrationBuilder.AddColumn<int>(
                name: "ScoreInt",
                table: "FearGreedIndices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserPostEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorNickname = table.Column<string>(type: "text", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    Emotion = table.Column<int>(type: "integer", nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostText = table.Column<string>(type: "text", nullable: false),
                    ReactionsJson = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Tickers = table.Column<string>(type: "text", nullable: false),
                    TotalReactions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPostEvaluations", x => x.Id);
                });
        }
    }
}
