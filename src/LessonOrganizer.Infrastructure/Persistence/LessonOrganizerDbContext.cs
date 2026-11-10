using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Persistence;

public sealed class LessonOrganizerDbContext(DbContextOptions<LessonOrganizerDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<Student> Students => Set<Student>();

    public DbSet<LessonPlan> LessonPlans => Set<LessonPlan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(LessonOrganizerDbContext).Assembly);
    }
}
