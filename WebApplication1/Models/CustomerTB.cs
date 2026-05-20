using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class CustomerTB
    {
        [Key]
        public int acno { get; set; }
        [Required(ErrorMessage = "Name can't be Empty")]
        public string name { get; set; }
        public int balance { get; set; }
        public string loginPwd { get; set; }
        public int transPwd { get; set; }
        public int branchId { get; set; }
    }
    public class loginCustomer
    {
        [Required]
        public int acno { get; set; }
        [Required]
        public string loginPwd { get; set; }
    }

}
