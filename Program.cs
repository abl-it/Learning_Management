using KPI.Services;
using Training.Data;
using Training.Filters;
using Training.Middleware;
using Training.Services;
using Training.Services.IServices;

var builder = WebApplication.CreateBuilder(args);
// Add configuration for connection string
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
//builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("ConnectionStrings"));
// Bind ConnectionStrings section to DatabaseSettings
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("ConnectionStrings"));


// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); // Optional: for development
builder.Services.AddHttpContextAccessor(); // Register IHttpContextAccessor

// Add session services
builder.Services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true; // Ensure the session cookie is HTTP-only
    options.Cookie.IsEssential = true; // Mark cookie as essential
    options.Cookie.SameSite = SameSiteMode.Lax; // Adjust as needed
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Adjust as needed
    options.IdleTimeout = TimeSpan.FromMinutes(10);

});

// Register our custom services
builder.Services.AddScoped<ProfileAttribute>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IPlanningService, PlanningService>();
builder.Services.AddScoped<IInitialService, InitialService>();

// Configure anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Employee/Home");
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseMiddleware<SessionTokenMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
