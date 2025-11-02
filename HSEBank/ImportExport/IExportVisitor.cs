using HSEBank.Main.Entities;

namespace HSEBank.ImportExport
{
    public interface IExportVisitor
    {
        void Visit(BankAccount acc);
        void Visit(Category cat);
        void Visit(Operation op);
        string Result { get; }
    }
}