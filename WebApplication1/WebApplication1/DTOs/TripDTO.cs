
namespace WebApplication1.DTOs;

public class TripDTO
{
    public int IdTrip { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public int MaxPeople { get; set; }

    public IEnumerable<ClientDTO> ClientTrips { get; set; }

    public IEnumerable<CountryDTO> Countries { get; set; }
}