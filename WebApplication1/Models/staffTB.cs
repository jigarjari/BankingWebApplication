using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class staffTB
    {
        [Key]
        public int staffId { get; set; }
        public string name { get; set; }
        public string loginPwd { get; set; }
        public int branchId { get; set; }
    }
    public class loginStaff
    {
        [Required]
        public int staffId { get; set; }
        [Required]
        public string loginPwd { get; set; }
    }
}
