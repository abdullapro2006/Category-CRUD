using Pustok.Database.Abstracts;

namespace Pustok.Database.DomainModels
{
    public class ProductCategory : IEntity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }


    }
}
