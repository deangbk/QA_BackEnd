using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.EntityFrameworkCore;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	using JsonTable = Dictionary<string, object>;
//// adds the filters to what is returned based on detials of the user an drequest. And maps the data to the json table
	public static class Mapper {
		public static JsonTable FromProject(Project obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["name"] = obj.Name,
				["display_name"] = obj.DisplayName ?? obj.Name,
				["date_start"] = obj.ProjectStartDate,
				["date_end"] = obj.ProjectEndDate,
			};

			if (detailsLevel >= 1) {
				table["description"] = obj.Description ?? "";
				table["company"] = obj.CompanyName;

				table["url_logo"] = obj.LogoUrl!;
				table["url_banner"] = obj.BannerUrl!;
			}
			if (detailsLevel >= 2) {
				table["tranches"] = obj.Tranches.Select(x => x.Name).ToList();
			}

			return table;
		}

		public static JsonTable FromUser(AppUser obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["display_name"] = obj.DisplayName,
			};

			if (detailsLevel >= 1) {
				table["user_name"] = obj.UserName;
				table["date_created"] = obj.DateCreated;
			}

			return table;
		}

		public static JsonTable FromPost(Question obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["q_num"] = obj.QuestionNum,
				["type"] = obj.Type,
				["category"] = obj.Category,
				["tranche"] = obj.Account != null ? obj.Account.Tranche.Name : null!,
				["account"] = obj.Account != null ? obj.Account.AccountNo : null!,

				["q_text"] = obj.QuestionText,
				["a_text"] = obj.QuestionAnswer!,

				["post_by"] = FromUser(obj.PostedBy),

				["date_post"] = obj.DatePosted,
				["date_edit"] = obj.DateLastEdited,

				["attachments"] = obj.Attachments
					.OrderBy(x => x.DateUploaded)
					.Select(x => FromDocument(x, 0))
					.ToList(),
			};

			if (detailsLevel >= 1) {
				table["answer_by_id"] = obj.AnsweredById!;
				table["answer_by"] = obj.AnsweredBy != null ? obj.AnsweredBy.DisplayName : null!;

				table["q_approve_by_id"] = obj.QuestionApprovedById!;
				table["q_approve_by"] = obj.QuestionApprovedBy != null ? obj.QuestionApprovedBy.DisplayName : null!;

				table["a_approve_by_id"] = obj.AnswerApprovedById!;
				table["a_approve_by"] = obj.AnswerApprovedBy != null ? obj.AnswerApprovedBy.DisplayName : null!;

				table["edit_by_id"] = obj.LastEditorId!;
				table["edit_by"] = obj.LastEditor != null ? obj.LastEditor.DisplayName : null!;

				table["date_q_approve"] = obj.DateQuestionApproved!;
				table["date_a_approve"] = obj.DateAnswerApproved!;
			}

			return table;
		}

		public static JsonTable FromDocument(Document obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["name"] = obj.FileName,

				["date_upload"] = obj.DateUploaded,
			};

			if (detailsLevel >= 1) {
				table["url"] = obj.FileUrl;
				//table["doc_type"] = obj.Type;

				table["hidden"] = obj.Hidden;
				table["allow_print"] = obj.AllowPrint;

				if (obj.Type == DocumentType.Question) {
					table["assoc_post"] = obj.AssocQuestionId!;
				}
				else if (obj.Type == DocumentType.Account) {
					table["assoc_account"] = obj.AssocAccountId!;
				}
			}
			if (detailsLevel >= 2) {
				table["description"] = obj.Description ?? "";
				table["file_type"] = obj.FileType ?? "";

				table["upload_by"] = obj.UploadedById;
			}

			return table;
		}

		public static JsonTable FromComment(Comment obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["num"] = obj.CommentNum,
				["text"] = obj.CommentText,
				["date_post"] = obj.DatePosted,
			};

			if (detailsLevel >= 1) {
				table["post_by_id"] = obj.PostedById;
				table["post_by"] = obj.PostedBy != null ? obj.PostedBy.DisplayName : null!;
			}

			return table;
		}

		public static JsonTable FromAccount(Account obj, int detailsLevel = 0) {
			var table = new JsonTable() {
				["id"] = obj.GetIdentifierName(),
				["no"] = obj.AccountNo,
				["name"] = obj.AccountName ?? "",
			};

			return table;
		}
	}
}
