using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SmartTask.Infrastructure.Persistence;

public static class UniqueConstraintGuard
{
    public const string DuplicateUserEmailMessage = "A user with this email already exists.";

    public static async Task SaveChangesOrThrowDuplicateEmailAsync(Func<Task> saveChanges)
    {
        try
        {
            await saveChanges();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new InvalidOperationException(DuplicateUserEmailMessage, ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
