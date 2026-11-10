using Microsoft.Data.SqlClient;

namespace LessonOrganizer.Infrastructure.Repositories;

public static class SqlErrors
{
    private const int ForeignKeyViolation = 547;
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInConstraint = 2627;

    public static bool IsForeignKeyViolation(Exception ex) => SqlErrorNumber(ex) == ForeignKeyViolation;

    public static bool IsDuplicateKey(Exception ex) =>
        SqlErrorNumber(ex) is DuplicateKeyInUniqueIndex or DuplicateKeyInConstraint;

    private static int? SqlErrorNumber(Exception ex) => (ex as SqlException ?? ex.InnerException as SqlException)?.Number;
}
