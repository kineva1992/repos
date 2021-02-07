
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
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            IUrlHelper urlHelper = urelHelperFactory.GetUrlHelper(viewContext);
            TagBuilder result = new TagBuilder("div");
            for (int i = 0; i <= PageModel.TotalPages; i++) {
                TagBuilder tag = new TagBuilder("a");
                tag.Attributes["href"] = urlHelper.Action(PageAction, 
                    new {productPage = i});
                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}
