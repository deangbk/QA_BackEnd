using System.Net;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Helpers;

namespace DocumentsQA_Backend {
	public class Initialize {
		private readonly IConfiguration _configuration;
		private bool useAuthorization;

		public static bool IsDevelopment { get; set; } = true;

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
					options.UseSqlServer(_configuration.GetConnectionString("DocumentsDB")));
			}

			services.AddCors();

			if (useAuthorization) {
				services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
					.AddJwtBearer(options => {
						options.TokenValidationParameters = new TokenValidationParameters {
							ValidateIssuerSigningKey = true,
							ValidateIssuer = false,
							ValidateAudience = false,
							ValidateLifetime = false,
							// Use JWT key HS384 when testing
							IssuerSigningKey = new SymmetricSecurityKey(
								Encoding.ASCII.GetBytes(_configuration.GetSection("AppSettings:Token").Value!)),
							ClockSkew = TimeSpan.Zero,
						};
					});
			}
			else {
				services.AddSingleton<IAuthorizationHandler, AuthorizationAllowAnonymous>();
			}

			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();
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
