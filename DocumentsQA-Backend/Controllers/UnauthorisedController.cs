using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocumentsQA_Backend.Controllers
{

    /// <summary>
    /// This controller is responsible to handling requests outside of the normal scope of the application. Mostly through no code solutions.
    /// Will will add security, but for now it is just a placeholder. security will handled within the http request as needed.
    /// </summary>
    public class UnauthorisedController : Controller
    {
        private readonly DataContext _dataContext;
        public UnauthorisedController(DataContext dataContext)
        {

            _dataContext = dataContext;
           

         
        }

        /// <summary>
        /// Posts general questions in bulk
        /// </summary>
        [HttpPost("post_question_g_multiple/{pid}")]
        public async Task<IActionResult> PostGeneralQuestionMultiple(int pid, [FromBody] List<PostCreateDTO> dto)
        {
            Project? project = await Queries.GetProjectFromId(_dataContext, pid);
            if (project == null)
                return BadRequest("Project not found");

            //if (!_access.AllowToProject(project))
            //    return Unauthorized();

            List<Question> listQuestions = new();

            ///placeholder, will get the user identity from the request details.
            var userId = 1;
            foreach (var i in dto)
            {
                var question = PostHelpers.CreateQuestion(
                    QuestionType.Account, project.Id,
                    i.Text, i.Category ?? QuestionCategory.General,
                    userId);

                listQuestions.Add(question);
            }

            project.Questions.AddRange(listQuestions);
            await _dataContext.SaveChangesAsync();

            // Return IDs of all created questions
            var questionIds = listQuestions.Select(x => x.Id).ToList();

            return Ok(questionIds);
        }

    }
}
