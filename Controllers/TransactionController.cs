using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Training.Filters;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    public class TransactionController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ICourseService _courseService;
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _emp;
        private readonly IAntiforgery _antiforgery;
        public TransactionController(
            ICurrentUserService emp,
            ICategoryService categoryService,
            ICourseService courseService,
            ITrainingService trainingService,
            IAntiforgery antiforgery
            )
        {
            _categoryService = categoryService;
            _courseService = courseService;
            _trainingService = trainingService;
            _emp = emp;
            _antiforgery = antiforgery;
            _antiforgery = antiforgery;
        }

        public IActionResult Index()
        {
            return View();
        }


        #region Event Registration
        public IActionResult Event() 
        {
            return View();
        }
        #endregion

        #region Event Realization

        #endregion



    }
}
