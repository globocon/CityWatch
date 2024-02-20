using CityWatch.Common.Services;
using CityWatch.Data;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

var Configuration = builder.Configuration;
// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CityWatchDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IClientDataProvider, ClientDataProvider>();
builder.Services.AddScoped<IViewDataService, ViewDataService>();
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();
builder.Services.AddScoped<IKpiDataProvider, KpiDataProvider>();
builder.Services.AddScoped<IImportJobDataProvider, ImportJobDataProvider>();
builder.Services.AddScoped<IImportDataService, ImportDataService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IKpiSchedulesDataProvider, KpiSchedulesDataProvider>();
builder.Services.AddScoped<ICleanupService, CleanupService>();
builder.Services.AddScoped<ISendScheduleService, SendScheduleService>();
builder.Services.AddScoped<IReportUploadService, ReportUploadService>();
builder.Services.AddScoped<IDropboxService, DropboxService>();
builder.Services.AddScoped<IIrDataProvider, IrDataProvider>();
builder.Services.AddScoped<IPatrolDataReportService, PatrolDataReportService>();
builder.Services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
builder.Services.AddScoped<IGuardDataProvider, GuardDataProvider>();
builder.Services.AddSession();
builder.Services.AddRazorPages(options =>
{
    // AllowAnonymousToFolder("/") given for allowing guard can access the KPI Dashboard 
    options.Conventions.AllowAnonymousToFolder("/");
    options.Conventions.AuthorizeFolder("/Develop");
    options.Conventions.AllowAnonymousToFolder("/Account");
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
});
builder.Services.AddHttpContextAccessor();


var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();