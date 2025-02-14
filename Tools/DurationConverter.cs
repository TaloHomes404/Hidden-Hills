namespace Hidden_Hills.Tools
{
    public class Duration
    {
        public int Milliseconds { get; set; }

        public override string ToString()
        {
            return Milliseconds switch
            {
                30000 => "30 seconds",
                60000 => "1 minute",
                180000 => "3 minutes",
                300000 => "5 minutes",
                600000 => "10 minutes",
                _ => $"{Milliseconds / 60000} minuts"
            };
        }
    }
}
