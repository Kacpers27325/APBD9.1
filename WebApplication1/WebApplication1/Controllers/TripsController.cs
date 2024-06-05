using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1.Controllers;


[Route("api/[controller]")] 
[ApiController] 
public class TripsController : ControllerBase 
{ 
    private readonly MasterContext _context; 
     
    public TripsController(MasterContext context) 
    { 
        _context = context; 
    } 
    
    [HttpGet]
    public async Task<ActionResult<TripResponse>> GetTrips([FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        var trips = _context.Trips
            .Select(e => new TripDTO()
            {
                Name = e.Name,
                DateFrom = e.DateFrom,
                MaxPeople = e.MaxPeople,
                ClientTrips = e.ClientTrips.Select(e => new ClientDTO()
                {
                    FirstName = e.IdClientNavigation.FirstName,
                    LastName = e.IdClientNavigation.LastName
                }),
                Countries = e.IdCountries.Select(c => new CountryDTO() { Name = c.Name })
            })
            .OrderBy(e => e.DateFrom);

        var count = await trips.CountAsync();
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);

        trips = (IOrderedQueryable<TripDTO>)trips.Skip((page - 1) * pageSize).Take(pageSize);

        var result = new TripResponse
        {
            PageNum = page,
            PageSize = pageSize,
            AllPages = totalPages,
            Trips = await trips.ToListAsync()
        };
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
       var client = await _context.Clients
                       .Include(c => c.ClientTrips)
                       .FirstOrDefaultAsync(c => c.IdClient == id);
       if (client == null)
       {
           return NotFound("Client not found");
       }
       if(client.ClientTrips.Any())
       {
           return BadRequest("Client is included in trip, cannot be deleted");
       }
       _context.Clients.Remove(client);
       await _context.SaveChangesAsync();
       return NoContent();
    }

[HttpPost("{idTrip}/clients")]
        public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] ClientDTO clientDto)
        {
            var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == clientDto.Pesel);
            if (existingClient != null)
                return BadRequest("Client already exists");
            
            var isClientAssigned = await _context.ClientTrips.AnyAsync(ct => ct.IdClientNavigation.Pesel == clientDto.Pesel && ct.IdTrip == idTrip);
            if (isClientAssigned)
                return BadRequest("Client already assigned to this trip");
            
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.DateFrom > DateTime.Now);
            if (trip == null)
                return NotFound("It is after trip");
            
            
            var newClient = new Client
            {
                FirstName = clientDto.FirstName,
                LastName = clientDto.LastName,
                Email = clientDto.Email,
                Telephone = clientDto.Telephone,
                Pesel = clientDto.Pesel,
                ClientTrips = new List<ClientTrip>()
            };
            var existingClientTrip = await _context.ClientTrips
                .FirstOrDefaultAsync(ct => ct.IdClientNavigation.Pesel == clientDto.Pesel && ct.IdTrip == idTrip);

            var clientTrip = new ClientTrip
            {
                IdClientNavigation = newClient,
                IdTripNavigation = trip,
                RegisteredAt = DateTime.Now,
                PaymentDate = existingClientTrip.PaymentDate
            };

            _context.Clients.Add(newClient);
            _context.ClientTrips.Add(clientTrip);
            
            await _context.SaveChangesAsync();
            return Ok("Client assigned");
        }


    public class TripResponse
    {
        public int PageNum { get; set; }
        public int PageSize { get; set; }
        public int AllPages { get; set; }
        public List<TripDTO> Trips { get; set; }
    }
}



