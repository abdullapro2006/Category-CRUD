using Pustok.Database.Abstracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pustok.Database.DomainModels;
public class Category : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}
