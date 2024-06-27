using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Helpers {
	public class AddUserData {
		public AppUser? User { get; set; } = null;
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Company { get; set; } = string.Empty;
		public List<int> Tranches { get; set; } = new();
		public bool Staff { get; set; }
	}

	public class ProjectHelpers {
		private readonly DataContext _dataContext;

		private readonly UserManager<AppUser> _userManager;

		private readonly AdminHelpers _adminHelper;

		public ProjectHelpers(
			DataContext dataContext,
			UserManager<AppUser> userManager,
			AdminHelpers adminHelper)
		{
			_dataContext = dataContext;
			_userManager = userManager;
			_adminHelper = adminHelper;
		}

		// -----------------------------------------------------

		public static bool CanUserAccessProject(Project project, AppUser user) {
			return CanUserAccessProject(project, user.Id);
		}
		public static bool CanUserAccessProject(Project project, int userId) {
			// User has project access if they have access to any of the project's tranches
			var tranches = project.Tranches;
			return tranches.Any(x => x.UserAccesses.Any(y => y.Id == userId));
		}

		public static List<Tranche> GetUserTrancheAccessesInProject(AppUser user, int projectId) {
			return user.TrancheAccesses
				.Where(x => x.ProjectId == projectId)
				.ToList();
		}

		/// <summary>
		/// Gets all user IDs who has access to the project
		/// <para>Admins are excluded</para>
		/// <para>Note: Users without tranche accesses will not show up</para>
		/// </summary>
		public static List<int> GetProjectUserAccesses(Project project) {
			return project.Tranches
				.SelectMany(x => x.UserAccesses)
				.Select(x => x.Id)
				.ToList();
		}

		/// <summary>
		/// Gets all user IDs who has access to the tranche
		/// </summary>
		public static List<int> GetTrancheUserAccesses(Tranche tranche) {
			return tranche.UserAccesses
				.Select(x => x.Id)
				.ToList();
		}

		/// <summary>
		/// Gets all user IDs who has access to the project's tranches
		/// <para>"A": [1, 2, 3, ...],</para>
		/// <para>"B": [10, 11, 12, ...], ...</para>
		/// <para>...</para>
		/// </summary>
		public static Dictionary<Tranche, List<int>> GetTrancheUserAccessesMap(Project project) {
			return project.Tranches
				.Select(x => new {
					tranche = x,
					ids = x.UserAccesses
						.Select(x => x.Id)
						.ToList(),
				})
				.ToDictionary(x => x.tranche, x => x.ids);
		}

		// -----------------------------------------------------

		public async Task AddUsersToProject(Project project, List<AddUserData> users, IDbContextTransaction? prevTransaction = null) {
			int projectId = project.Id;
			DateTime date = DateTime.Now;

			// Extra user constraints
			{
				var uniqueEmails = users
					.Select(x => _userManager.NormalizeEmail(x.Email))
					.ToHashSet();
				
				// Maybe delete this as it's already covered by a model constraint
				{
					var exists = await _dataContext.Users
						.Where(x => x.ProjectId == projectId)
						.Where(x => uniqueEmails.Any(y => y == x.NormalizedEmail))
						.ToListAsync();
					if (exists.Any()) {
						throw new InvalidDataException("Users already exists: "
							+ exists.ToStringEx(before: "", after: ""));
					}
				}

				// Users with ProjectId == null are reserved and must not be duplicated
				var reservedEmails = await _dataContext.Users
					.Where(x => x.ProjectId == null)
					.Select(x => x.NormalizedEmail)
					.ToListAsync();
				{
					var intersect = uniqueEmails.Intersect(reservedEmails);
					if (intersect.Any()) {
						throw new InvalidDataException("Cannot create users with the following emails: "
							+ intersect.ToStringEx(before: "", after: ""));
					}
				}

				if (users.Any(x => !x.Staff && !x.Tranches.Any())) {
					throw new InvalidDataException("Illegal to create a normal user with no tranche access");
				}

				{
					var trancheIds = users
						.SelectMany(x => x.Tranches)
						.Distinct()
						.ToList();
					var validTranches = project.Tranches
						.Select(x => x.Id)
						.ToList();
					var invalidTranches = trancheIds.Except(validTranches);
					if (invalidTranches.Any()) {
						throw new InvalidDataException("Non-existent tranches: "
							+ invalidTranches.ToStringEx(before: "", after: ""));
					}
				}
			}

			// Wrap all operations in a transaction so failure would revert the entire thing
			IDbContextTransaction transaction = null!;
			try {
				if (prevTransaction == null) {
					transaction = _dataContext.Database.BeginTransaction();
				}
				else {
					transaction = prevTransaction;
					transaction.CreateSavepoint("sav_AddUsersToProject");
				}

				// Warning: Inefficient
				// If the system is to be scaled in the future, find some way to efficiently bulk-create users
				//	rather than repeatedly awaiting CreateAsync

				foreach (var u in users) {
					var user = new AppUser {
						Email = u.Email,
						UserName = $"{projectId}:{u.Email}",	// UserName in EFCore must be unique
						//UserName = u.Email,
						DisplayName = u.Name,
						Company = project.CompanyName,
						DateCreated = date,
						ProjectId = projectId,
					};
					u.User = user;

					var result = await _userManager.CreateAsync(user, u.Password);
					if (!result.Succeeded)
						throw new Exception(u.Email);

					// Set user role
					await _adminHelper.GrantUserRole(user, AppRole.User);
				}

				await _dataContext.SaveChangesAsync();

				{
					var accesses = users
						.SelectMany(x =>
							x.Tranches.Select(y => new EJoinClass {
								Id1 = y,
								Id2 = x.User!.Id,
							})
						)
						.ToList();

					var dbsTrancheAccess = _dataContext.Set<EJoinClass>("TrancheUserAccess");
					dbsTrancheAccess.AddRange(accesses);
				}
				{
					var newStaffs = users
						.Where(x => x.Staff)
						.Select(x => x.User!)
						.ToList();
					if (newStaffs.Count > 0) {
						await _adminHelper.MakeProjectManagers(project, newStaffs);
					}
				}

				await _dataContext.SaveChangesAsync();

				if (prevTransaction == null) {
					await transaction.CommitAsync();
				}
			}
			catch (Exception e) {
				if (transaction != null)
					await transaction.RollbackToSavepointAsync("sav_AddUsersToProject");
				throw;
			}
		}
	}
}
