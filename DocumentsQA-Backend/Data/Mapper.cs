using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.CSharp.RuntimeBinder;

using Castle.DynamicProxy;

using DocumentsQA_Backend.Data;
using DocumentsQA_Backend.DTO;
using DocumentsQA_Backend.Helpers;
using DocumentsQA_Backend.Models;

namespace DocumentsQA_Backend.Data {
	using JsonTable = Dictionary<string, object>;

//// adds the filters to what is returned based on detials of the user and request. And maps the data to the json table
	public static class Mapper {
		public static JsonTable ToJsonTable(this Project obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["name"] = obj.Name,
				["display_name"] = obj.DisplayName ?? obj.Name,
			};

			if (detail >= 1) {
				table["date_start"] = obj.ProjectStartDate;
				table["date_end"] = obj.ProjectEndDate;
				table["description"] = obj.Description ?? "";
				table["company"] = obj.CompanyName;

				//table["url_logo"] = obj.LogoUrl!;
				//table["url_banner"] = obj.BannerUrl!;
			}
			if (detail >= 2) {
				//table["tranches"] = obj.Tranches.Select(x => x.ToJsonTable(0)).ToList();
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Tranche obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["name"] = obj.Name,
			};

			if (detail >= 1) {
				table["accounts"] = obj.Accounts
					.Select(x => x.ToJsonTable(0))
					.ToList();
			}

			return table;
		}

		public static JsonTable ToJsonTable(this AppUser obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["display_name"] = obj.DisplayName,
			};

			if (detail >= 1) {
				table["user_name"] = obj.Email;
				table["date_created"] = obj.DateCreated;
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Question obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["q_num"] = obj.QuestionNum,
				["type"] = obj.Type,
				["category"] = obj.Category,
				//["account"] = obj.Account?.ToJsonTable(0)!,

				["q_text"] = obj.QuestionText,
				["a_text"] = obj.QuestionAnswer!,

                ["post_by"] = obj.PostedBy.ToJsonTable(0),

				["date_post"] = obj.DatePosted,
				["date_sent"] = obj.DateSent,
				["date_edit"] = obj.DateLastEdited,
				["date_answered"] = obj.DateAnswered!,

				["attachments"] = obj.Attachments
					.OrderBy(x => x.DateUploaded)
					.Select(x => x.ToJsonTable(0))
					.ToList(),
			};

			if (obj.Type == QuestionType.Account) {
				table["account"] = obj.Account?.ToJsonTable(0)!;
			}

			if (detail >= 1) {
				{
					int subDetail = detail >= 2 ? 1 : 0;

					table["answer_by"] = obj.AnsweredBy?.ToJsonTable(subDetail)!;
					table["q_approve_by"] = obj.QuestionApprovedBy?.ToJsonTable(subDetail)!;
					table["a_approve_by"] = obj.AnswerApprovedBy?.ToJsonTable(subDetail)!;
					table["edit_by"] = obj.LastEditor?.ToJsonTable(subDetail)!;
				}

				table["date_q_approve"] = obj.DateQuestionApproved!;
				table["date_a_approve"] = obj.DateAnswerApproved!;
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Document obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["name"] = obj.FileName,

				["date_upload"] = obj.DateUploaded,
			};

			if (detail >= 1) {
				//table["url"] = obj.FileUrl;
				//table["doc_type"] = obj.Type;

				table["hidden"] = obj.Hidden;
				table["allow_print"] = obj.AllowPrint;

				if (detail >= 3) {
					if (obj.Type == DocumentType.Question) {
						table["assoc_post"] = obj.AssocQuestion!.ToJsonTable(0);
					}
					else if (obj.Type == DocumentType.Account) {
						table["assoc_account"] = obj.AssocAccount!.ToJsonTable(0);
					}
				}
				else {
					if (obj.Type == DocumentType.Question) {
						table["assoc_post"] = obj.AssocQuestionId!;
					}
					else if (obj.Type == DocumentType.Account) {
						table["assoc_account"] = obj.AssocAccountId!;
					}
				}
			}
			if (detail >= 2) {
				table["description"] = obj.Description ?? "";
				table["file_type"] = obj.FileType ?? "";

				table["upload_by"] = obj.UploadedBy.ToJsonTable(0);
			}
			if (detail >= 4) {
				table["url"] = obj.FileUrl;
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Comment obj, int detail) {
			var table = new JsonTable() {
				["num"] = obj.CommentNum,
				["text"] = obj.CommentText,
				["date_post"] = obj.DatePosted,
			};

			if (detail >= 1) {
				table["post_by"] = obj.PostedBy.ToJsonTable(detail >= 2 ? 1 : 0);
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Account obj, int detail) {
			var table = new JsonTable() {
				["id"] = obj.Id,
				["id_pretty"] = obj.GetIdentifierName(),
				["no"] = obj.AccountNo,
				["name"] = obj.AccountName ?? "",
			};

			if (detail >= 1) {
				table["tranche"] = obj.Tranche.ToJsonTable(0);
			}

			return table;
		}

		public static JsonTable ToJsonTable(this Note obj, int detail) {
			var table = new JsonTable() {
				["num"] = obj.Num,
				["text"] = obj.Text,
				["description"] = obj.Description,
				["category"] = obj.Category,
				["date_post"] = obj.DatePosted,
				["sticky"] = obj.Sticky,
			};

			if (detail >= 1) {
				table["post_by"] = obj.PostedBy.ToJsonTable(detail >= 2 ? 1 : 0);
			}

			return table;
		}
	}
}
