using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models
{
    public class SendRequestViewModel
    {
        [Range(1, int.MaxValue)]
        public int NeedId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int ResourceId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
