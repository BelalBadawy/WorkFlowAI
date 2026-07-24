using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WFAI.Domain.Entities;

namespace WFAI.Infrastructure.Persistence.DbConfigurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .IsRequired()
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("nvarchar(150)");

            builder.Property(c => c.NormalizedName)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            builder.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("nvarchar(250)");

            builder.Property(c => c.NormalizedSlug)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)");

            builder.Property(c => c.ParentId);

            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(c => c.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(c => c.RowVersion)
                .IsConcurrencyToken()
                .HasColumnType("varbinary(max)");

            builder.HasIndex(c => c.NormalizedName)
                .IsUnique()
                .HasDatabaseName("UX_Categories_NormalizedName")
                .HasFilter("[SoftDeleted] = 0");

            builder.HasIndex(c => c.NormalizedSlug)
                .IsUnique()
                .HasDatabaseName("UX_Categories_NormalizedSlug")
                .HasFilter("[SoftDeleted] = 0");

            builder.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}