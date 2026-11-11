using Kriseportal.Web.Models.Entities; 

namespace Kriseportal.Web.Models
{
    public class MatchingSuggestionsViewModel
    {
        public int NeedId { get; set; }
        public List<Resource> Resources { get; set; } = new();
    }
}
