using API_ZAD_1.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace API_ZAD_1.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

    public async Task<IActionResult> getAllTripsForClient(int id) // metody asynchroniczne zwracają Taska a w środku rezultat
    {
        var toBeReturned = new Dictionary<int, TripsDTO>();
        
        string query = @"SELECT 
                    T.IdTrip, T.Name AS TripName, T.Description, T.DateFrom, T.DateTo, T.MaxPeople,
                    C.IdCountry,ClT.RegisteredAt, ClT.PAYMENTDATE, C.Name as CountryName
                 FROM Trip T
                 JOIN Country_Trip CT ON T.IdTrip = CT.IdTrip
                 JOIN Country C ON CT.IdCountry = C.IdCountry
                 JOIN Client_Trip CLT ON T.IdTrip = CLT.IdTrip
                 JOIN Client CL ON CLT.IdClient = CL.IdClient
                 WHERE CL.IdClient = @id";  // WYSZUKUJE WSZYSTKIE WYCIECZKI DLA DANEGO KLIENTA

        await using var conn = new SqlConnection(_connectionString); // połączenie, dzięki using nie muszę korzystać z dispose
        await using var cmd = new SqlCommand(query, conn); // to co zostaje wysłane
        cmd.Parameters.AddWithValue("@id", id); //AddWithValue  

        await conn.OpenAsync();
        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

        
        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));
            if (!toBeReturned.ContainsKey(tripId))
            {
                toBeReturned[tripId] = new TripsDTO
                {
                    id = tripId,
                    name =(string)reader["TripName"],
                    description =(string)reader["Description"],
                    start_date = (DateTime)reader["DateFrom"],
                    end_date = (DateTime)reader["DateTo"],
                    max_people = (int)reader["MaxPeople"],
                    registeredAt = (int)reader["RegisteredAt"],
                    paymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? null : reader.GetInt32(reader.GetOrdinal("PaymentDate")), // wartość może być nullem
                    countries = new List<CountriesDTO>()
                };
            }

            var country = new CountriesDTO
            {
                country_code = (int)reader["IdCountry"],
                country_name =(string)reader["CountryName"] 

            };

            toBeReturned[tripId].countries.Add(country);
        }
        
        return new OkObjectResult(toBeReturned.Values.ToList());
    }
    public async Task<bool> DoesClientExist(int id)
    {
        string query = @"SELECT COUNT(1) FROM Client WHERE IdClient = @id";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);

        await conn.OpenAsync();
        var result = (int)await cmd.ExecuteScalarAsync(); // zwraca pierwszą kolumnę pierwszego wiersza wyniku
        
        return result > 0;
    }
    public async Task<ClientsDTO> AddClientRawAsync(ClientsDTO client)
    {
        string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

        await conn.OpenAsync();
        client.idClient = (int)await cmd.ExecuteScalarAsync(); // Zwraca nowo stworzone ID dzieki OUTPUT

        return client;
    }

}