using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Data
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Barco> Barcos { get; set; }
        public DbSet<Empresa_Bodegas> Empresa_Bodegas { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Importacion> Importaciones { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<Unidad> Unidades { get; set; }
        public DbSet<HistorialCambios> HistorialCambios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<RolPermiso> RolPermisos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Importacion)
                .WithMany()
                .HasForeignKey(m => m.idimportacion)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.idempresa)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Movimiento>()
                .HasOne<Empresa_Bodegas>()
                .WithMany()
                .HasForeignKey(m => m.bodega)
                .OnDelete(DeleteBehavior.Restrict);
        
            modelBuilder.Entity<Importacion>()
                .HasOne<Barco>()
                .WithMany()
                .HasForeignKey(i => i.idbarco)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<RolPermiso>()
                .ToTable("rol_permisos");

            modelBuilder.Entity<Importacion>()
                .HasOne(i => i.Barco)
                .WithMany()
                .HasForeignKey(i => i.idbarco)
                .HasPrincipalKey(b => b.id)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Importacion>()
                .Property(i => i.idbarco)
                .HasColumnName("idbarco");
        }
    }
}
