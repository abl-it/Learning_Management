namespace Training.Services.IServices
{
    public interface IInitialService
    {
        //Initial Yearly Plan 
        Task<int> InitYearlyPlanAsync(int year, string employeeCode);

    }
}
