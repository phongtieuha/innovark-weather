using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Innovark.Weather.Web.Models.Base;

/// <summary>
/// Base for pages rendered by a React app: says which Vite entry to load, from the Vite dev server
/// in Development or from the built bundle in wwwroot/js/build otherwise.
/// </summary>
public abstract partial class BaseModel : PageModel
{
    /// <summary>Serialized into <c>window.siteData</c> and passed to the React app as props.</summary>
    public virtual object? Data => null;

    /// <summary>The page's folder under Pages, e.g. "Home" for Pages/Home/Home.ts.</summary>
    public abstract string AppPath { get; }

    public virtual string PageName => AppPath.Replace("/", "", StringComparison.Ordinal);

    public bool IsDevelopment => HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

    public string JsPath => CollapseSlashes(IsDevelopment
        ? $"Pages/{AppPath}/{PageName}.ts"
        : $"js/build/Pages/{AppPath}/{PageName}-*.js");

    public string CssPath => $"js/build/assets/{PageName}-*.css";

    private static string CollapseSlashes(string path) => RepeatedSlashes().Replace(path, "/");

    [GeneratedRegex("/{2,}")]
    private static partial Regex RepeatedSlashes();
}
