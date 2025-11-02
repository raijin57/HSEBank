namespace HSEBank.Commands
{
    public class CommandTimerDecorator : ICommand
    {
        private readonly ICommand _inner;
        private readonly Action<TimeSpan> _report;

        public CommandTimerDecorator(ICommand inner, Action<TimeSpan> report)
        {
            _inner = inner;
            _report = report;
        }

        public void Execute()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _inner.Execute();
            sw.Stop();
            _report(sw.Elapsed);
        }

        public void Undo() => _inner.Undo();
    }
}