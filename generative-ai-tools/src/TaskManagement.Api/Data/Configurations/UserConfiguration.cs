using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const int EmailMaxLength = 320;
    public const int NameMaxLength = 200;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.Email).IsRequired().HasMaxLength(EmailMaxLength);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.Name).IsRequired().HasMaxLength(NameMaxLength);
    }
}
