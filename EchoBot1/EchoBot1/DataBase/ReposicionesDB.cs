using EchoBot1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.DataBase
{
    public class ReposicionesDB
    {
        public static List<ReposicionModel> GetReposicions()
        {
            var lista = new List<ReposicionModel>()
            {
                new ReposicionModel()
                {
                    titulo="Credencial de ingreso",
                    imagen="https://www.elcontribuyente.mx/wp-content/uploads/2020/04/WhatsApp-Image-2020-04-17-at-1.36.48-PM.jpeg"
                },
                new ReposicionModel()
                {
                    titulo="Tarjeta de vales de despensa",
                    imagen="https://www.elcontribuyente.mx/wp-content/uploads/2020/04/WhatsApp-Image-2020-04-17-at-1.36.48-PM.jpeg"
                }
            };
            return lista;
        }
    }
}
