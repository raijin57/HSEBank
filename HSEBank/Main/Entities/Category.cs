using HSEBank.ImportExport;

namespace HSEBank.Main.Entities
{
    public class Category
    {
        public Guid Id { get; }
        public CategoryType Type { get; }
        public string Name { get; private set; }

        public Category(Guid id, CategoryType type, string name)
        {
            Id = id;
            Type = type;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public void Accept(IExportVisitor visitor) => visitor.Visit(this);
    }
}