namespace Common.Commands
{
    public class Done
    {
        public string Name { get; }
        public string Value { get; }

        public Done(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
