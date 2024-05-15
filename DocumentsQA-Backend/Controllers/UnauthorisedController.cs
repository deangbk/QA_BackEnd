using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Identity;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers
{
    [Route("api/unauth")]
    /// <summary>
    /// This controller is responsible to handling requests outside of the normal scope of the application. Mostly through no code solutions.
    /// Will will add security, but for now it is just a placeholder. security will handled within the http request as needed.
    /// </summary>
    public class UnauthorisedController : Controller
    {
        private readonly DataContext _dataContext;
		private readonly ILogger<PostController> _logger;

		private readonly UserManager<AppUser> _userManager;

		public UnauthorisedController(DataContext dataContext, ILogger<PostController> logger,
			UserManager<AppUser> userManager) {

            _dataContext = dataContext;
			_logger = logger;

			_userManager = userManager;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Posts general questions in bulk
		/// </summary>
		[HttpPost("post_question_g_multiple")]
        public async Task<IActionResult> PostGeneralQuestionMultiple([FromBody] List<Unauth_PostCreateDTO> dtos)
        {
			Dictionary<int, Project> mapProject;
			Dictionary<string, AppUser> mapUsers;

			{
				// Collect and validate project IDs

				var projectIds = dtos
					.Select(x => x.ProjectID!.Value)
					.Distinct()
					.ToList();

				mapProject = await _dataContext.Projects
					.Where(x => projectIds.Any(y => x.Id == y))
					.ToDictionaryAsync(x => x.Id, x => x);

				if (mapProject!.Count != projectIds.Count) {
					var invalidProjects = projectIds.Except(mapProject.Keys);
					return BadRequest("Project ID not found: " + invalidProjects.ToStringEx());
				}
			}

			{
				// Collect and validate user IDs

				List<string> invalidUsers = new();

				mapUsers = new();
				foreach (var dto in dtos) {
					int projectId = dto.ProjectID!.Value;

					var user = await _userManager.FindByEmailAsync(dto.Email);
					if (user == null) {
						invalidUsers.Add($"(project={projectId}){dto.Email}");
					}
					else {
						// TODO: Verify user project access

						if (user != null) {
							mapUsers[dto.Email] = user;
						}
					}
				}

				if (invalidUsers.Count > 0) {
					return BadRequest("Users not found: " + invalidUsers.ToStringEx());
				}
			}

			List<Question> listQuestions = new();

            foreach (var i in dtos) {
				var user = mapUsers[i.Email];

                var question = PostHelpers.CreateQuestion(
                    QuestionType.General, i.ProjectID!.Value,
                    i.Text, i.Category ?? "general",
					user.Id);
				if (i.DateSent is not null)
					question.DateSent = i.DateSent.Value;

				listQuestions.Add(question);
            }

            _dataContext.Questions.AddRange(listQuestions);
            await _dataContext.SaveChangesAsync();

            // Return IDs of all created questions
            var questionIds = listQuestions.Select(x => x.Id).ToList();

            return Ok(questionIds);
        }

    }
}
