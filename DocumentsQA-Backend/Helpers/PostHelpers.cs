﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using DocumentsQA_Backend.Models;
using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Services;

namespace DocumentsQA_Backend.Helpers {
	public class PostHelpers {
		public static IQueryable<Question> FilterQuery(IQueryable<Question> baseQuery, PostGetFilterDTO filter) {
			IQueryable<Question> query = baseQuery;

			if (filter.Type is not null) {
				string typeName = filter.Type.ToLower();
				if (typeName == "general") {
					query = query.Where(x => x.Type == QuestionType.General);
				}
				else if (typeName == "account") {
					if (filter.Account == null) {
						throw new ArgumentException("Account number cannot be null", "Account");
					}

					query = query.Where(x => x.Type == QuestionType.Account
						&& x.AccountId == filter.Account);
				}
			}

			if (filter.TicketID != null) {
				query = query.Where(x => x.Id == filter.TicketID);
			}
			if (filter.PosterID != null) {
				query = query.Where(x => x.PostedById == filter.PosterID);
			}
			if (filter.Tranche != null) {
				query = query.Where(x => x.Account == null || x.Account.Tranche.Name == filter.Tranche);
			}
			if (filter.PostedFrom is not null) {
				query = query.Where(x => x.DatePosted >= filter.PostedFrom);
			}
			if (filter.PostedTo is not null) {
				query = query.Where(x => x.DatePosted < filter.PostedTo);
			}
			if (filter.OnlyAnswered != null) {
				if (filter.OnlyAnswered.Value)
					query = query.Where(x => x.QuestionAnswer != null);
				else
					query = query.Where(x => x.QuestionAnswer == null);
			}
			if (filter.SearchTerm != null) {
				query = query.Where(x => EF.Functions.Contains(x.QuestionText, filter.SearchTerm));
			}

			return query;
		}

		public static bool AllowUserReadPost(IAccessService access, Question question) {
			if (question.Type == QuestionType.General) {
				Project project = question.Project;
				return access.AllowToProject(project);
			}
			else {
				Tranche tranche = question.Account!.Tranche;
				return access.AllowToTranche(tranche);
			}
		}
		public static bool AllowUserEditPost(IAccessService access, Question question) {
			return AllowUserManagePost(access, question) || (access.GetUserID() == question.PostedById);
		}
		public static bool AllowUserManagePost(IAccessService access, Question question) {
			if (question.Type == QuestionType.General) {
				Project project = question.Project;
				return access.AllowManageProject(project);
			}
			else {
				Tranche tranche = question.Account!.Tranche;
				return access.AllowManageTranche(tranche);
			}
		}
	}
}