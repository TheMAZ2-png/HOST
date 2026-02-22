using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI;
using HOST.Data;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// LOGGING CONFIGURATION (REQUIRED FOR ASSIGNMENT)
// ----------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ----------------------------------------------------
// RAZOR PAGES + ANONYMOUS ACCESS FOR /Parties/Index
// ----------------------------------------------------
builder.Services.AddRazorPages();

builder.Services.AddRazorPages(options =>
{
    // Allow anonymous access to the Parties Index page
    options.Conventions.AllowAnonymousToPage("/Parties/Index");
});

// ----------------------------------------------------
// DATABASE + IDENTITY CONFIGURATION
// ----------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HOST")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;

})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// ----------------------------------------------------
// COOKIE SETTINGS
// ----------------------------------------------------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Index";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogout = context =>
    {
        context.Response.Redirect("/Index");
        return Task.CompletedTask;
    };
});

// ----------------------------------------------------
// GLOBAL AUTHORIZATION POLICY
// ----------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// ----------------------------------------------------
// GLOBAL EXCEPTION HANDLING (REQUIRED FOR ASSIGNMENT)
// ----------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

// IMPORTANT: Razor Pages are mapped normally.
// FallbackPolicy still applies, but our AllowAnonymous override works.
app.MapRazorPages()
   .WithStaticAssets();

// ----------------------------------------------------
// ROLE SEEDING (Manager + Guest)
// ----------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Existing seeding
        await DbSeeder.SeedRolesAndAdmin(services);

        // Add Guest role if missing
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Guest"))
        {
            await roleManager.CreateAsync(new IdentityRole("Guest"));
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
