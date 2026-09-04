using Document_Management.Data;
using Document_Management.Repository;
using Document_Management.Service;
using Document_Management.Utility;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


//// Load configuration based on the environment
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

//New added middleware
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

//DI
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<IDmsAccessService, DmsAccessService>();
builder.Services.AddScoped<IDmsQueryService, DmsQueryService>();
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<IDmsSearchService, DmsSearchService>();
builder.Services.AddScoped<ICloudStorageService, GoogleCloudStorageService>();
builder.Services.AddScoped<IDocumentStorageWorkflowService, DocumentStorageWorkflowService>();
builder.Services.AddScoped<IPdfUploadValidationService, PdfUploadValidationService>();
builder.Services.AddScoped<IPdfTextExtractionService, PdfTextExtractionService>();
builder.Services.AddScoped<IDocumentOcrService, DocumentOcrService>();
builder.Services.AddScoped<CloudStorageMigrationService>();

var ocrWorkerEnabled = builder.Configuration.GetValue("OcrWorker:Enabled", true);
var ocrExecutionMode = builder.Configuration["OcrWorker:ExecutionMode"] ?? "Web";
if (ocrWorkerEnabled && string.Equals(ocrExecutionMode, "Web", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<DocumentOcrBackgroundService>();
}

if (builder.Environment.IsProduction())
{
    var bucketName = builder.Configuration["GoogleCloudStorage:BucketName"]!;
    var storageClient = StorageClient.Create();

    builder.Services.AddDataProtection()
        .SetApplicationName("IBS-Web")
        .AddKeyManagementOptions(options =>
        {
            options.XmlRepository = new GcsXmlRepository(
                storageClient,
                bucketName,
                "dataprotection-keys.xml"
            );
        });
}

var app = builder.Build();

await ApplicationDbSeeder.SeedAsync(app.Services);

if (string.Equals(ocrExecutionMode, "Job", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var documentOcrService = scope.ServiceProvider.GetRequiredService<IDocumentOcrService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OcrJob");
    var maxDocumentsPerRun = builder.Configuration.GetValue("OcrWorker:MaxDocumentsPerRun", 10);
    var processedCount = await documentOcrService.ProcessPendingDocumentsAsync(maxDocumentsPerRun, CancellationToken.None);
    logger.LogInformation("OCR job completed. Processed {ProcessedCount} document(s).", processedCount);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// This code is to change the behavior of timestamp of postgresql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseMiddleware<MaintenanceMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Additional routes for your DMS controller
app.MapControllerRoute(
    name: "dmsRoutes",
    pattern: "Dms/{action=Index}/{id?}",
    defaults: new { controller = "Dms" });

// Route for file downloads
app.MapControllerRoute(
    name: "fileDownload",
    pattern: "Dms/Download/{*filepath}",
    defaults: new { controller = "Dms", action = "Download" });

// Route for displaying files with complex parameters
app.MapControllerRoute(
    name: "displayFiles",
    pattern: "Dms/DisplayFiles/{companyFolderName}/{yearFolderName}/{departmentFolderName}/{documentTypeFolderName}/{subCategoryFolder?}",
    defaults: new { controller = "Dms", action = "DisplayFiles" });

app.Run();
