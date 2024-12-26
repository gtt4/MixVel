using Microsoft.AspNetCore.Mvc;
using MixVel.Interfaces;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MixVel.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(SearchResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SearchAsync([FromBody] SearchRequest request, CancellationToken cancellationToken)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _searchService.SearchAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return BadRequest("Request was canceled.");
            }
            catch (Exception ex)
            {
                // Log the exception (using a logging framework)
                return StatusCode((int)HttpStatusCode.InternalServerError, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpGet("ping")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var isAvailable = await _searchService.IsAvailableAsync(cancellationToken);
                return Ok(isAvailable);
            }
            catch (OperationCanceledException)
            {
                return BadRequest("Request was canceled.");
            }
            catch (Exception ex)
            {
                // Log the exception (using a logging framework)
                return StatusCode((int)HttpStatusCode.InternalServerError, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }
    }
}
