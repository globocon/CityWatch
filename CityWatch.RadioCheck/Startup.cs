using CityWatch.Common.Services;
using CityWatch.Data;
using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.RadioCheck
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
            
            services.Configure<EmailOptions>(Configuration.GetSection(EmailOptions.Email));
            services.AddScoped<IClientDataProvider, ClientDataProvider>();           
            services.AddScoped<IKpiDataProvider, KpiDataProvider>();
            services.AddScoped<IImportJobDataProvider, ImportJobDataProvider>();
            services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
            services.AddScoped<IKpiSchedulesDataProvider, KpiSchedulesDataProvider>();             
            services.AddScoped<IDropboxService, DropboxService>();
            services.AddScoped<IIrDataProvider, IrDataProvider>();
            services.AddScoped<IPatrolDataReportService, PatrolDataReportService>();
            services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
            services.AddScoped<IGuardDataProvider, GuardDataProvider>();
            services.AddScoped<IGuardDataProvider, GuardDataProvider>();
            services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
            


            services.AddRazorPages(options => 
            {
                // AllowAnonymousToFolder("/") given for allowing guard can access the KPI Dashboard 
                options.Conventions.AllowAnonymousToFolder("/");
                options.Conventions.AuthorizeFolder("/Develop");
                options.Conventions.AllowAnonymousToFolder("/Account");
            });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
            });
            services.AddDbContext<CityWatchDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
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
