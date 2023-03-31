namespace HostDns
{
    public class HostItem
    {
        private readonly string _source;

        public HostItem(string source)
        {
            _source = source;
            IsComment = source.TrimStart().StartsWith("#");
            if (IsComment) return;
            var kv = source.Trim().Split(" ");
            if (kv.Length < 2)
            {
                IsComment = true;
                return;
            }

            Ip = kv[0].Trim();
            Domain = kv[1].Trim();
        }

        public bool IsComment { get; }

        public string Ip { get; set; }

        public string Domain { get; set; }

        public override string ToString()
        {
            return IsComment ? _source : $"{Ip} {Domain}";
        }
    }
}