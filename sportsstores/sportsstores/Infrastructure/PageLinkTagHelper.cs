using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using sportsstores.Models.ViewModels;

namespace sportsstores.Infrastructure
{
    [HtmlTargetElement ("div", Attributes = "page-model")]
    public class PageLinkTagHelper:TagHelper 
    {
        private IUrlHelperFactory urelHelperFactory;
        public PageLinkTagHelper(IUrlHelperFactory HelperFactory) {
            urelHelperFactory = HelperFactory;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext viewContext { set; get; }
        public PadingInfo PageModel { set; get; }
        public string PageAction { set; get; }
        [HtmlAttributeName(DictionaryAttributePrefix = "pure-url-")]
        public Dictionary<string, object> PageUrlValue { get; set; }
        = new Dictionary<string, object>();

        public bool PageClassesEnable { get; set; } = false;
        public string PageClass { get; set; }
        public string PageClassesNormal { get; set; }
        public string PageClassSelect { get; set; }
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            IUrlHelper urlHelper = urelHelperFactory.GetUrlHelper(viewContext);
            TagBuilder result = new TagBuilder("div");
            for (int i = 1; i <= PageModel.TotalPages; i++) {
                TagBuilder tag = new TagBuilder("a");
                tag.Attributes["href"] = urlHelper.Action(PageAction, 
                    new {productPage = i});
                PageUrlValue["productPage"] = i;
                tag.Attributes["href"] = urlHelper.Action(PageAction, PageUrlValue);
                if (PageClassesEnable) {
                    tag.AddCssClass(PageClass);
                    tag.AddCssClass(i == PageModel.CurentPage
                        ? PageClassSelect : PageClassesNormal);
                }
                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}
