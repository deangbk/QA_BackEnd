using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	public class DataContext : IdentityDbContext<AppUser, AppRole, int> {
		public DataContext(DbContextOptions<DataContext> options) : base(options) {
		}

		public DbSet<Project> Projects { get; set; }
		public DbSet<Tranche> Tranches { get; set; }
		public DbSet<Account> Accounts { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Document> Documents { get; set; }
		public DbSet<Comment> Comments { get; set; }

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
				// Map Project:User as N:M using join entity
				modelBuilder.Entity<Project>()
					.HasMany(e => e.UserAccesses)
					.WithMany(e => e.ProjectAccesses)
					.UsingEntity<EJoinClass>(
						"ProjectUserAccess",
						l => l.HasOne<AppUser>().WithMany().HasForeignKey(e => e.Id1),
						r => r.HasOne<Project>().WithMany().HasForeignKey(e => e.Id2));

				// Map Project:User as N:M using join entity
				modelBuilder.Entity<Project>()
					.HasMany(e => e.UserManagers)
					.WithMany(e => e.ProjectManages)
					.UsingEntity<EJoinClass>(
						"ProjectUserManage",
						l => l.HasOne<AppUser>().WithMany().HasForeignKey(e => e.Id1),
						r => r.HasOne<Project>().WithMany().HasForeignKey(e => e.Id2));
			}

			// Model: Tranche
			{
				// Map Tranche:Project as N:1
				modelBuilder.Entity<Tranche>()
					.HasOne(e => e.Project)
					.WithMany(e => e.Tranches)
					.HasForeignKey(e => e.ProjectId)
					.OnDelete(DeleteBehavior.Cascade);
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

				// Map User:Project as N:1
				modelBuilder.Entity<AppUser>()
					.HasOne(e => e.FavouriteProject)
					.WithMany()
					.HasForeignKey(e => e.FavouriteProjectId)
					.OnDelete(DeleteBehavior.SetNull)
					.IsRequired(false);

				// Map User:Tranche as N:M using join entity
				modelBuilder.Entity<AppUser>()
					.HasMany(e => e.TrancheAccesses)
					.WithMany(e => e.UserAccesses)
					.UsingEntity<EJoinClass>(
						"TrancheUserAccess",
						l => l.HasOne<Tranche>().WithMany().HasForeignKey(e => e.Id1),
						r => r.HasOne<AppUser>().WithMany().HasForeignKey(e => e.Id2));
			}

			// Model: Account
			{
				// Map Account:Tranche as N:1
				modelBuilder.Entity<Account>()
					.HasOne(e => e.Tranche)
					.WithMany()
					.HasForeignKey(e => e.TrancheId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();
			}

			// Model: Question
			{
				// Map Question:Project as N:1
				modelBuilder.Entity<Question>()
					.HasOne(e => e.Project)
					.WithMany(e => e.Questions)
					.HasForeignKey(e => e.ProjectId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				// Map Question:Account as N:1
				modelBuilder.Entity<Question>()
					.HasOne(e => e.Account)
					.WithMany()
					.HasForeignKey(e => e.AccountId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				// Map Question:User as N:1
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

			// Model: Comment
			{
				// Map Comment:Question as N:1
				modelBuilder.Entity<Comment>()
					.HasOne(e => e.Question)
					.WithMany(e => e.Comments)
					.HasForeignKey(e => e.QuestionId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			}

			// Model: Document
			{
				// Map Document:User as N:1
				modelBuilder.Entity<Document>()
					.HasOne(e => e.UploadedBy)
					.WithMany()
					.HasForeignKey(e => e.AssocQuestionId)
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				// Map Document:Question as N:1
				modelBuilder.Entity<Document>()
					.HasOne(e => e.AssocQuestion)
					.WithMany(e => e.Attachments)
					.HasForeignKey(e => e.AssocQuestionId)
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired(false);

				// Map Document:User as N:1
				modelBuilder.Entity<Document>()
					.HasOne(e => e.AssocQuestion)
					.WithMany(e => e.Attachments)
					.HasForeignKey(e => e.AssocQuestionId)
					.OnDelete(DeleteBehavior.Cascade)
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
