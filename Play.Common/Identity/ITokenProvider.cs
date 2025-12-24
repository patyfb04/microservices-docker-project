using System.Threading;
using System.Threading.Tasks;

namespace Play.Common.Identity
{
    public interface ITokenProvider
    {
        Task<string> GetTokenAsync(CancellationToken ct = default);
    }
}
