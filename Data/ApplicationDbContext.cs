using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Models;

namespace Sistema_de_Gestion_de_Importaciones.Data
{
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
        public DbSet<Usuario> Usuarios { get; set; }

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
                entity.Property(e => e.FechaHora).HasColumnName("fechahora");
                entity.Property(e => e.FechaHoraSystema).HasColumnName("fechahorasystema");
                entity.Property(e => e.IdBarco).HasColumnName("idbarco");
                entity.Property(e => e.TotalCargaKilos).HasColumnName("totalcargakilos");
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

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
                entity.Property(e => e.UltimoAcceso).HasColumnName("ultimo_acceso");
                entity.Property(e => e.Activo).HasColumnName("activo");
            });
        }
    }
}