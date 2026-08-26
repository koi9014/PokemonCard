using Microsoft.AspNetCore.Authentication.Cookies; // [管理員登入系統新增] 使用 Cookie 驗證
using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ===== [違禁字庫管理新增] 註冊違禁字比對服務，供後台與未來前台審查流程共用 =====
builder.Services.AddScoped<BannedWordReviewService>();

// ===== [登入驗證系統整合開始] 一般會員使用預設 Cookie，管理員使用 AdminCookie =====
// CookieAuthenticationDefaults.AuthenticationScheme 是一般會員預設登入方案。
// AdminCookie 僅供管理員後台使用，避免管理員與一般會員登入狀態混用。
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/UserLogin/Login";
        options.AccessDeniedPath = "/UserLogin/Login";
    })
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/AdminLogin";
        options.LogoutPath = "/AdminLogin/Logout";
        options.AccessDeniedPath = "/AdminLogin";
        options.Cookie.Name = "PokemonCard.Admin";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
// ===== [登入系統新增結束] =====

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
    pattern: "{controller=AdminLogin}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
