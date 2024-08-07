using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data;
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;


var builder = WebApplication.CreateBuilder(args);
var Configuration = builder.Configuration;

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CityWatchDbContext>(options => options.UseSqlServer(connectionString).LogTo(Console.WriteLine, LogLevel.Information));
builder.Services.Configure<Settings>(Configuration.GetSection(Settings.Name));
builder.Services.Configure<EmailOptions>(Configuration.GetSection(EmailOptions.Email));
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IClientDataProvider, ClientDataProvider>();
builder.Services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
builder.Services.AddScoped<IViewDataService, ViewDataService>();
builder.Services.AddScoped<IIrDataProvider, IrDataProvider>();
builder.Services.AddScoped<IUserDataProvider, UserDataProvider>();
builder.Services.AddScoped<ICleanupService, CleanupService>();
builder.Services.AddScoped<IIncidentReportGenerator, IncidentReportGenerator>();
builder.Services.AddScoped<IAppConfigurationProvider, AppConfigurationProvider>();
builder.Services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
builder.Services.AddScoped<IGuardSettingsDataProvider, GuardSettingsDataProvider>();
builder.Services.AddScoped<IGuardLogReportGenerator, GuardLogReportGenerator>();
builder.Services.AddScoped<IGuardDataProvider, GuardDataProvider>();
builder.Services.AddScoped<IIrUploadService, IrUploadService>();
builder.Services.AddScoped<IClientSiteWandDataProvider, ClientSiteWandDataProvider>();
builder.Services.AddScoped<ISiteLogUploadService, SiteLogUploadService>();
builder.Services.AddScoped<IKeyVehicleLogReportGenerator, KeyVehicleLogReportGenerator>();
builder.Services.AddScoped<IPatrolDataReportService, PatrolDataReportService>();
builder.Services.AddScoped<IGuardLogZipGenerator, GuardLogZipGenerator>();
builder.Services.AddScoped<IDropboxService, DropboxService>();
builder.Services.AddScoped<IGuardLoginDetailService, GuardLoginDetailService>();
builder.Services.AddScoped<IDropboxMonitorService, DropboxMonitorService>();
builder.Services.AddScoped<IKeyVehicleLogDocketGenerator, KeyVehicleLogDocketGenerator>();
builder.Services.AddScoped<IAuditLogViewDataService, AuditLogViewDataService>();
builder.Services.AddScoped<IClientSiteViewDataService, ClientSiteViewDataService>();
builder.Services.AddScoped<IClientSiteRadioStatusDataProvider, ClientSiteRadioStatusDataProvider>();
builder.Services.AddScoped<IGuardReminderService, GuardReminderService>();
builder.Services.AddScoped<IClientSiteActivityStatusDataProvider, ClientSiteActivityStatusDataProvider>();
builder.Services.AddScoped<IRadioCheckViewDataService, RadioCheckViewDataService>();
builder.Services.AddScoped<IRadioChecksActivityStatusService, RadioChecksActivityStatusService>();
builder.Services.AddScoped<ISiteEventLogDataProvider, SiteEventLogDataProvider>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ISmsSenderProvider, SmsSenderProvider>();
builder.Services.AddScoped<ISmsGlobalService, SmsGlobalService>();
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
builder.Services.AddScoped<ITimesheetReportGenerator, TimesheetReportGenerator>();
builder.Services.AddScoped<ILogbookDataService, LogbookDataService>();


builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AuthorizeFolder("/Incident");
    options.Conventions.AuthorizeFolder("/Reports");
    options.Conventions.AuthorizeFolder("/Develop");
    options.Conventions.AuthorizeFolder("/Radio");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

//builder.Services.Configure<CookiePolicyOptions>(options =>
//{

//    builder.Services.AddHttpContextAccessor();
//    builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

//});

//builder.Services.AddHttpContextAccessor();

//builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();    
}

app.UseCors("AllowSpecificOrigin");
AuthUserHelper.Configure(app.Services.GetService<IHttpContextAccessor>());
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
// Configure the HTTP request pipeline.
app.MapHub<UpdateHub>("/updateHub");
app.Run();