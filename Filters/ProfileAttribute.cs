using Training.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Training.Filters
{
    public class ProfileAttribute : ActionFilterAttribute 
    {
        private readonly ITrainingService _trainingService;
        private readonly ICurrentUserService _currentUserService;

        public ProfileAttribute(ITrainingService trainingService, ICurrentUserService currentUserService)
        {
            _trainingService = trainingService;
            _currentUserService = currentUserService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var username = context.HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var employee = _trainingService.GetEmployeeDataByUsername(username);
                _currentUserService.CurrentEmployee = employee;
            }
        }

    }
}
