using Training.Models;

namespace Training.Services.IServices
{
    public interface ICurrentUserService
    {
        Employees CurrentEmployee { get; set; }
    }
}
