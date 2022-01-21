using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TicketService.Application.Interfaces;

namespace TicketService.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketAnalyzer _analyzer;
        private readonly ICalendarGeneratorService _calendarGeneratorService;
        private readonly ITicketService _service;
        private readonly IStartTicketPlanningTool _startTicketPlanningTool;

        public TicketsController(
            ITicketService service,
            ITicketAnalyzer analyzer,
            ICalendarGeneratorService calendarGeneratorService, IStartTicketPlanningTool startTicketPlanningTool)
        {
            _service = service;
            _analyzer = analyzer;
            _calendarGeneratorService = calendarGeneratorService;
            _startTicketPlanningTool = startTicketPlanningTool;
        }

        /// <summary>
        ///     Retrieves a list of tickets
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetAll(int amount = 10, int page = 1)
        {
            var tickets = await _service.GetAll(10, page);
            return Ok(tickets);
        }

        /// <summary>
        ///     Retrieves a single ticket by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var singleTicket = await _service.Get(id);
            return Ok(singleTicket);
        }

        [HttpGet("start")]
        public async Task<ActionResult> Start()
        {
            await _startTicketPlanningTool.Start();
            return Ok();
        }
    }
}