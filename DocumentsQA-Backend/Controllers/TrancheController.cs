using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Repository;
using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Extensions;

namespace DocumentsQA_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/tranche")]
	[Authorize(Policy = "Project_Access")]
	public class TrancheController : Controller {
		private readonly ILogger<TrancheController> _logger;

		private readonly DataContext _dataContext;
		private readonly IAccessService _access;

		private readonly IProjectRepository _repoProject;

		public TrancheController(
			ILogger<TrancheController> logger, 
			DataContext dataContext, IAccessService access, 
			IProjectRepository repoProject) 
		{
			_logger = logger;

			_dataContext = dataContext;
			_access = access;

			_repoProject = repoProject;
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets tranche information
		/// </summary>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetTrancheInfo([FromRoute] int id, [FromQuery] int details = 0) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, id);
			if (tranche == null)
				return BadRequest("Tranche not found");
			if (!_access.AllowToTranche(tranche))
				return Forbid();

			return Ok(tranche.ToJsonTable(details));
		}

		/// <summary>
		/// Adds new tranche
		/// </summary>
		[HttpPost("add")]
		[Authorize(Policy = "Role_Admin")]
		public async Task<IActionResult> CreateTranche([FromBody] CreateTrancheDTO dto) {
			var project = await _repoProject.GetProjectAsync();

			if (project.Tranches.Any(x => x.Name == dto.Name)) {
				return BadRequest("Duplicated tranche name");
			}

			var tranche = new Tranche {
				ProjectId = project.Id,
				Name = dto.Name,
			};
			project.Tranches.Add(tranche);

			await _dataContext.SaveChangesAsync();
			return Ok(tranche.Id);
		}

		/// <summary>
		/// Edits tranche information
		/// </summary>
		[HttpPut("edit/{id}")]
		[Authorize(Policy = "Role_Admin")]
		public async Task<IActionResult> EditTranche([FromRoute] int id, [FromBody] EditTrancheDTO dto) {
			var project = await _repoProject.GetProjectAsync();

			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, id);
			if (tranche == null)
				return BadRequest("Tranche not found");

			if (dto.Name != null && dto.Name != tranche.Name) {
				if (project.Tranches.Any(x => x.Name == dto.Name)) {
					return BadRequest("Duplicated tranche name");
				}
				tranche.Name = dto.Name;
			}

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Deletes tranche
		/// </summary>
		[HttpDelete("{id}")]
		[Authorize(Policy = "Role_Admin")]
		public async Task<IActionResult> DeleteTranche([FromRoute] int id) {
			Tranche? tranche = await Queries.GetTrancheFromId(_dataContext, id);
			if (tranche == null)
				return BadRequest("Tranche not found");

			_dataContext.Tranches.Remove(tranche);
			await _dataContext.SaveChangesAsync();

			return Ok();
		}
	}
}
