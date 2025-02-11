using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Barco> Barcos { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Bodega> Bodegas { get; set; }
    public DbSet<Importacion> Importaciones { get; set; }
    public DbSet<Movimiento> Movimientos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Barco>(entity =>
        {
            entity.ToTable("barcos");
            entity.Property(e => e.Id).HasColumnName("id");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("empresas");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
        });

        modelBuilder.Entity<Bodega>(entity =>
        {
            entity.ToTable("bodegas");
            entity.Property(e => e.IdBodega).HasColumnName("id");
        });

        modelBuilder.Entity<Importacion>(entity =>
        {
            entity.ToTable("importaciones");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.HasOne(i => i.Barco)
                .WithMany()
                .HasForeignKey(i => i.IdBarco)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.ToTable("movimientos");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.HasOne(m => m.Importacion)
                .WithMany()
                .HasForeignKey(m => m.IdImportacion)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.IdEmpresa)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}