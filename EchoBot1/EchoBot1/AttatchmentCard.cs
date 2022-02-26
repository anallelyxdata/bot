using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1
{
    public class AttatchmentCard
    {
        public static Activity GetDocumento(string tipoFormato)
        {
            var documentoCard = new Attachment();
            documentoCard.Name = $"Formato {tipoFormato}";
            documentoCard.ContentType = "application/pdf";
            switch (tipoFormato)
            {
                case "vacaciones":
                    documentoCard.ContentUrl = "https://apimex.org/esp/intranet/formatosRH/EJEMPLO_FORMATO_SOLICITUD_VACACIONES.pdf";
                    break;
                case "permiso":
                    documentoCard.ContentUrl = "https://ucienegam.mx/wp-content/uploads/2018/08-Doc/Administracion/Formatos/FORMATOS_R.H..pdf";
                    break;
                case "acceso":
                    documentoCard.ContentUrl = "https://www.ipomex.org.mx/recursos/ipo/files_ipo/2017/20/3/82269069831e5b2faf75b8799ca948b8.pdf";
                break;
            }
            

            var reply = MessageFactory.Attachment(documentoCard);
            return reply as Activity;
        }
    }
}
