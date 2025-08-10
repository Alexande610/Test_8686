using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LittleFishBeauty.Data;
using LittleFishBeauty.Models;

namespace LittleFishBeauty.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ChiTietController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChiTietController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int id)
        {
            var sanPham = await _context.SanPham
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGias)
                    .ThenInclude(d => d.TaiKhoan)
                .Include(s => s.DanhMuc)
                .Include(s => s.BienTheSanPhams)
                .FirstOrDefaultAsync(s => s.ID_SanPham == id && s.TrangThai == true);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Tính tổng số lượng tồn kho từ các biến thể
            var tongSoLuongTonKho = sanPham.BienTheSanPhams?.Sum(bt => bt.SoLuongTonKho) ?? 0;
            ViewBag.TongSoLuongTonKho = tongSoLuongTonKho;

            // Tính thống kê đánh giá
            var danhGias = sanPham.DanhGias?.Where(d => d.SoSao > 0).ToList() ?? new List<DanhGia>();
            ViewBag.TongSoDanhGia = danhGias.Count;
            ViewBag.DiemTrungBinh = danhGias.Any() ? Math.Round(danhGias.Average(d => d.SoSao), 1) : 0;
            
            // Thống kê theo từng mức sao
            ViewBag.ThongKeSao = new Dictionary<int, int>
            {
                { 5, danhGias.Count(d => d.SoSao == 5) },
                { 4, danhGias.Count(d => d.SoSao == 4) },
                { 3, danhGias.Count(d => d.SoSao == 3) },
                { 2, danhGias.Count(d => d.SoSao == 2) },
                { 1, danhGias.Count(d => d.SoSao == 1) }
            };

            // Lấy sản phẩm gợi ý (cùng danh mục)
            var sanPhamGoiY = await _context.SanPham
                .Include(s => s.AnhSanPhams)
                .Include(s => s.DanhGias)
                .Where(s => s.ID_DanhMuc == sanPham.ID_DanhMuc && 
                           s.ID_SanPham != id && 
                           s.TrangThai == true)
                .Take(4)
                .ToListAsync();

            ViewBag.SanPhamGoiY = sanPhamGoiY;
            ViewBag.ProductId = id;

            return View(sanPham);
        }

        [HttpPost]
        public async Task<IActionResult> ThemVaoGioHang(int productId, int quantity)
        {
            try
            {
                var sanPham = await _context.SanPham.FindAsync(productId);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // TODO: Implement add to cart logic
                // For now, just return success
                return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MuaNgay(int productId, int quantity)
        {
            try
            {
                var sanPham = await _context.SanPham.FindAsync(productId);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // TODO: Implement buy now logic
                return RedirectToAction("Index", "GioHang", new { area = "Customer" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ThemDanhGia(int productId, int rating, string comment, List<IFormFile> images)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"ThemDanhGia called - productId: {productId}, rating: {rating}, comment length: {comment?.Length ?? 0}, images count: {images?.Count ?? 0}");
                
                // Kiểm tra bắt buộc chọn số sao
                if (rating < 1 || rating > 5)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn số sao để đánh giá sản phẩm.";
                    return RedirectToAction("Index", new { id = productId });
                }

                var sanPham = await _context.SanPham.FindAsync(productId);
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction("Index", new { id = productId });
                }

                // Tạo đánh giá mới (tạm thời không cần login, dùng ID_TaiKhoan = 1)
                var danhGia = new DanhGia
                {
                    ID_SanPham = productId,
                    ID_TaiKhoan = 1, // TODO: Thay bằng current user ID khi có authentication
                    SoSao = rating,
                    BinhLuan = comment?.Trim(),
                    NgayDanhGia = DateTime.Now
                };

                // Xử lý upload hình ảnh (lưu tất cả hình được chọn)
                if (images != null && images.Count > 0)
                {
                    var imageUrls = new List<string>();
                    
                    // Giới hạn tối đa 5 hình
                    var maxImages = Math.Min(images.Count, 5);
                    
                    for (int i = 0; i < maxImages; i++)
                    {
                        var image = images[i];
                        if (image.Length > 0)
                        {
                            // Tạo tên file unique
                            var fileName = $"review_{productId}_{DateTime.Now.Ticks}_{i}_{Path.GetExtension(image.FileName)}";
                            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "reviews");
                            
                            // Tạo thư mục nếu chưa tồn tại
                            Directory.CreateDirectory(uploadsPath);
                            
                            var filePath = Path.Combine(uploadsPath, fileName);
                            
                            // Lưu file
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            
                            imageUrls.Add($"/images/reviews/{fileName}");
                        }
                    }
                    
                    // Lưu tất cả URL ảnh vào AnhDanhGia (ghép bằng dấu ';')
                    if (imageUrls.Count > 0)
                    {
                        danhGia.AnhDanhGia = string.Join(";", imageUrls);
                    }
                }

                _context.DanhGia.Add(danhGia);
                await _context.SaveChangesAsync();

                // Redirect đến trang cảm ơn
                TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
                return RedirectToAction("CamOnDanhGia", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index", new { id = productId });
            }
        }

        public async Task<IActionResult> CamOnDanhGia(int productId)
        {
            var sanPham = await _context.SanPham.FindAsync(productId);
            ViewBag.ProductName = sanPham?.TenSanPham ?? "Sản phẩm";
            ViewBag.ProductId = productId;
            return View();
        }

        public async Task<IActionResult> LaySanPhamGoiY(int productId)
        {
            try
            {
                var sanPham = await _context.SanPham.FindAsync(productId);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var sanPhamGoiY = await _context.SanPham
                    .Include(s => s.AnhSanPhams)
                    // Tạm comment vì chưa có bảng DanhGia
                    // .Include(s => s.DanhGias)
                    .Where(s => s.ID_DanhMuc == sanPham.ID_DanhMuc && 
                               s.ID_SanPham != productId && 
                               s.TrangThai == true)
                    .Take(4)
                    .Select(s => new
                    {
                        s.ID_SanPham,
                        s.TenSanPham,
                        s.GiaBan,
                        AnhChinh = s.AnhSanPhams.FirstOrDefault(a => a.LoaiAnh == "chinh").DuongDan,
                        // Tạm thời set giá trị mặc định
                        DiemDanhGia = 0, // s.DanhGias.Any() ? s.DanhGias.Average(d => d.SoSao) : 0,
                        SoLuongDanhGia = 0 // s.DanhGias.Count()
                    })
                    .ToListAsync();

                return Json(new { success = true, products = sanPhamGoiY });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
