using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    //public enum FeedbackType
    //{
    //    General = 1,
    //    [Display(Name = "Patrol Car")]
    //    PatrolCar  = 2,
    //    [Display(Name = "Colour Codes")]
    //    ColourCodes = 3, 
    //}

    public class FeedbackTemplate
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        //public FeedbackType Type { get; set; }
        public int Type { get; set; }
        public string Text { get; set; }
    }
    public class FeedbackType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
 }
