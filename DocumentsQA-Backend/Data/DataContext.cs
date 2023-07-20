using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public class DataContext : IdentityDbContext<AppUser, AppRole, int> {
		public DataContext(DbContextOptions<DataContext> options) : base(options) {
		}

		public DbSet<Project> Projects { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Document> Documents { get; set; }
		public DbSet<Comment> Comments { get; set; }

		public DbSet<Tranche> Tranches { get; set; }
		public DbSet<ProjectUserAccess> ProjectUserAccesses { get; set; }

		//--------------------------------------------------------------------------



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
				// Map Project:Tranche as 1:N
				modelBuilder.Entity<Project>()
					.HasMany(e => e.Tranches)
					.WithOne()
					.HasForeignKey(e => e.ProjectId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				// Map Project:Question as 1:N
				modelBuilder.Entity<Project>()
					.HasMany(e => e.Questions)
					.WithOne(e => e.Project)
					.HasForeignKey(e => e.ProjectId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				// Map Project:User as N:M using join entity
				modelBuilder.Entity<Project>()
					.HasMany(e => e.UserAccesses)
					.WithMany(e => e.ProjectAccesses)
					.UsingEntity<ProjectUserAccess>();

				// Map Project:User as 1:N
				modelBuilder.Entity<Project>()
					.HasMany<AppUser>()
					.WithOne(e => e.FavouriteProject)
					.HasForeignKey(e => e.FavouriteProjectId)
					.OnDelete(DeleteBehavior.SetNull)
					.IsRequired(false);
			}

			// Model: Tranche
			{
				// Map Tranche:Question as 1:N
				modelBuilder.Entity<Tranche>()
					.HasMany<Question>()
					.WithOne(e => e.Tranche)
					.HasForeignKey(e => e.TrancheId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
			}

			// Model: User
			{
				// Configure Id of AspNetUsers as identity column
				modelBuilder.Entity<AppUser>(b => {
					b.Property<int>("Id")
						.ValueGeneratedOnAdd()
						.HasColumnType("int");
					SqlServerPropertyBuilderExtensions.UseIdentityColumn(
						b.Property<int>("Id"));
				});

				modelBuilder.Entity<AppUser>()
					.HasMany<Question>()
					.WithOne(e => e.PostedBy)
					.HasForeignKey(e => e.PostedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
				modelBuilder.Entity<AppUser>()
					.HasMany<Question>()
					.WithOne(e => e.AnsweredBy)
					.HasForeignKey(e => e.AnsweredById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired(false);
				modelBuilder.Entity<AppUser>()
					.HasMany<Question>()
					.WithOne(e => e.QuestionApprovedBy)
					.HasForeignKey(e => e.QuestionApprovedById)
					.OnDelete(DeleteBehavior.Restrict);
				modelBuilder.Entity<AppUser>()
					.HasMany<Question>()
					.WithOne(e => e.AnswerApprovedBy)
					.HasForeignKey(e => e.AnswerApprovedById)
					.OnDelete(DeleteBehavior.Restrict);
				modelBuilder.Entity<AppUser>()
					.HasMany<Question>()
					.WithOne(e => e.LastEditor)
					.HasForeignKey(e => e.LastEditorId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				modelBuilder.Entity<AppUser>()
					.HasMany<Document>()
					.WithOne(e => e.UploadedBy)
					.HasForeignKey(e => e.UploadedById)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				modelBuilder.Entity<AppUser>()
					.HasMany(e => e.Documents)
					.WithOne(e => e.AssocUser)
					.HasForeignKey(e => e.AssocUserId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired(false);
			}

			// Model: Question
			{
				modelBuilder.Entity<Question>()
					.HasMany(e => e.Attachments)
					.WithOne(e => e.AssocQuestion)
					.HasForeignKey(e => e.AssocQuestionId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired(false);
			}

			// Model: Document
			{
				
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
