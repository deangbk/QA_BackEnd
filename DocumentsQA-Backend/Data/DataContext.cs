using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public class DataContext : IdentityDbContext<EDUser> {
		public DataContext(DbContextOptions<DataContext> options) : base(options) {
		}



		//--------------------------------------------------------------------------

		public DbSet<Project> Projects { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Document> Documents { get; set; }

		public DbSet<Tranche> Tranches { get; set; }
		public DbSet<ProjectUserAccess> ProjectUserAccesses { get; set; }

		//--------------------------------------------------------------------------

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			// Configure model relationships
			ConfigureRelationships(modelBuilder);

			// Register DB functions
			{
				// -----------------------------------------------------
				// Scalar DB functions

				

				// -----------------------------------------------------
				// Table DB functions

				
			}

			base.OnModelCreating(modelBuilder);
		}

		private void ConfigureRelationships(ModelBuilder modelBuilder) {
			// Model: Project
			{
				// Map many-to-many using join entity class
				modelBuilder.Entity<Project>()
					.HasMany(e => e.UserAccesses)
					.WithMany(e => e.ProjectAccesses)
					.UsingEntity<ProjectUserAccess>();

				modelBuilder.Entity<Project>()
					.HasMany(e => e.Tranches)
					.WithOne()
					.HasForeignKey(e => e.ProjectId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
			}

			// Model: User
			{
				modelBuilder.Entity<EDUser>()
					.HasOne(e => e.FavouriteProject)
					.WithMany()
					.HasForeignKey(e => e.FavouriteProjectId)
					.IsRequired(false);
			}

			// Model: Question
			{
				modelBuilder.Entity<Question>()
					.HasOne(e => e.PostedBy)
					.WithMany()
					.HasForeignKey(e => e.PostedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
				modelBuilder.Entity<Question>()
					.HasOne(e => e.AnsweredBy)
					.WithMany()
					.HasForeignKey(e => e.AnsweredById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);

				modelBuilder.Entity<Question>()
					.HasOne(e => e.QuestionApprovedBy)
					.WithMany()
					.HasForeignKey(e => e.QuestionApprovedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);
				modelBuilder.Entity<Question>()
					.HasOne(e => e.AnswerApprovedBy)
					.WithMany()
					.HasForeignKey(e => e.AnswerApprovedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);

				modelBuilder.Entity<Question>()
					.HasOne(e => e.LastEditor)
					.WithMany()
					.HasForeignKey(e => e.LastEditorId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
			}

			// Model: Document
			{
				modelBuilder.Entity<Document>()
					.HasOne(e => e.UploadedBy)
					.WithMany()
					.HasForeignKey(e => e.UploadedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				modelBuilder.Entity<Document>()
					.HasOne(e => e.AssocQuestion)
					.WithMany()
					.HasForeignKey(e => e.AssocQuestionId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);
				modelBuilder.Entity<Document>()
					.HasOne(e => e.AssocUser)
					.WithMany()
					.HasForeignKey(e => e.AssocUserId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);
			}
		}

		//--------------------------------------------------------------------------

		public async Task<int> SetIdentityInsert(string table, bool on) {
			string dboTable = $"[dbo].[{table}]";
			string setArg = on ? "on" : "off";
			return await Database.ExecuteSqlRawAsync($"set IDENTITY_INSERT {dboTable} {setArg}");
		}
	}
}
