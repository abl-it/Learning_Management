using Training.Services.IServices;
using Training.Models;


namespace Training.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        public Employees? CurrentEmployee { get; set; }
    }
}
