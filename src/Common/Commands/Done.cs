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

    public class Start
    {
        public Start(string broker, string connectionString)
        {
            Broker = broker;
            ConnectionString = connectionString;
        }

        public string Broker { get; }
        public string ConnectionString { get; }
    }
}
