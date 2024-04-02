using System;
using System.Net.Http;
using MySql.Data.MySqlClient;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando migración de datos del país...");
        MigrateCountryData();
        Console.WriteLine("Migración de datos del país finalizada.");
    }

    static void MigrateCountryData()
    {
        try
        {
            // Conexion a la base de datos
            string connectionString = "server=localhost;user=root;password=root;database=tp2";
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = connection.CreateCommand();

            // Iterar sobre los códigos del 1 al 300
            for (int code = 1; code <= 300; code++)
            {
                command.Parameters.Clear(); // Limpiar los parametros antes de agregar nuevos

                string url = $"https://restcountries.com/v2/callingcode/{code}";
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = response.Content.ReadAsStringAsync().Result;
                        dynamic countryData = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);

                        if (countryData != null)
                        {
                            dynamic country = countryData[0]; // Suponiendo que siempre retorna una lista con al menos un elemento

                            // Obtener los datos del pais
                            string nombrePais = country.name;
                            string capitalPais = country.capital ?? ""; // Se asigna una cadena vacia como valor predeterminado
                            string region = country.region;
                            int poblacion = country.population;
                            double? latitud = country.latlng != null ? country.latlng[0] : null;
                            double? longitud = country.latlng != null ? country.latlng[1] : null;
                            string codigoPais = country.numericCode;

                            // Buscar pais en la base de datos
                            command.CommandText = "SELECT * FROM Pais WHERE codigoPais = @codigoPais";
                            command.Parameters.AddWithValue("@codigoPais", codigoPais);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // Actualizar registro en la tabla pais
                                    reader.Close();
                                    command.CommandText = "UPDATE Pais SET nombrePais = @nombrePais, capitalPais = @capitalPais, region = @region, poblacion = @poblacion, latitud = @latitud, longitud = @longitud WHERE codigoPais = @codigoPais";
                                    command.Parameters.AddWithValue("@nombrePais", nombrePais);
                                    command.Parameters.AddWithValue("@capitalPais", capitalPais);
                                    command.Parameters.AddWithValue("@region", region);
                                    command.Parameters.AddWithValue("@poblacion", poblacion);
                                    command.Parameters.AddWithValue("@latitud", latitud);
                                    command.Parameters.AddWithValue("@longitud", longitud);
                                    command.ExecuteNonQuery();
                                }
                                else
                                {
                                    reader.Close();
                                    // Insertar nuevo registro en la tabla país
                                    command.CommandText = "INSERT INTO Pais (codigoPais, nombrePais, capitalPais, region, poblacion, latitud, longitud) VALUES (@codigoPais, @nombrePais, @capitalPais, @region, @poblacion, @latitud, @longitud)";
                                    command.Parameters.AddWithValue("@nombrePais", nombrePais);
                                    command.Parameters.AddWithValue("@capitalPais", capitalPais);
                                    command.Parameters.AddWithValue("@region", region);
                                    command.Parameters.AddWithValue("@poblacion", poblacion);
                                    command.Parameters.AddWithValue("@latitud", latitud);
                                    command.Parameters.AddWithValue("@longitud", longitud);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            connection.Close();
        }
        catch (MySqlException ex)
        {
            Console.WriteLine("Error al conectarse a la base de datos: " + ex.Message);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("Error al realizar la solicitud HTTP: " + ex.Message);
        }
    }
}
