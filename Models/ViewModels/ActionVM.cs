namespace Training.Models.ViewModels
{
    public class ActionVM
    {
        public int Id { get; set; }
        public string ActionName { get; set; }
        public string Remarks { get; set; }
        public string By { get; set; }
        public string Role { get; set; }

        // yearlyplan
        public string Co { get; set; }
        public string Yr { get; set; }
        public string Dp { get; set; }
        public string St { get; set; }
    }
}
