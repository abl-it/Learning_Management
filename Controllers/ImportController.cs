using Microsoft.AspNetCore.Mvc;
using Training.Filters;

namespace Training.Controllers
{
    /// <summary>
    /// Controller untuk Import Data functionality
    /// </summary>
    [ServiceFilter(typeof(ProfileAttribute))]
    public class ImportController : Controller
    {
        /// <summary>
        /// Import Master Course page
        /// Hanya authorized roles: tc, gm, director
        /// </summary>
        [RoleAuthorize("tc", "gm", "director")]
        public IActionResult ImportCourse()
        {
            return View();
        }

        // Future: ImportCategory(), ImportEvent(), ImportParticipant() akan ditambah
    }
}
