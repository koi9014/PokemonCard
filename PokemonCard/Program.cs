using Microsoft.AspNetCore.Authentication.Cookies; // [管理員登入系統新增] 使用 Cookie 驗證
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// ===== [管理員登入系統新增開始] 註冊管理員 Cookie 登入設定 =====
// AdminCookie 僅供管理員後台使用，之後一般會員登入可再新增獨立 Cookie，避免權限混用。
builder.Services.AddAuthentication()
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/AdminLogin";
        options.LogoutPath = "/AdminLogin/Logout";
        options.AccessDeniedPath = "/AdminLogin";
        options.Cookie.Name = "PokemonCard.Admin";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
// ===== [管理員登入系統新增結束] =====

builder.Services.AddDbContext<PicartchuContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("PicartchuConnection")
    )
);
var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ===== [管理員登入系統新增開始] 啟用登入 Cookie 讀取 =====
// UseAuthentication 必須放在 UseAuthorization 前，系統才會先還原登入身分再判斷授權。
app.UseAuthentication();
// ===== [管理員登入系統新增結束] =====

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
