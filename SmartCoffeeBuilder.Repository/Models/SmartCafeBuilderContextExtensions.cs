using Microsoft.EntityFrameworkCore;

namespace SmartCoffeeBuilder.Repository.Models;

public partial class SmartCafeBuilderContext
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.Token).IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("token");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });
    }
}
