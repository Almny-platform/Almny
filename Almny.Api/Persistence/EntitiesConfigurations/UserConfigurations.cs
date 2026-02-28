using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Almny.Api.Persistence.EntitiesConfigurations;

public class UserConfigurations : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");
    }
}
