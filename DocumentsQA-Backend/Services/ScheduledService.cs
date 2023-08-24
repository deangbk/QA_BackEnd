using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentsQA_Backend.Services {
	interface IScopedProcessingService {
		Task Work(CancellationToken stoppingToken);
	}

	public class ConsumeScopedServiceHostedService : BackgroundService {
		private readonly ILogger<ConsumeScopedServiceHostedService> _logger;

		public ConsumeScopedServiceHostedService(IServiceProvider services,
			ILogger<ConsumeScopedServiceHostedService> logger) {

			Services = services;

			_logger = logger;
		}

		public IServiceProvider Services { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
			_logger.LogInformation("ConsumeScopedServiceHostedService run.");

			await Work(stoppingToken);
		}

		private async Task Work(CancellationToken stoppingToken) {
			_logger.LogInformation("ConsumeScopedServiceHostedService work");

			using (var scope = Services.CreateScope()) {
				var scopedProcessingService = scope.ServiceProvider
					.GetRequiredService<IScopedProcessingService>();

				await scopedProcessingService.Work(stoppingToken);
			}
		}

		public override async Task StopAsync(CancellationToken stoppingToken) {
			_logger.LogInformation("ConsumeScopedServiceHostedService stop");

			await base.StopAsync(stoppingToken);
		}
	}
}
