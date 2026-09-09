using Microsoft.EntityFrameworkCore;

namespace SampleInvoiceProcessor.Data;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.VendorName).IsRequired().HasMaxLength(200);
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");
            entity.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(i => i.Status).IsRequired().HasMaxLength(20);
            entity.HasIndex(i => i.InvoiceNumber);
        });
    }
}
