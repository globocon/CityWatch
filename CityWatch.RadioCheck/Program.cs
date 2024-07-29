using CityWatch.Common.Services;
using CityWatch.Data;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.RadioCheck.Helpers;
using CityWatch.RadioCheck.Services;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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

builder.Services.Configure<Settings>(Configuration.GetSection(Settings.Name));
builder.Services.Configure<EmailOptions>(Configuration.GetSection(EmailOptions.Email));
builder.Services.AddScoped<IClientDataProvider, ClientDataProvider>();
builder.Services.AddScoped<IKpiDataProvider, KpiDataProvider>();
builder.Services.AddScoped<IImportJobDataProvider, ImportJobDataProvider>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IKpiSchedulesDataProvider, KpiSchedulesDataProvider>();
builder.Services.AddScoped<IDropboxService, DropboxService>();
builder.Services.AddScoped<IIrDataProvider, IrDataProvider>();
builder.Services.AddScoped<IPatrolDataReportService, PatrolDataReportService>();
builder.Services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
builder.Services.AddScoped<IGuardDataProvider, GuardDataProvider>();
builder.Services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IRadioChecksActivityStatusService, CityWatch.RadioCheck.Services.RadioChecksActivityStatusService>();
builder.Services.AddScoped<IUserDataProvider, UserDataProvider>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ISmsSenderProvider, SmsSenderProvider>();
builder.Services.AddScoped<ISiteEventLogDataProvider, SiteEventLogDataProvider>();
builder.Services.AddScoped<ISmsGlobalService, SmsGlobalService>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IViewDataService, CityWatch.RadioCheck.Services.ViewDataService>();
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IClientSiteViewDataService, CityWatch.RadioCheck.Services.ClientSiteViewDataService>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IAuditLogViewDataService, CityWatch.RadioCheck.Services.AuditLogViewDataService>();

builder.Services.AddScoped<IClientSiteWandDataProvider, ClientSiteWandDataProvider>();
builder.Services.AddScoped<IGuardLoginDetailService, GuardLoginDetailService>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IGuardLogReportGenerator, CityWatch.RadioCheck.Services.GuardLogReportGenerator>();
builder.Services.AddScoped<CityWatch.RadioCheck.Services.IGuardLogZipGenerator, CityWatch.RadioCheck.Services.GuardLogZipGenerator>();
builder.Services.AddScoped<ILogbookDataService, LogbookDataService>();

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
builder.Services.AddSignalR();

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