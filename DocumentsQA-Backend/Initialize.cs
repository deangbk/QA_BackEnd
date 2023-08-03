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
			services.AddControllers();

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
					// Password settings

					options.Password.RequiredLength = 6;
					options.Password.RequiredUniqueChars = 2;

					options.Password.RequireNonAlphanumeric = false;
					options.Password.RequireLowercase = true;
					options.Password.RequireUppercase = false;
					options.Password.RequireDigit = true;

					// Lockout settings

					options.Lockout.MaxFailedAccessAttempts = 10;
				});
			}

			services.AddCors();

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
				services.ConfigureApplicationCookie(options => {
					// Prevent login redirection on unauthorized
					options.Cookie.HttpOnly = true;
					options.LoginPath = "";
					options.AccessDeniedPath = "";
					options.Events.OnRedirectToLogin = context => {
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						return Task.CompletedTask;
					};
				});
				*/

				services.AddAuthorization(options => {
					options.AddPolicy("IsAdmin", 
						policy => policy.RequireClaim("role", AppRole.Admin.Name));
					options.AddPolicy("IsManager", 
						policy => policy.RequireClaim("role", AppRole.Manager.Name));
					options.AddPolicy("IsStaff", 
						policy => policy.RequireClaim("role", AppRole.Admin.Name, AppRole.Manager.Name));
				});

				services.AddScoped<AccessService, AccessService>();
			}
			else {
				services.AddSingleton<IAuthorizationHandler, AuthorizationAllowAnonymous>();
				services.AddSingleton<IAccessService, AccessAllowAll>();
			}

			services.AddEndpointsApiExplorer();
			//services.AddSwaggerGen();
		}

		// Called by runtime
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			IsDevelopment = env.IsDevelopment();
			if (IsDevelopment) {
				app.UseDeveloperExceptionPage();
			}
			else {
				app.UseExceptionHandler(builder => {
					builder.Run(async context => {
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						var error = context.Features.Get<IExceptionHandlerFeature>();
						if (error != null) {
							context.Response.AddApplicationError(error.Error.Message);
							await context.Response.WriteAsync(error.Error.Message);
						}
					});
				});
			}

			app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

			app.UseAuthentication();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllers();
			});
		}
	}

	public class AuthorizationAllowAnonymous : IAuthorizationHandler {
		public Task HandleAsync(AuthorizationHandlerContext context) {
			foreach (var requirement in context.PendingRequirements.ToList())
				context.Succeed(requirement);
			return Task.CompletedTask;
		}
	}
	public class AccessAllowAll : IAccessService {
		public Task<bool> AllowToProject(HttpContext ctx, Project project) => Task.FromResult(true);
		public Task<bool> AllowToTranche(HttpContext ctx, Tranche tranche) => Task.FromResult(true);
	}
}
