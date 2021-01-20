
namespace MW.Blazor
{
    public class TreeStyle
    {
        public static readonly TreeStyle Bootstrap = new TreeStyle
        {
            ExpandNodeIconClass = "far fa-plus-square curosr-pointer",
            CollapseNodeIconClass = "far fa-minus-square curosr-pointer",
            NodeTitleClass = "p-1 curosr-pointer",
            NodeTitleSelectedClass = "bg-primary text-white"
        };

        public string ExpandNodeIconClass { get; set; }
        public string CollapseNodeIconClass { get; set; }
        public string NodeTitleClass { get; set; }
        public string NodeTitleSelectedClass { get; set; }
    }
}
