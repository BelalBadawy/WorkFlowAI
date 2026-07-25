using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WFAI.Domain.Entities;

namespace WFAI.Infrastructure.Persistence.DbConfigurations
{
    public class PhaseConfiguration : IEntityTypeConfiguration<Phase>
    {
        public void Configure(EntityTypeBuilder<Phase> builder)
        {
            builder.ToTable("Phases");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .IsRequired()
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("nvarchar(150)");

            builder.Property(p => p.NormalizedTitle)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            builder.Property(p => p.Description)
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            builder.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(p => p.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(p => p.RowVersion)
                .IsConcurrencyToken()
                .HasColumnType("varbinary(max)");

            builder.HasIndex(p => p.NormalizedTitle)
                .IsUnique()
                .HasDatabaseName("UX_Phases_NormalizedTitle")
                .HasFilter("[SoftDeleted] = 0");
        }
    }
}
