using Microsoft.AspNetCore.Mvc;
using Training.Services.IServices;

namespace Training.ViewComponents
{
    public class ProfileViewComponent : ViewComponent
    {
        private readonly ITrainingService _trainingService;
        public ProfileViewComponent(ITrainingService trainingService)
        {
            _trainingService = trainingService;
        }

        public IViewComponentResult Invoke()
        {
            // Ambil username dari session
            var username = HttpContext.Session.GetString("Username");

            TempData["username"] = username;
            if (string.IsNullOrEmpty(username))
            {
                return View(null); // Atau return view dengan model kosong
            }
            var employee = _trainingService.GetEmployeeDataByUsername(username);

            return View(employee);
        }
    }
}
