namespace SigortaPro.Domain.Entities;

// Sağlık teklifinde "başkası adına" sigortalanan kişi (sigortalı ≠ sigorta ettiren).
// Quote'a gömülü (owned) değer nesnesidir; kendi kimliği/tablosu yoktur ve yalnızca poliçe sahibinin
// beyanıyla oluşturulur. Fiyatlamada yaş, bu kişinin doğum tarihinden hesaplanır.
public class InsuredPerson
{
    protected InsuredPerson()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Tckn = string.Empty;
        PhoneNumber = string.Empty;
        Relationship = string.Empty;
    }

    public InsuredPerson(
        string firstName,
        string lastName,
        string tckn,
        DateTime birthDate,
        string phoneNumber,
        string relationship)
    {
        FirstName = firstName;
        LastName = lastName;
        Tckn = tckn;
        BirthDate = birthDate;
        PhoneNumber = phoneNumber;
        Relationship = relationship;
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Tckn { get; private set; }
    public DateTime BirthDate { get; private set; }
    public string PhoneNumber { get; private set; }

    // Sigorta ettirene yakınlık derecesi (ör. "Eş", "Çocuk", "Anne", "Baba").
    public string Relationship { get; private set; }

    public string FullName => $"{FirstName} {LastName}";
}
