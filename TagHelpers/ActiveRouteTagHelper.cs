using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Training.TagHelpers
{
    [HtmlTargetElement(Attributes = "is-active-route")]
    public class ActiveRouteTagHelper : TagHelper
    {
        private IDictionary<string, string> _routeValues;

        [HtmlAttributeName("asp-controller")]
        public string Controller { get; set; }

        [HtmlAttributeName("asp-action")]
        public string Action { get; set; }

        [HtmlAttributeName("asp-all-route-data", DictionaryAttributePrefix = "asp-route-")]
        public IDictionary<string, string> RouteValues
        {
            get => _routeValues ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            set => _routeValues = value;
        }

        [HtmlAttributeName("is-active-route")]
        public string ActiveClass { get; set; }

        // NEW: Support for parent controllers
        [HtmlAttributeName("parent-controllers")]
        public string ParentControllers { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);

            if (ShouldBeActive() || ShouldParentBeActive())
            {
                MakeActive(output);
            }

            output.Attributes.RemoveAll("is-active-route");
            output.Attributes.RemoveAll("parent-controllers");
        }

        private bool ShouldBeActive()
        {
            var currentController = ViewContext.RouteData.Values["Controller"]?.ToString();
            var currentAction = ViewContext.RouteData.Values["Action"]?.ToString();

            if (!string.IsNullOrEmpty(Controller) && !string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(Action) && !string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var routeValue in RouteValues)
            {
                if (!ViewContext.RouteData.Values.ContainsKey(routeValue.Key) ||
                    ViewContext.RouteData.Values[routeValue.Key]?.ToString() != routeValue.Value)
                {
                    return false;
                }
            }

            return true;
        }

        // NEW: Check if parent should be active based on child controllers
        private bool ShouldParentBeActive()
        {
            if (string.IsNullOrEmpty(ParentControllers))
                return false;

            var currentController = ViewContext.RouteData.Values["Controller"]?.ToString();
            var parentControllerList = ParentControllers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                      .Select(c => c.Trim())
                                                      .ToArray();

            return parentControllerList.Any(pc => string.Equals(pc, currentController, StringComparison.OrdinalIgnoreCase));
        }

        private void MakeActive(TagHelperOutput output)
        {
            var classAttribute = output.Attributes.FirstOrDefault(a => a.Name == "class");
            if (classAttribute == null)
            {
                output.Attributes.Add("class", ActiveClass ?? "active");
            }
            else
            {
                if (classAttribute.Value == null || classAttribute.Value.ToString().IndexOf(ActiveClass ?? "active") < 0)
                {
                    output.Attributes.SetAttribute("class", classAttribute.Value == null
                        ? ActiveClass ?? "active"
                        : classAttribute.Value + " " + (ActiveClass ?? "active"));
                }
            }
        }
    }
}
