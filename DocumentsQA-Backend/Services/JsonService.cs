using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DocumentsQA_Backend.Services {
	public class RequiredBindingMetadataProvider : IBindingMetadataProvider {
		public void CreateBindingMetadata(BindingMetadataProviderContext context) {
			if (context.PropertyAttributes?.OfType<RequiredAttribute>().Any() is true) {
				context.BindingMetadata.IsBindingRequired = true;
			}
		}
	}

	public class RequiredPropertiesContractResolver : DefaultContractResolver {
		protected override JsonObjectContract CreateObjectContract(Type objectType) {
			var contract = base.CreateObjectContract(objectType);

			foreach (var contractProperty in contract.Properties) {
				if (contractProperty.PropertyType!.IsValueType) {
					if (contractProperty.AttributeProvider!.GetAttributes(
						typeof(RequiredAttribute), inherit: true).Any())
					{
						contractProperty.Required = Required.Always;
					}
				}
			}

			return contract;
		}
	}
}
