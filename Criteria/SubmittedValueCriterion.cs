﻿using EPiServer.Core;
using EPiServer.Data.Dynamic;
using EPiServer.DataAnnotations;
using EPiServer.Forms.Core;
using EPiServer.Forms.Core.Data;
using EPiServer.Forms.Core.Models;
using EPiServer.Forms.Helpers;
using EPiServer.Forms.Implementation.Elements;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc.VisitorGroups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EPiServer.Forms.Core.Internal;
namespace EPiServer.Forms.Samples.Criteria
{
    [VisitorGroupCriterion(
        Category = "EPiServer Forms",
        DisplayName = "Submitted Value Form",
        LanguagePath = "/episerver/forms/samples/criteria/submittedvaluecriterion", 
        ScriptUrl = "ClientResources/Criteria/SubmittedValue.js")]
    public class SubmittedValueCriterion : CriterionBase<SubmittedValueModel>
    {
        private Injected<IFormDataRepository> _formDataRepository;
        private Injected<ProgressiveSubmitInfoService> _progressiveSubmitInfoProvider;
        private Injected<IContentRepository> _contentRepository;

        /// <summary>
        /// Determines whether current user already submitted a value to the form.
        /// </summary>
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            if (Model == null || Model.SelectedForm == null || Model.SelectedField == null)
            {
                return false;
            }

            var formGuid = Guid.Parse(Model.SelectedForm);
            IContent content;
            var ret = _contentRepository.Service.TryGet<IContent>(formGuid, out content);
            if (!ret || content == null || !(content is FormContainerBlock))
            {
                return false;
            }

            var progressiveSubmitInfo = _progressiveSubmitInfoProvider.Service.GetProgressiveSubmitInfo(content.ContentGuid, httpContext);
            if (progressiveSubmitInfo == null || !progressiveSubmitInfo.IsFinalized)
            {
                return false;
            }

            var localizable = content as ILocalizable;
            var post = _formDataRepository.Service.GetSubmissionData(new FormIdentity(formGuid, localizable.Language.Name), new string[] { progressiveSubmitInfo.SubmissionId.ToString() }).SingleOrDefault();
            if (post == null)
            {
                return false;
            }

            var submittedValue = string.Empty;
            if (!post.Data.ContainsKey(Model.SelectedField) || post.Data[Model.SelectedField] == null)
            {
                return false;      
            }
            
            submittedValue = post.Data[Model.SelectedField].ToString();

            bool isMatch = false;
            switch (Model.Condition)
            {
                case FieldValueCompareCondition.Contains:
                    isMatch = submittedValue.Contains(Model.Value);
                    break;
                case FieldValueCompareCondition.Equals:
                    isMatch = submittedValue == Model.Value;
                    break;
                case FieldValueCompareCondition.NotContains:
                    isMatch = !submittedValue.Contains(Model.Value);
                    break;
                case FieldValueCompareCondition.NotEquals:
                    isMatch = submittedValue != Model.Value;
                    break;
            }

            return isMatch;
        }
    }
}
