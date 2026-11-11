using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Api.Tasks;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Data.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks", table => table.HasCheckConstraint("CK_Tasks_Title_NotBlank", "LEN([Title]) > 0"));
        builder.HasKey(task => task.Id).IsClustered(false);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.Title).IsRequired().HasMaxLength(TaskValidator.TitleMaxLength);
        builder.Property(task => task.Description).HasMaxLength(TaskValidator.DescriptionMaxLength);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        builder.Property(task => task.DueDate);
        builder.Property(task => task.CreatedAt);
        builder.Property(task => task.UpdatedAt);

        builder.HasOne<User>().WithMany().HasForeignKey(task => task.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(task => new { task.UserId, task.CreatedAt }).IsClustered();
    }
}
