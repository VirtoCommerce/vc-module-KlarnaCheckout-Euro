using Klarna.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Klarna.Checkout.Euro.KlarnaApi
{
    public interface IKlarnaApi
    {
        ActivateReservationResponse Activate(string s);
        bool CancelReservation(string s);
        string CreditInvoice(string s);
    }
}