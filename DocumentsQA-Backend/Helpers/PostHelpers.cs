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
		public static Question CreateQuestion(QuestionType type, int projectId, string text, string category, int userId) {
			var time = DateTime.Now;
			var question = new Question {
				QuestionNum = 0,
				Type = type,
				Category = category,

				ProjectId = projectId,
				AccountId = null,

				QuestionText = text,

				PostedById = userId,
				LastEditorId = userId,

				DatePosted = time,
				DateSent = time,
				DateLastEdited = time,
			};
			return question;
		}

		/// <summary>
		/// Gets the highest QuestionNum out of all questions in the project
		/// </summary>
		public static int GetHighestQuestionNo(Project project) {
			try {
				return project.Questions.Max(x => x.QuestionNum);
			}
			catch (InvalidOperationException) {
				return 0;
			}
		}

		/// <summary>
		/// Gets the highest CommentNum out of all comments in the question
		/// </summary>
		public static int GetHighestCommentNo(Question question) {
			try {
				return question.Comments.Max(x => x.CommentNum);
			}
			catch (InvalidOperationException) {
				return 0;
			}
		}

		/// <summary>
		/// Gets the highest Num out of all project notes
		/// </summary>
		public static int GetHighestNoteNo(Project project) {
			try {
				return project.Notes.Max(x => x.Num);
			}
			catch (InvalidOperationException) {
				return 0;
			}
		}

		public static void EditQuestion(Question question, PostEditDTO edit, int userId, bool approve = false) {
			var time = DateTime.Now;

			if (edit.Question != null) {
				question.QuestionText = edit.Question;
			}
			if (edit.Answer != null) {
				question.DateAnswered = time;

				question.QuestionAnswer = edit.Answer;
				question.AnsweredById = userId;
				
			}
			if (edit.Category != null) {
				question.Category = edit.Category;
			}

			question.LastEditorId = userId;
			question.DateLastEdited = time;

			ApproveQuestion(question, userId, approve);
			if (edit.Answer != null) {
				ApproveAnswer(question, userId, approve);
			}
		}

		public static void ApproveQuestion(Question question, int userId, bool approve) {
			var time = DateTime.Now;

			if (approve) {
				question.QuestionApprovedById = userId;
				question.DateQuestionApproved = time;
			}
			else {
				question.QuestionApprovedById = null;
				question.DateQuestionApproved = null;

				// Unapproving a question also unapproves its answer
				question.AnswerApprovedById = null;
				question.DateAnswerApproved = null;
			}
		}

		/// <summary>
		/// update single question from edit page
		/// </summary>
		public static  Question updateQuestion(Question question, PostEditQuestionDTO questionDetails, int userId)
		{
			question.QuestionText = questionDetails.Question;
			question.Category = questionDetails.Category;
			question.QuestionAnswer = questionDetails.Answer;
			//question.AccountId = questionDetails.accountId;
			question.DateLastEdited = DateTime.UtcNow;
			question.LastEditorId = userId;

			return question;
		}
		public static void ApproveAnswer(Question question, int userId, bool approve) {
			var time = DateTime.Now;

			if (approve) {
				// Also approve the question if it was unapproved before
				if (question.QuestionApprovedById == null) {
					ApproveQuestion(question, userId, true);
				}

				question.AnswerApprovedById = userId;
				question.DateAnswerApproved = time;
			}
			else {
				question.AnswerApprovedById = null;
				question.DateAnswerApproved = null;
			}
		}

		public static IQueryable<Question> FilterQuery(IQueryable<Question> baseQuery, PostGetFilterDTO filter) {
			IQueryable<Question> query = baseQuery;

			if (filter.Type is not null) {
				string typeName = filter.Type.ToLower();
				if (typeName == "general") {
					query = query.Where(x => x.Type == QuestionType.General);
				}
				else if (typeName == "account") {
					/*
					if (filter.Account == null) {
						throw new ArgumentException("Account number cannot be null", "Account");
					}
					*/
					query = query.Where(x => x.Type == QuestionType.Account);
					if (filter.Account != null) {
						query = query.Where(x => x.AccountId == filter.Account);
					}
				}
			}
			if (filter.Category != null) {
				query = query.Where(x => x.Category == filter.Category);
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
