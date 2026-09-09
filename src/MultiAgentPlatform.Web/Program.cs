using Microsoft.AspNetCore.Identity;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Infrastructure;
using MultiAgentPlatform.Infrastructure.Identity;
using MultiAgentPlatform.Infrastructure.Persistence;
using MultiAgentPlatform.Web.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks().AddDbContextCheck<MainDbContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        // No EF Core migrations are checked into the repository yet, so Migrate() will throw
        // for a fresh database and we intentionally fall back to EnsureCreated(). Replace this
        // with a real migration bundle once one exists.
        try
        {
            db.Database.Migrate();
        }
        catch (Exception migrateEx)
        {
            logger.LogWarning(migrateEx, "Database migration failed or no migrations exist; falling back to EnsureCreated().");
            db.Database.EnsureCreated();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Enum.GetNames<PlatformRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!userManager.Users.Any())
        {
            var adminPassword = app.Configuration["Seed:AdminPassword"] ?? "ChangeMe!123";
            var admin = new ApplicationUser
            {
                UserName = "admin@platform.local",
                Email = "admin@platform.local",
                DisplayName = "Platform Administrator",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, nameof(PlatformRole.PlatformAdministrator));
            }
            else
            {
                logger.LogWarning("Failed to seed default admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Startup database initialization/seeding failed; continuing without it.");
    }
}

app.Run();
