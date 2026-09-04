using Training.Models.DTO.Event;

namespace Training.Services.IServices
{
    public interface IAttendanceFormService
    {
        byte[] GenerateAttendanceFormPdf(TrainingEventDetailDto trainingEvent);
    }
}
