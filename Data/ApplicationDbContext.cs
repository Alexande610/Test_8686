using Microsoft.EntityFrameworkCore;
using LittleFishBeauty.Models;

namespace LittleFishBeauty.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets - tên properties không có đuôi s để tránh nhầm lẫn với tên bảng
        public DbSet<TaiKhoan> TaiKhoan { get; set; }
        public DbSet<DiaChi> DiaChi { get; set; }
        public DbSet<DanhMuc> DanhMuc { get; set; }
        public DbSet<SanPham> SanPham { get; set; }
        public DbSet<AnhSanPham> AnhSanPham { get; set; }
        public DbSet<ThuocTinh> ThuocTinh { get; set; }
        public DbSet<GiaTriThuocTinh> GiaTriThuocTinh { get; set; }
        public DbSet<BienTheSanPham> BienTheSanPham { get; set; }
        public DbSet<ChiTietBienThe> ChiTietBienThe { get; set; }
        public DbSet<GioHang> GioHang { get; set; }
        public DbSet<ChiTietGioHang> ChiTietGioHang { get; set; }
        public DbSet<DonHang> DonHang { get; set; }
        public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
        public DbSet<DanhGia> DanhGia { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Không cần cấu hình tên bảng vì Entity Framework tự động thêm đuôi 's'
            // và database hiện tại đã có các bảng với đuôi 's' rồi

            // Configure composite primary key for ChiTietBienThe
            modelBuilder.Entity<ChiTietBienThe>()
                .HasKey(ct => new { ct.ID_BienThe, ct.ID_GiaTri });

            // Configure self-referencing relationship for DanhMuc
            modelBuilder.Entity<DanhMuc>()
                .HasOne(d => d.DanhMucCha)
                .WithMany(d => d.DanhMucCons)
                .HasForeignKey(d => d.ID_DanhMucCha)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure check constraints (if supported by your database)
            modelBuilder.Entity<TaiKhoan>()
                .HasCheckConstraint("CK_TaiKhoan_VaiTro", "VaiTro IN ('admin', 'khach')");

            modelBuilder.Entity<AnhSanPham>()
                .HasCheckConstraint("CK_AnhSanPham_LoaiAnh", "LoaiAnh IN ('chinh', 'phu', '360')");

            modelBuilder.Entity<DanhGia>()
                .HasCheckConstraint("CK_DanhGia_SoSao", "SoSao BETWEEN 1 AND 5");

            // Configure unique constraints
            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(t => t.Email)
                .IsUnique();

            modelBuilder.Entity<BienTheSanPham>()
                .HasIndex(b => b.SKU)
                .IsUnique();

            // Configure decimal precision
            modelBuilder.Entity<SanPham>()
                .Property(s => s.GiaBan)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BienTheSanPham>()
                .Property(b => b.GiaBan)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DonHang>()
                .Property(d => d.TongTien)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChiTietDonHang>()
                .Property(c => c.GiaLucDat)
                .HasPrecision(18, 2);

            // Configure cascade delete behavior
            modelBuilder.Entity<SanPham>()
                .HasMany(s => s.AnhSanPhams)
                .WithOne(a => a.SanPham)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SanPham>()
                .HasMany(s => s.BienTheSanPhams)
                .WithOne(b => b.SanPham)
                .OnDelete(DeleteBehavior.Cascade);

            // Tạm comment vì chưa có DanhGia
            // modelBuilder.Entity<SanPham>()
            //     .HasMany(s => s.DanhGias)
            //     .WithOne(d => d.SanPham)
            //     .OnDelete(DeleteBehavior.Cascade);

            // Configure DonHang relationships to avoid multiple cascade paths
            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.TaiKhoan)
                .WithMany(t => t.DonHangs)
                .HasForeignKey(d => d.ID_TaiKhoan)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DonHang>()
                .HasOne(d => d.DiaChi)
                .WithMany()
                .HasForeignKey(d => d.ID_DiaChi)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
