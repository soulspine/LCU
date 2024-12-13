namespace WildRune.DTOs.LOL
{
    public class Item
    {
        public bool canUse { get; set; }
        public bool consumable { get; set; }
        public int count { get; set; }
        public string displayName { get; set; }
        public int itemID { get; set; }
        public int price { get; set; }
        public string rawDescription { get; set; }
        public string rawDisplayName { get; set; }
        public int slot { get; set; }
    }
}
