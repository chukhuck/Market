using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FaG.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FearGreedIndices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScoreInt = table.Column<int>(type: "integer", nullable: false),
                    ScoreNormalized = table.Column<double>(type: "double precision", nullable: false),
                    TotalPosts = table.Column<int>(type: "integer", nullable: false),
                    PositivePosts = table.Column<int>(type: "integer", nullable: false),
                    NegativePosts = table.Column<int>(type: "integer", nullable: false),
                    NeutralPosts = table.Column<int>(type: "integer", nullable: false),
                    UnratedPosts = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FearGreedIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPostEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Emotion = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorNickname = table.Column<string>(type: "text", nullable: false),
                    PostText = table.Column<string>(type: "text", nullable: false),
                    CommentsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalReactions = table.Column<int>(type: "integer", nullable: false),
                    ReactionsJson = table.Column<string>(type: "text", nullable: false),
                    Tickers = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPostEvaluations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FearGreedIndices");

            migrationBuilder.DropTable(
                name: "UserPostEvaluations");
        }
    }
}
