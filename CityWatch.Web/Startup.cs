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
using System;

namespace CityWatch.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Settings>(Configuration.GetSection(Settings.Name));
            services.Configure<EmailOptions>(Configuration.GetSection(EmailOptions.Email));
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IClientDataProvider, ClientDataProvider>();
            services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
            services.AddScoped<IViewDataService, ViewDataService>();
            services.AddScoped<IIrDataProvider, IrDataProvider>();
            services.AddScoped<IUserDataProvider, UserDataProvider>();
            services.AddScoped<ICleanupService, CleanupService>();
            services.AddScoped<IIncidentReportGenerator, IncidentReportGenerator>();
            services.AddScoped<IAppConfigurationProvider, AppConfigurationProvider>();
            services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
            services.AddScoped<IGuardSettingsDataProvider, GuardSettingsDataProvider>();
            services.AddScoped<IGuardLogReportGenerator, GuardLogReportGenerator>();
            services.AddScoped<IGuardDataProvider, GuardDataProvider>();
            services.AddScoped<IIrUploadService, IrUploadService>();
            services.AddScoped<IClientSiteWandDataProvider, ClientSiteWandDataProvider>();
            services.AddScoped<ISiteLogUploadService, SiteLogUploadService>();
            services.AddScoped<IKeyVehicleLogReportGenerator, KeyVehicleLogReportGenerator>();
            services.AddScoped<IPatrolDataReportService, PatrolDataReportService>();
            services.AddScoped<IGuardLogZipGenerator, GuardLogZipGenerator>();
            services.AddScoped<IDropboxService, DropboxService>();
            services.AddScoped<IGuardLoginDetailService, GuardLoginDetailService>();
            services.AddScoped<IDropboxMonitorService, DropboxMonitorService>();
            services.AddScoped<IKeyVehicleLogDocketGenerator, KeyVehicleLogDocketGenerator>();
            services.AddScoped<IAuditLogViewDataService, AuditLogViewDataService>();
            services.AddScoped<IClientSiteViewDataService, ClientSiteViewDataService>();
            services.AddScoped<IClientSiteRadioStatusDataProvider, ClientSiteRadioStatusDataProvider>();
            services.AddScoped<IGuardReminderService, GuardReminderService>();               
            services.AddScoped<IClientSiteActivityStatusDataProvider, ClientSiteActivityStatusDataProvider>();
            services.AddScoped<IRadioCheckViewDataService, RadioCheckViewDataService>();
            services.AddScoped<IRadioChecksActivityStatusService, RadioChecksActivityStatusService>();
            services.AddRazorPages(options => 
            {
                options.Conventions.AuthorizePage("/Index");
                options.Conventions.AuthorizeFolder("/Admin");
                options.Conventions.AuthorizeFolder("/Incident");
                options.Conventions.AuthorizeFolder("/Reports");
                options.Conventions.AuthorizeFolder("/Develop");
                options.Conventions.AuthorizeFolder("/Radio");
            });
            services.AddDbContext<CityWatchDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
            });
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            AuthUserHelper.Configure(app.ApplicationServices.GetService<IHttpContextAccessor>());

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
