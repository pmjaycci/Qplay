namespace Request
{
    public class LoadTable
    {
        public float Version { get; set; }
    }
    public class Login
    {
        public string? Id { get; set; }
        public string? Password { get; set; }
    }
}