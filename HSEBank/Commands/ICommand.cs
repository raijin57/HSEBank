namespace HSEBank.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}