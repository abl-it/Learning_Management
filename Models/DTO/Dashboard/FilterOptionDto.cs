namespace Training.Models.Dtos
{
    /// <summary>
    /// DTO untuk filter options (dropdowns)
    /// </summary>
    public class FilterOptionDto
    {
        /// <summary>
        /// Option value/ID
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Display text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Is selected/default
        /// </summary>
        public bool Selected { get; set; } = false;
    }
}
