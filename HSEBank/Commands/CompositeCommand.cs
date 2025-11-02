namespace HSEBank.Commands
{
    public class CompositeCommand : ICommand
    {
        private readonly List<ICommand> _commands = new();

        public CompositeCommand()
        {
        }

        public void Add(ICommand cmd) => _commands.Add(cmd);

        public void Execute()
        {
            foreach (var c in _commands)
            {
                c.Execute();
            }
        }

        public void Undo()
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }

        public bool Any() => _commands.Count > 0;
    }
}