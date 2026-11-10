using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LessonOrganizer.Infrastructure.Persistence.Configurations;

public sealed class LessonPlanConfiguration : IEntityTypeConfiguration<LessonPlan>
{
    public void Configure(EntityTypeBuilder<LessonPlan> builder)
    {
        builder.ToTable("LessonPlans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();
        builder.Property(plan => plan.Title).HasMaxLength(LessonPlan.TitleMaxLength).IsRequired();
        builder.Property(plan => plan.Description);
        builder.Property(plan => plan.LessonDate).HasColumnType("date");
        builder.Property(plan => plan.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Student>().WithMany().HasForeignKey(plan => plan.StudentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(plan => new { plan.StudentId, plan.LessonDate });
    }
}
