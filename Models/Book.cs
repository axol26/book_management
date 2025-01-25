namespace book_management.Models
{
    public class Book
    {
        public string BookId { get; set; }
        public string BookName { get; set; }
        public string Author { get; set; }
        public string Language { get; set; }
        public string Genre { get; set; }
        public int CurrentStock { get; set; }
        public int IssuedBooks { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }
} 