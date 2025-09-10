using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoFrame.Web.Data;
using PhotoFrame.Data;
using PhotoFrame.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("IdentityConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => { 
    options.UseSqlite(connectionString);
});

// Add PhotoFrame database context
builder.Services.AddDbContext<PhotoFrameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PhotoFrameConnection") ?? 
                     "Data Source=photos.db"));

// Add custom services
builder.Services.AddScoped<ImageProcessingService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ensure PhotoFrame database is created
using (var scope = app.Services.CreateScope())
{
    var photoContext = scope.ServiceProvider.GetRequiredService<PhotoFrameDbContext>();
    photoContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

