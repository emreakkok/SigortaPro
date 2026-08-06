using SigortaPro.Domain.Common;
using SigortaPro.Domain.Constants;

namespace SigortaPro.Domain.Entities;

public class Customer : BaseEntity, IAggregateRoot
{
    protected Customer()
    {
    }

    public Customer(Guid appUserId, string firstName, string lastName, string tckn, DateTime birthDate, string phoneNumber, Address address)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != BusinessConstants.TcknLength)
        {
            throw new DomainException("TCKN 11 haneli olmalıdır.");
        }

        Id = Guid.NewGuid();
        AppUserId = appUserId;
        FirstName = firstName;
        LastName = lastName;
        Tckn = tckn;
        BirthDate = birthDate;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    // Identity kullanıcısına (Persistence katmanındaki AppUser : IdentityUser<Guid>) yalnızca Id ile bağlanır;
    // Domain, Identity tipini bilmez.
    public Guid AppUserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Tckn { get; private set; } = string.Empty;
    public DateTime BirthDate { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;

    public ICollection<Vehicle> Vehicles { get; private set; } = new List<Vehicle>();
    public ICollection<Property> Properties { get; private set; } = new List<Property>();
    public ICollection<Quote> Quotes { get; private set; } = new List<Quote>();
    public ICollection<Policy> Policies { get; private set; } = new List<Policy>();
    public ICollection<Claim> Claims { get; private set; } = new List<Claim>();

    public void UpdateContactInfo(string phoneNumber, Address address)
    {
        PhoneNumber = phoneNumber;
        Address = address;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
