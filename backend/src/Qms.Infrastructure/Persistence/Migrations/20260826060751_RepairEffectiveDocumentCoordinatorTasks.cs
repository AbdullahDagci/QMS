using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairEffectiveDocumentCoordinatorTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE workflow.task_assignment AS task
                SET "Status" = 'Cancelled'
                FROM document.controlled_document AS document
                WHERE task."AggregateType" = 'Document'
                  AND task."AggregateId" = document."Id"
                  AND task."TaskRole" = 'DocumentAuthor'
                  AND task."Status" = 'Active'
                  AND document."Status" = 'Effective';

                INSERT INTO workflow.task_assignment
                    ("Id", "AggregateType", "AggregateId", "TaskRole",
                     "AssignedUserId", "AssignedUserNameSnapshot",
                     "AssignedDepartmentId", "AssignedDepartmentNameSnapshot",
                     "DelegatedFromUserId", "Status", "AssignedAtUtc",
                     "DueAtUtc", "CompletedAtUtc")
                SELECT
                    gen_random_uuid(),
                    'Document',
                    document."Id",
                    'DocumentCoordinator',
                    controller."Id",
                    controller."DisplayName",
                    controller."DepartmentId",
                    department."Name",
                    NULL,
                    'Active',
                    NOW(),
                    document."NextReviewDateUtc",
                    NULL
                FROM document.controlled_document AS document
                CROSS JOIN LATERAL
                (
                    SELECT app_user."Id", app_user."DisplayName", app_user."DepartmentId"
                    FROM identity."user" AS app_user
                    INNER JOIN identity.user_role AS user_role
                        ON user_role."UserId" = app_user."Id"
                    INNER JOIN identity."role" AS role
                        ON role."Id" = user_role."RoleId"
                    WHERE app_user."IsActive" = TRUE
                      AND role."Name" = 'DocumentController'
                    ORDER BY app_user."DisplayName", app_user."Id"
                    LIMIT 1
                ) AS controller
                LEFT JOIN organization.department AS department
                    ON department."Id" = controller."DepartmentId"
                WHERE document."Status" = 'Effective'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM workflow.task_assignment AS active_task
                      WHERE active_task."AggregateType" = 'Document'
                        AND active_task."AggregateId" = document."Id"
                        AND active_task."TaskRole" = 'DocumentCoordinator'
                        AND active_task."Status" = 'Active'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data repair is intentionally not reversed. Restoring stale active
            // author tasks would make effective documents non-actionable again.
        }
    }
}
