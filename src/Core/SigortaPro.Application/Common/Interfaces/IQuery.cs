using MediatR;

namespace SigortaPro.Application.Common.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
