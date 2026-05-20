using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EFModels.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        private const string Schema = "public";

        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", Schema);

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnName("Id")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();

            builder.Property(u => u.Username)
                .HasColumnName("Username")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("PasswordHash")
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Email");

            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("UQ_Users_Username");
        }
    }
}
