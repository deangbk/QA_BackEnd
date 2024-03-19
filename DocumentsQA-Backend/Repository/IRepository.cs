using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.Services;
using DocumentsQA_Backend.DTO;

namespace DocumentsQA_Backend.Repository {
	public interface IProjectRepository {
		Task<Project> GetProjectAsync();
	}
}
