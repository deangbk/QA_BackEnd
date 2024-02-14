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

		[HttpGet("get_stuff/{pid}")]
        public async Task<IActionResult> GetPosts(int pid)
        {

            return Ok(pid);
        }

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
					.Select(x => x.ProjectID)
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

				var userEmails = dtos
					.Select(x => x.Email)
					.Distinct()
					.ToList();

				mapUsers = new();
				foreach (var email in userEmails) {
					var user = await _userManager.FindByEmailAsync(email);

					// TODO: Verify user project access

					if (user != null) {
						mapUsers[email] = user;
					}
				}

				if (mapUsers!.Count != userEmails.Count) {
					var invalidEmails = userEmails.Except(mapUsers.Keys);
					return BadRequest("User not found: " + invalidEmails.ToStringEx());
				}
			}

			List<Question> listQuestions = new();

            foreach (var i in dtos) {
				var user = mapUsers[i.Email];

                var question = PostHelpers.CreateQuestion(
                    QuestionType.General, i.ProjectID,
                    i.Text, i.Category ?? "general",
					user.Id);

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
