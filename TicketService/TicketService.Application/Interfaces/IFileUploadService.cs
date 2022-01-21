using System.Threading.Tasks;
using TicketService.Domain.Entities;

namespace TicketService.Application.Interfaces
{
    public interface IFileUploadService
    {
        Task FileUploadSftp(LogFile fileLog);

        Task FileUploadServer(LogFile fileLog);
    }
}