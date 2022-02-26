using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Helpers
{
    public class ConexionDB
    {
        public static async Task<string> RegistroTicketDB(String idTicket, String tipo)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "107.180.47.15",
                Database = "interproteccion-test",
                UserID = "koj8s2gxf97o",
                Password = "X@data1234",
            };

                using (var conn = new MySqlConnection(builder.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO RegistroTickets (idTicket, tipo, estatus) VALUES (@idTicket, @tipo, @estatus);";
                        command.Parameters.AddWithValue("@idTicket", idTicket);
                        command.Parameters.AddWithValue("@tipo", tipo);
                        command.Parameters.AddWithValue("@estatus", "En revisión");

                        int rowCount = await command.ExecuteNonQueryAsync();
                    }
                    return "CORRECTO";
                }
            
        }

        public static async Task<string> RegistrarPreguntaDB(String idPregunta, String pregunta)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "107.180.47.15",
                Database = "interproteccion-test",
                UserID = "koj8s2gxf97o",
                Password = "X@data1234",
            };
           
                using (var conn = new MySqlConnection(builder.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO RegistroPreguntas (idPregunta, pregunta) VALUES (@idPregunta, @pregunta);";
                        command.Parameters.AddWithValue("@idPregunta", idPregunta);
                        command.Parameters.AddWithValue("@pregunta", pregunta);

                        int rowCount = await command.ExecuteNonQueryAsync();
                    }
                    return "CORRECTO";
                }
        }

    }
}
