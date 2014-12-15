using System.Net;

namespace HttpReactor.Client
{
    public interface IEndPoints
    {
        EndPoint Next();
    }
}
