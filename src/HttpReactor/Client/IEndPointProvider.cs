using System.Net;

namespace HttpReactor.Client
{
    public interface IEndPointProvider
    {
        EndPoint Next();
    }
}
