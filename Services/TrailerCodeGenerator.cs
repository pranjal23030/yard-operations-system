namespace YardOps.Services
{
    public class TrailerCodeGenerator
    {
        public static string GenerateNextCode(int currentMaxId)
        {
            int nextNumber = currentMaxId + 1;
            return $"TRL-{nextNumber:D3}";  // D3 = pad with zeros (001, 002, etc.)
        }
    }
}