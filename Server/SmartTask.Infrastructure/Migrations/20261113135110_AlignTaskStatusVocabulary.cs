using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTask.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignTaskStatusVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Assignment vocabulary: To Do (0), In Progress (1), Completed (2), Cancelled (3).
            // Legacy rows used Review (2), Done (3), Blocked (4).
            migrationBuilder.Sql("UPDATE [TaskItems] SET [Status] = 2 WHERE [Status] = 3;");
            migrationBuilder.Sql("UPDATE [TaskItems] SET [Status] = 3 WHERE [Status] = 4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [TaskItems] SET [Status] = 4 WHERE [Status] = 3;");
            migrationBuilder.Sql("UPDATE [TaskItems] SET [Status] = 3 WHERE [Status] = 2;");
        }
    }
}
