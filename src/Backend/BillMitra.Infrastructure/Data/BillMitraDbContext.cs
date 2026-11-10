using Microsoft.EntityFrameworkCore;

namespace BillMitra.Infrastructure.Data;

public class BillMitraDbContext : DbContext
{
    public BillMitraDbContext(DbContextOptions<BillMitraDbContext> options) : base(options)
    {
    }

    public DbSet<SystemSettingEntity> SystemSettings => Set<SystemSettingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SystemSettingEntity>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

public class SystemSettingEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

