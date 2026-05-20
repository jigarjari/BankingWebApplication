using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class BranchTB
    {
        [Key]
        public int branchId { get; set; }
        public string branchName { get; set; }
    }
}
