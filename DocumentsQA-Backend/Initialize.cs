using System.Net;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

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
				services.AddDbContext<DataContext>(options =>
					//options.UseSqlServer(_configuration.GetConnectionString("DocumentsDB")));
					options.UseSqlServer(_configuration.GetConnectionString("TempLocalDB")));

				services.AddIdentity<AppUser, AppRole>()
					.AddEntityFrameworkStores<DataContext>()
					.AddDefaultTokenProviders();

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

				services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
					.AddJwtBearer(options => {
						options.TokenValidationParameters = new TokenValidationParameters {
							ValidateIssuerSigningKey = true,
							ValidateIssuer = false,
							ValidateAudience = false,
							ValidateLifetime = true,
							// Use JWT key HS384 when testing
							IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtKey)),
							ClockSkew = TimeSpan.Zero,
						};
					});
			}
			else {
				services.AddSingleton<IAuthorizationHandler, AuthorizationAllowAnonymous>();
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
}
