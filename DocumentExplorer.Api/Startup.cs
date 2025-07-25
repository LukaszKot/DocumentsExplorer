﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DocumentExplorer.Infrastructure.Mappers;
using DocumentExplorer.Infrastructure.IoC.Modules;
using DocumentExplorer.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DocumentExplorer.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using DocumentExplorer.Infrastructure.Mongo;
using DocumentExplorer.Api.Framework;
using Microsoft.Extensions.Logging;
using DocumentExplorer.Infrastructure.EF;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace DocumentExplorer.Api
{
    public class Startup
    {
        public IConfiguration ConfigurationRoot { get; }
        public IContainer ApplicationContainer { get; private set; }

        public Startup(IConfiguration configuration)
        {
            ConfigurationRoot = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = ConfigurationRoot["jwt:issuer"],
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationRoot["jwt:key"])),
                SaveSigninToken = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            });


            services.AddSingleton(serviceProvider => AutoMapperConfig.Initialize(serviceProvider.GetRequiredService<ILoggerFactory>()));
            services.AddMemoryCache();
            services.AddAuthorization(x => x.AddPolicy("admin", p=>p.RequireRole("admin")));
            services.AddAuthorization(x => x.AddPolicy("user", p=>p.RequireRole("user")));
            services.AddAuthorization(x => x.AddPolicy("complementer", p=>p.RequireRole("complementer")));
            services.AddAuthorization(x => x.AddPolicy("complementerAndAdmin", p=>p.RequireRole("complementer", "admin")));
            services.AddTransient<TokenManagerMiddleware>();
            services.AddTransient<ITokenManager,TokenManager>();
            services.AddSingleton<IHttpContextAccessor,HttpContextAccessor>();
            services.AddMvc();
            if(ConfigurationRoot["general:Database"]=="SqlOrInMemory")
            {
                services.AddEntityFrameworkSqlServer().AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<DocumentExplorerContext>();
            }
            var builder = new ContainerBuilder();
            builder.Populate(services);
            if(ConfigurationRoot["general:Database"]=="SqlOrInMemory")
            {
                builder.RegisterModule<SqlModule>();
            }
            if(ConfigurationRoot["general:Database"]=="Mongo")
            {
                builder.RegisterModule<MongoModule>();
            }
            if(ConfigurationRoot["general:FileRepository"]=="FileSystem")
            {
                builder.RegisterModule<FileSystemModule>();
            }
            if(ConfigurationRoot["general:FileRepository"]=="BlobStorage")
            {
                builder.RegisterModule<BlobStorageModule>();
            }
            builder.RegisterModule<ServiceModule>();
            builder.RegisterModule(new SettingsModule(ConfigurationRoot));
            builder.RegisterModule<CommandModule>();
            builder.RegisterType<Handler>().As<IHandler>().InstancePerDependency();
            ApplicationContainer = builder.Build();

            return new AutofacServiceProvider(ApplicationContainer);
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime,
            ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<TokenManagerMiddleware>();
            app.UseAuthentication();
            app.UseExceptionMiddleware();
            MongoConfigurator.Initialize();
            var generalSettings = app.ApplicationServices.GetService<GeneralSettings>();
            if(generalSettings.DataInitialize)
            {
                var dataInitilizer = app.ApplicationServices.GetService<IDataInitializer>();
                dataInitilizer.SeedAsync();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            appLifetime.ApplicationStopped.Register(()=> ApplicationContainer.Dispose());
        }
    }
}
