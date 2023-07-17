using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public class DataContext : IdentityDbContext {
		public DataContext(DbContextOptions<DataContext> options) : base(options) {
		}

		

		//--------------------------------------------------------------------------

		

		//--------------------------------------------------------------------------

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			// Register DB functions
			{
				// -----------------------------------------------------
				// Scalar DB functions

				

				// -----------------------------------------------------
				// Table DB functions

				
			}

			base.OnModelCreating(modelBuilder);
		}

		//--------------------------------------------------------------------------

		public async Task<int> SetIdentityInsert(string table, bool on) {
			string dboTable = $"[dbo].[{table}]";
			string setArg = on ? "on" : "off";
			return await Database.ExecuteSqlRawAsync($"set IDENTITY_INSERT {dboTable} {setArg}");
		}
	}
}
