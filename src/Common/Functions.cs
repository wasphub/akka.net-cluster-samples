using System.Threading.Tasks;
using System.Linq;

namespace Common
{
    public static class Functions
    {
        public static async Task<string> Reverse(string input)
        {
            await Task.Delay(5000);

            return string.Join(null, input.Reverse());
        }
    }
}
