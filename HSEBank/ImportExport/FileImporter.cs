namespace HSEBank.ImportExport
{
    public abstract class FileImporter
    {
        public void Import(string path)
        {
            var content = File.ReadAllText(path);
            var records = Parse(content);
            foreach (var rec in records)
            {
                ProcessRecord(rec);
            }
        }

        protected abstract IEnumerable<Dictionary<string, string>> Parse(string content);
        protected abstract void ProcessRecord(Dictionary<string, string> record);
    }
}