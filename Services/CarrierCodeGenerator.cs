namespace YardOps.Services
{
    public class CarrierCodeGenerator
    {
        public static string GenerateNextCode(int currentMaxId)
        {
            int nextNumber = currentMaxId + 1;
            return $"CAR-{nextNumber:D3}";  // D3 = pad with zeros (001, 002, etc.)
        }
    }
}