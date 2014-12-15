using System.Net;

namespace HttpReactor
{
    public interface IEndPoints
    {
        EndPoint Next();
    }
}
