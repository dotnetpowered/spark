﻿using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi.Models;
using Spark.Engine;
using Spark.Engine.Extensions;
using Spark.Web.Data;
using Spark.Engine.Formatters;
using Spark.Web.Services;

namespace Spark.Web
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Bind to Spark and store settings from appSettings.json
            SparkSettings sparkSettings = new SparkSettings();
            Configuration.Bind("SparkSettings", sparkSettings);
            services.AddSingleton<SparkSettings>(sparkSettings);

            StoreSettings storeSettings = new StoreSettings();
            Configuration.Bind("StoreSettings", storeSettings);

            // Configure cookie policy
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/fhir+json", "application/fhir+xml" });
            });

            // Add database context for user administration
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection"))
            );

            // Add Identity management
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddRoles<IdentityRole>()
                .AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddAuthorization();

            // Set up a default policy for CORS that accepts any origin, method and header.
            // only for test purposes.
            services.AddCors(options =>
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                }));

            // AddFhir also calls AddMvcCore
            services.AddFhir(sparkSettings);

            services.AddTransient<ServerMetadata>();

            // AddMvc needs to be called since we are using a Home page that is reliant on the full MVC framework
            services.AddMvc(options =>
            {
                options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
                options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
                // We remove StringOutputFormatter to make Swagger happy by not 
                // showing text/plain in the list of available media types.
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.EnableEndpointRouting = false;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Spark API", Version = "v1" });
            });
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Spark API");
            });

            app.UseAuthentication();
            app.UseCors();

            app.UseRouting();


            // UseFhir also calls UseMvc
            app.UseFhir(r => r.MapRoute(name: "default", template: "{controller}/{action}/{id?}", defaults: new { controller = "Home", action = "Index" }));
        }
    }
}