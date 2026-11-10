using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LessonOrganizer.Infrastructure.Persistence.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(student => student.Id);
        builder.Property(student => student.Id).ValueGeneratedNever();
        builder.Property(student => student.Name).HasMaxLength(Student.NameMaxLength).IsRequired();
        builder.Property(student => student.Email).HasMaxLength(EmailField.MaxLength);
        builder.Property(student => student.EnglishLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(student => student.Notes).HasMaxLength(Student.NotesMaxLength);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(student => student.TeacherId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(student => new { student.TeacherId, student.Name });
    }
}
