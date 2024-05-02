using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	[Route("api/manage")]
	[Authorize]
	public class ManagerController : Controller {
		private readonly ILogger<ManagerController> _logger;

		private readonly IWebHostEnvironment _env;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly UserManager<AppUser> _userManager;

		private readonly AdminHelpers _adminHelper;
		private readonly AuthHelpers _authHelper;
		private readonly IProjectRepository _repoProject;

		public ManagerController(
			ILogger<ManagerController> logger,
			IWebHostEnvironment env,
			DataContext dataContext, IAccessService access,
			UserManager<AppUser> userManager,
			AdminHelpers adminHelper,
			AuthHelpers authHelper,
			IProjectRepository repoProject)
		{
			_logger = logger;

			_env = env;

			_dataContext = dataContext;
			_access = access;

			_userManager = userManager;

			_adminHelper = adminHelper;
			_authHelper = authHelper;
			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Grants tranche read access for a user to a project
		/// <para>To grant management rights, see <see cref="AdminController.GrantProjectManagement"/></para>
		/// </summary>
		[HttpPut("grant/access/{tid}/{uid}")]
		public async Task<IActionResult> GrantTrancheAccess(int tid, int uid) {
			var tranche = await _repoProject.GetTrancheAsync(tid);
			if (tranche == null)
				return BadRequest("Tranche not found");

			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (!await _authHelper.CanManageUser(user))
				return Forbid();

			if (!tranche.UserAccesses.Exists(x => x.Id == uid))
				tranche.UserAccesses.Add(user);

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		/// <summary>
		/// Grants tranche read access for a group of users to a project
		/// <para>To grant management rights, see <see cref="AdminController.GrantProjectManagementFromFile"/></para>
		/// <example>
		/// Example file structure is (newline is interpreted as a comma):
		/// <code>
		///		100, 101, 102
		///		103
		///		104, 105
		///		110, 120, 1111
		/// </code>
		/// </example>
		/// </summary>
		[HttpPut("grant/access/file/{tid}")]
		[RequestSizeLimit(bytes: 4 * 1024 * 1024)]  // 4MB
		public async Task<IActionResult> GrantTrancheAccessFromFile(int tid, [FromForm] IFormFile file) {
			// TODO: Figure out bulk CanManageUser check later
			return StatusCode((int)HttpStatusCode.NotImplemented);

			var tranche = await _repoProject.GetTrancheAsync(tid);
			if (tranche == null)
				return BadRequest("Tranche not found");

			List<int> userIdsGrant;
			try {
				string contents = await FileHelpers.ReadIFormFile(file);
				userIdsGrant = ValueHelpers.SplitIntString(contents).ToList();
			}
			catch (Exception e) {
				return BadRequest("File parse error: " + e.Message);
			}

			{
				// Clear existing access first to not cause insert conflicts
				_adminHelper.RemoveUsersTrancheAccess(tid, userIdsGrant);

				var dbSetTranche = _dataContext.Set<EJoinClass>("TrancheUserAccess");
				dbSetTranche.AddRange(userIdsGrant.Select(u => new EJoinClass {
					Id1 = tid,
					Id2 = u,
				}));
			}

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		/// <summary>
		/// Removes user tranche read access
		/// <para>To remove management rights, see <see cref="AdminController.RemoveProjectManagement"/></para>
		/// </summary>
		[HttpDelete("ungrant/access/{tid}/{uid}")]
		public async Task<IActionResult> RemoveTrancheAccess(int tid, int uid) {
			var tranche = await _repoProject.GetTrancheAsync(tid);
			if (tranche == null)
				return BadRequest("Tranche not found");

			AppUser? user = await Queries.GetUserFromId(_dataContext, uid);
			if (user == null)
				return BadRequest("User not found");

			if (!await _authHelper.CanManageUser(user))
				return Forbid();

			_adminHelper.RemoveUsersTrancheAccess(tid, new List<int> { uid });

			var rows = await _dataContext.SaveChangesAsync();
			return Ok(rows);
		}

		// -----------------------------------------------------

		private class _TmpUserData {
			public AppUser? User { get; set; } = null;
			public string Email { get; set; } = string.Empty;
			public string Password { get; set; } = string.Empty;
			public string Name { get; set; } = string.Empty;
			public string Company { get; set; } = string.Empty;
			public HashSet<int>? Tranches { get; set; }
			public bool Staff { get; set; }
		}

		private async Task _AddUsersIntoDatabase(List<_TmpUserData> users, Project project) {
			int projectId = project.Id;
			DateTime date = DateTime.Now;

			// Extra user constraints
			{
				if (users.Any(x => !x.Staff && x.Tranches != null && x.Tranches.Count == 0)) {
					throw new InvalidDataException("Illegal to create a normal user with no tranche access");
				}
			}

			// Wrap all operations in a transaction so failure would revert the entire thing
			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Warning: Inefficient
				// If the system is to be scaled in the future, find some way to efficiently bulk-create users
				//	rather than repeatedly awaiting CreateAsync

				foreach (var u in users) {
					var user = new AppUser {
						Email = u.Email,
						UserName = u.Email,
						DisplayName = u.Name,
						//Company = u.Company,
						DateCreated = date,
					};
					u.User = user;

					var result = await _userManager.CreateAsync(user, u.Password);
					if (!result.Succeeded)
						throw new Exception(u.Email);

					// Set user role
					await AppRole.AddRoleToUser(_userManager, user, AppRole.User);
				}

				await _dataContext.SaveChangesAsync();

				{
					var normalUsers = users
						.Where(x => !x.Staff)
						.ToList();
					foreach (var iTranche in project.Tranches) {
						var accesses = normalUsers
							.Where(x => x.Tranches == null || x.Tranches.Contains(iTranche.Id))
							.Select(x => x.User!)
							.ToArray();
						iTranche.UserAccesses.AddRange(accesses);
					}
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
				await transaction.CommitAsync();
			}
		}

		/// <summary>
		/// Creates new user in bulk, with access to specific tranches of a project
		/// <para>Each line is a user data; email, display name [, tranches access...]</para>
		/// <para>A user might also be created without any initial tranche access</para>
		/// <para>Putting "*" as the tranche access will give access to all of the project's tranches</para>
		/// <example>
		/// Example file structure is:
		/// <code>
		///		aaaa@email.com, Maria, TrancheA
		///		bbbb@email.com, hhhhhhhhhhh, TrancheA, TrancheB, TrancheC
		///		cccc@email.com, M*rjorie T*ylor Gr*ene
		///		dddd@email.com, asdkajfhsd, TrancheB
		///		zzzz@fbc.us.gov, Jesse, *
		/// </code>
		/// </example>
		/// </summary>
		[HttpPost("bulk/create_user")]
		[RequestSizeLimit(bytes: 16 * 1024 * 1024)]		// 16MB
		public async Task<IActionResult> AddUsersFromFile([FromForm] IFormFile file) {
			var project = await _repoProject.GetProjectAsync();

			List<string> fileLines;
			{
				try {
					string contents = await FileHelpers.ReadIFormFile(file);
					fileLines = contents.SplitLines();
				}
				catch (Exception e) {
					return BadRequest("File open error: " + e.Message);
				}
			}

			DateTime date = DateTime.Now;
			string projectCompany = project.CompanyName;

			List<_TmpUserData> listUser = new();
			{
				Dictionary<string, int> trancheMap = project.Tranches
					.ToDictionary(x => x.Name, x => x.Id);

				var rnd = new Random(date.GetHashCode());

				int iLine = 1;
				try {
					foreach (var line in fileLines) {
						var data = line.Split(',').Select(x => x.Trim()).ToArray();

						string email = data[0];
						string displayName = data[1];

						HashSet<int>? tranches;

						if (data.Length > 2) {
							// * means all tranches, represented with a null, because this is unfortunately not Rust
							if (data.Length == 3 && data[2] == "*") {
								tranches = null;
							}
							else {
								// Collect all tranches after as varargs-like
								tranches = data.Skip(2)
									.Select(x => trancheMap[x])
									.ToHashSet();
							}
						}
						else {
							// No access to any tranche -> empty set
							tranches = new();
						}

						listUser.Add(new _TmpUserData {
							Email = email,
							Password = AuthHelpers.GeneratePassword(rnd, 8),
							Name = displayName,
							Company = projectCompany,
							Tranches = tranches,
							Staff = false,
						});

						++iLine;
					}
				}
				catch (Exception e) {
					if (e is KeyNotFoundException) {
						return BadRequest($"Tranche not found in project at line=\"{iLine}\"");
					}
					return BadRequest($"File parse error [line={iLine}]: " + e.Message);
				}
			}

			try {
				await _AddUsersIntoDatabase(listUser, project);
			}
			catch (Exception e) {
				return BadRequest("Users create failed: " + e.Message);
			}

			// Return data all created users
			var userInfos = listUser
				.Select(x => new {
					id = x.User!.Id,
					user = x.Email,
					pass = x.Password,
				})
				.ToList();

			return Ok(userInfos);
		}

		[HttpPost("bulk/create_user/json")]
		public async Task<IActionResult> AddUsers([FromBody] List<AddUserDTO> dtos) {
			var project = await _repoProject.GetProjectAsync();

			DateTime date = DateTime.Now;
			string projectCompany = project.CompanyName;

			List<_TmpUserData> listUser = new();
			{
				Dictionary<string, int> trancheMap = project.Tranches
					.ToDictionary(x => x.Name, x => x.Id);

				var rnd = new Random(date.GetHashCode());

				try {
					foreach (var user in dtos) {
						HashSet<int>? tranches;

						// null means all tranches
						if (user.Tranches != null) {
							// No access to any tranche -> empty set

							tranches = user.Tranches
								.Select(x => trancheMap[x.Trim()])
								.ToHashSet();
						}
						else {
							tranches = null;
						}

						listUser.Add(new _TmpUserData {
							Email = user.Email,
							Password = AuthHelpers.GeneratePassword(rnd, 8),
							Name = user.Name,
							Company = user.Company ?? projectCompany,
							Tranches = tranches,
							Staff = user.Staff ?? false,
						});
					}
				}
				catch (KeyNotFoundException e) {
					return BadRequest($"Tranche not found in project: \"{e.Message}\"");
				}
			}

			if (!_access.IsAdmin()) {
				if (listUser.Any(x => x.Staff)) {
					return Forbid("Must be admin to create user as staff");
				}
			}

			try {
				await _AddUsersIntoDatabase(listUser, project);
			}
			catch (Exception e) {
				return BadRequest("Users create failed: " + e.Message);
			}

			// Return data all created users
			var userInfos = listUser
				.Select(x => new {
					id = x.User!.Id,
					user = x.Email,
					pass = x.Password,
				})
				.ToList();

			return Ok(userInfos);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets project questions as paginated list
		/// <para>Valid filters for filterDTO:</para>
		/// <list type="bullet">
		///		<item>TicketID</item>
		///		<item>PosterID</item>
		///		<item>Tranche</item>
		///		<item>Account</item>
		///		<item>PostedFrom</item>
		///		<item>PostedTo</item>
		///		<item>OnlyAnswered</item>
		///		<item>SearchTerm</item>
		/// </list>
		/// </summary>
		[HttpPost("post")]
		public async Task<IActionResult> GetPosts([FromBody] PostGetFilterDTO filterDTO, [FromQuery] int details = 0) {
			var project = await _repoProject.GetProjectAsync();
			var projectId = project.Id;

			IQueryable<Question> query;
			{
				switch (filterDTO.Approved) {
					case true:
						query = Queries.GetApprovedQuestionsQuery(_dataContext, projectId);		// Gets only approved
						break;
					case false:
						query = Queries.GetUnapprovedQuestionsQuery(_dataContext, projectId);	// Gets only unapproved
						break;
					case null:
						query = _dataContext.Questions
							.Where(x => x.ProjectId == projectId);		// Gets everything
						break;
				}
			}

			try {
				query = PostHelpers.FilterQuery(query, filterDTO);
			}
			catch (ArgumentException e) {
				ModelState.AddModelError(e.ParamName!, e.Message);
				return BadRequest(new ValidationProblemDetails(ModelState));
			}

			var listPosts = await query.ToListAsync();
			var listPostTables = listPosts.Select(x => x.ToJsonTable(details));

			return Ok(listPostTables);
		}

		/// <summary>
		/// <para>Deprecated, please use <see cref="PostController.EditQuestion"/> instead.</para>
		/// </summary>
		[HttpPost("editq")]
		public async Task<IActionResult> EditQestion([FromBody] PostEditQuestionDTO questionDetails)
		{
			var project = await _repoProject.GetProjectAsync();

			//var questions = project.Questions
			//.Where(x => questionDetails.Questions.Any(y => y == x.Id))
			//.ToList();
			var quest =  await Queries.GetQuestionFromId(_dataContext, questionDetails.Id);
            if (quest == null)
                return BadRequest("Question not found");
            quest =PostHelpers.updateQuestion(quest, questionDetails, _access.GetUserID());
            _dataContext.SaveChanges();

            return Ok("{}");
        }


		/// <summary>
		/// Handles file uploads for all document types
		/// <para>Deprecated, please use <see cref="DocumentController.UploadDocument"/> instead.</para>
		/// </summary>
		[HttpPost("upQDoc")]
        public async Task<IActionResult> UploadQestionDocs( [FromForm] fileUploadDTO uploadDetails)
        {
			var documentFolder= "Documents/1/";
            var files = Request.Form.Files;
			var rootPath =  _env.ContentRootPath;
			var upPath = Path.Combine(rootPath, documentFolder);
			var docList= new List<DocumentsQA_Backend.Models.Document>();
			var docType = uploadDetails.upType;

			// TODO: Change to use DocumentHelpers.ParseDocumentType
			var enumType= Enum.Parse<DocumentType>(docType);


            try
			{

				
				foreach (var file in files)
				{
                    var fName = System.IO.Path.GetFileNameWithoutExtension(file.FileName); 
					var finalName= fName;
                    int index = 1;
                    var ext = Path.GetExtension(file.FileName).ToString();
                    while (System.IO.File.Exists(upPath +"/"+ fName + ext))
                    {
                        fName = finalName + "(" + index + ")";
                        index++;
                    }
					

					var fullName = fName + ext;
                    var docDetails = new DocumentsQA_Backend.Models.Document()
					{
						UploadedById = _access.GetUserID(),
						DateUploaded = DateTime.UtcNow,
						AllowPrint = false,
						ProjectId = 1,   ///fixneeded 
					Type= enumType,
						FileName = fullName,
						FileUrl = fullName,//"/Documents/"+ fullName, ///fix needed

                        Description = "file",
						FileType= ext
                };
					//docDetails.Type = Enum.Parse<DocumentType>("Account"); 
					switch (docType)
					{
                        case "Account":
                            docDetails.AssocAccountId= uploadDetails.AccountId;
                            break;
                        case "Question":
                            docDetails.AssocQuestionId = uploadDetails.QuestionID;
                            break;
                       
                        default:
							docDetails.AssocAccountId = 0;
							docDetails.AssocQuestionId = 0;
                            break;
                    }
					var fullPath = Path.Combine(upPath, fullName);
					// You can access the file here
					using (var stream = new FileStream(fullPath, FileMode.Create))
					{
						//var fName= file.FileName;
						await file.CopyToAsync(stream);
					}
                    _dataContext.Documents.Add(docDetails);
                }
				_dataContext.SaveChanges();
			}
			catch (Exception ex)
			{
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return Ok("");
        }
    }


    }
