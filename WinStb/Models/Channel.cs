namespace WinStb.Models
{
    public class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Cmd { get; set; }
        public string Logo { get; set; }
        public int? Hd { get; set; }
        public int? Lock { get; set; }
        public int? Fav { get; set; }
        public string GenreTitle { get; set; }
        public bool? HasArchive { get; set; }
    }
}
