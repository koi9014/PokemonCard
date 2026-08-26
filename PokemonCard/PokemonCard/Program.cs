using Microsoft.EntityFrameworkCore;
using PokemonCard.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication. CreateBuilder(args);

// Add services to the container.
builder. Services. AddControllersWithViews( );

// 加入 Cookie 登入驗證（登入後才能存取會員功能）
builder. Services. AddAuthentication(CookieAuthenticationDefaults. AuthenticationScheme)
    . AddCookie(options =>
    {
        options. LoginPath = "/UserLogin/Login";
        options. AccessDeniedPath = "/UserLogin/Login";
    });

builder. Services. AddDbContext<PicartchuContext>(
    options => options. UseSqlServer(
        builder. Configuration. GetConnectionString("PIcartchuConnstring")
    )
);
var app = builder. Build( );

// Configure the HTTP request pipeline.
if(!app. Environment. IsDevelopment( ))
{
    app. UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app. UseHsts( );
}

app. UseHttpsRedirection( );
app. UseRouting( );

app. UseAuthentication( );

app. UseAuthorization( );

app. MapStaticAssets( );

app. MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    . WithStaticAssets( );


app. Run( );
