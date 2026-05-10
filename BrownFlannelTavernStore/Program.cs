using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
});

builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<StoreDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StripeWebhookService>();

var app = builder.Build();

// Apply pending migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    db.Database.Migrate();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapPost("/api/stripe/webhook", async (HttpContext context, StripeWebhookService service, ILogger<Program> logger) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var json = await reader.ReadToEndAsync();
    var signature = context.Request.Headers["Stripe-Signature"].ToString();

    try
    {
        await service.HandleEventAsync(json, signature);
        return Results.Ok();
    }
    catch (Stripe.StripeException ex)
    {
        logger.LogError(ex, "Stripe webhook signature verification failed");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();
