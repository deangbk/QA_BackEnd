﻿using System.Net;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Repository;

namespace DocumentsQA_Backend {
	public class Initialize {
		private readonly IConfiguration _configuration;
		private bool useAuthorization;

		public static bool IsDevelopment { get; set; } = true;
		public static string JwtKey { get; set; } = string.Empty;

		public Initialize(IConfiguration configuration) {
			_configuration = configuration;

			{
				var authoEnable = _configuration.GetSection("AppSettings:UseAuthorization").Value;

				if (!bool.TryParse(authoEnable, out useAuthorization))
					useAuthorization = false;
			}
		}

		// Called by runtime
		public void ConfigureServices(IServiceCollection services) {
			services
				.AddControllers(options => {
					options.Filters.Add<ModelValidationActionFilter>();
				});
				/*
				.AddNewtonsoftJson(options => {
					options.SerializerSettings.ContractResolver = new RequiredPropertiesContractResolver();
				});
				*/

			{
				services.AddDbContext<DataContext>(options => {
					//options.UseSqlServer(_configuration.GetConnectionString("DocumentsDB")));
					options.UseLazyLoadingProxies();
					options.UseSqlServer(_configuration.GetConnectionString("TempLocalDB"));
				});

				services.AddIdentity<AppUser, AppRole>()
					.AddEntityFrameworkStores<DataContext>()
					.AddSignInManager<SignInManager<AppUser>>()
					.AddDefaultTokenProviders();

				services.AddIdentityCore<AppUser>()
					.AddRoles<AppRole>()
					.AddEntityFrameworkStores<DataContext>();

				services.Configure<IdentityOptions>(options => {
					// User settings

					options.User.AllowedUserNameCharacters += ':';
					options.User.RequireUniqueEmail = true;

					// Password settings

					options.Password.RequiredLength = 6;
					options.Password.RequiredUniqueChars = 2;

					options.Password.RequireNonAlphanumeric = false;
					options.Password.RequireLowercase = true;
					options.Password.RequireUppercase = false;
					options.Password.RequireDigit = true;

					// Lockout settings

					options.Lockout.MaxFailedAccessAttempts = 99999;
				});
			}

			services.AddCors(options => {
				options.AddDefaultPolicy(
					policy => {
						policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
					});
			});

			if (useAuthorization) {
				JwtKey = _configuration.GetSection("AppSettings:Token").Value!;

				services.AddAuthentication(options => {
					options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				})
					.AddJwtBearer(options => {
						options.TokenValidationParameters = new TokenValidationParameters {
							// Don't care about these
							ValidateIssuer = false,
							ValidateAudience = false,

							// Validate signing key
							ValidateIssuerSigningKey = true,
							IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtKey)),

							// Don't allow any time difference, the expiration time is already large enough
							ValidateLifetime = true,
							ClockSkew = TimeSpan.Zero,
						};
					});

				/*
				services.AddAuthorization(options => {
					options.AddPolicy("IsAdmin", 
						policy => policy.RequireClaim("role", AppRole.Admin.Name));
					options.AddPolicy("IsManager", 
						policy => policy.RequireClaim("role", AppRole.Manager.Name));
					options.AddPolicy("IsStaff", 
						policy => policy.RequireClaim("role", AppRole.Admin.Name, AppRole.Manager.Name));
				});
				*/

				services.AddScoped<IAccessService, AccessService>();
			}
			else {
				services.AddTransient<IAuthorizationHandler, AuthorizationAllowAnonymous>();
				services.AddTransient<IAccessService, AccessAllowAll>();
			}

			// Register repository services
			{
				services.AddTransient<IProjectRepository, ProjectRepository>();
			}

			// Register email services
			{
				services.AddHostedService<ConsumeScopedServiceHostedService>();
				services.AddScoped<IScopedProcessingService, EmailService>();
				services.AddScoped<IEmailService, EmailService>();
			}

			// Register file manager service
			services.AddScoped<IFileManagerService, InProjectRootFileManager>();

			// -----------------------------------------------------------

			services.AddHttpContextAccessor();

			services.AddEndpointsApiExplorer();
			//services.AddSwaggerGen();
		}

		// Called by runtime
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			IsDevelopment = env.IsDevelopment();
			if (IsDevelopment) {
				app.UseDeveloperExceptionPage();
			}

			// Configure middlewares
			{
			app.UseAuthentication();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

				// Custom middlewares

				app.UseMiddleware<ExceptionMiddleware>();
				app.UseMiddleware<ProjectAccessMiddleware>();
			}

			app.UseCors();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}
}
