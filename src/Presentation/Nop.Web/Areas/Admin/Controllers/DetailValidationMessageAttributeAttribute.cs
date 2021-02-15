using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Represents a filter attribute that add a detail validation message to model error
    /// </summary>
    public sealed class DetailValidationMessageAttribute : TypeFilterAttribute
    {
        #region Ctor

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        /// <param name="ignore">Whether to ignore the execution of filter actions</param>
        public DetailValidationMessageAttribute(bool ignore = false) : base(typeof(DetailValidationMessageFilter))
        {
            IgnoreFilter = ignore;
            Arguments = new object[] { ignore };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether to ignore the execution of filter actions
        /// </summary>
        public bool IgnoreFilter { get; }
        
        #endregion

        #region Nested filter

        /// <summary>
        /// Represents a filter attribute that add displayName of property to error text
        /// </summary>
        private class DetailValidationMessageFilter : IAsyncActionFilter
        {
            #region Fields

            private readonly bool _ignoreFilter;

            #endregion

            #region Ctor

            public DetailValidationMessageFilter(bool ignoreFilter)
            {
                _ignoreFilter = ignoreFilter;
            }

            #endregion

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                //check whether this filter has been overridden for the Action
                var actionFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(filterDescriptor => filterDescriptor.Scope == FilterScope.Action)
                    .Select(filterDescriptor => filterDescriptor.Filter)
                    .OfType<DetailValidationMessageAttribute>()
                    .FirstOrDefault();

                //ignore filter
                if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                    return;

                var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

                if (context.ModelState.ErrorCount > 0)
                {
                    var modelType = context.ActionDescriptor.Parameters.Select(p => p.ParameterType)
                        .FirstOrDefault();

                    if (modelType != null)
                        foreach (var modelState in context.ModelState.Where(e => e.Value.ValidationState == ModelValidationState.Invalid))
                        {
                            var property = modelType.GetProperties().FirstOrDefault(p =>
                                p.Name.Equals(modelState.Key, StringComparison.InvariantCultureIgnoreCase));

                            if (property == null)
                                continue;

                            var displayName = property.Name;

                            var displayNameAttributeValue = property
                                .GetCustomAttributes(typeof(NopResourceDisplayNameAttribute), true)
                                .Cast<DisplayNameAttribute>().SingleOrDefault()?.DisplayName;
                            displayName = displayNameAttributeValue ?? displayName;

                            var errors = await modelState.Value.Errors.SelectAwait(async r => r.ErrorMessage.Replace("nop_value_must_not_be_null", string.Format(await localizationService.GetResourceAsync("Admin.Common.ValueMustNotBeNull"), displayName))).ToListAsync();
                            modelState.Value.Errors.Clear();
                            foreach (var error in errors)
                                modelState.Value.Errors.Add(error);
                        }
                }

                await next();
            }
        }

        #endregion
    }
}