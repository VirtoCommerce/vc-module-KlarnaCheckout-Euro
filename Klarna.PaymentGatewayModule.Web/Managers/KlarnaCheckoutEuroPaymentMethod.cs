using Klarna.Api;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Payment.Model;

namespace Klarna.Checkout.Euro.Managers
{
    public class KlarnaCheckoutEuroPaymentMethod : VirtoCommerce.Domain.Payment.Model.PaymentMethod
    {
        private const string _euroTestBaseUrl = "https://checkout.testdrive.klarna.com/checkout/orders";
        private const string _euroLiveBaseUrl = "https://checkout.klarna.com/checkout/orders";
        private const string _contentType = "application/vnd.klarna.checkout.aggregated-order-v2+json";

        private const string _klarnaModeStoreSetting = "Klarna.Checkout.Euro.Mode";
        private const string _klarnaAppKeyStoreSetting = "Klarna.Checkout.Euro.AppKey";
        private const string _klarnaAppSecretStoreSetting = "Klarna.Checkout.Euro.SecretKey";
        private const string _klarnaTermsUrl = "Klarna.Checkout.Euro.TermsUrl";
        private const string _klarnaCheckoutUrl = "Klarna.Checkout.Euro.CheckoutUrl";
        private const string _klarnaConfirmationUrl = "Klarna.Checkout.Euro.ConfirmationUrl";
        private const string _klarnaPaymentActionType = "Klarna.Checkout.Euro.PaymentActionType";

        private const string _klarnaPurchaseCurrencyStoreSetting = "Klarna.Checkout.Euro.PurchaseCurrency";
        private const string _klarnaPurchaseCountyTwoLetterCodeStoreSetting = "Klarna.Checkout.Euro.PurchaseCountyTwoLetterCode";
        private const string _klarnaLocaleStoreSetting = "Klarna.Checkout.Euro.Locale";

        private const string _klarnaSalePaymentActionType = "Sale";

        public KlarnaCheckoutEuroPaymentMethod()
            : base("KlarnaCheckoutEuro")
        {
        }

        private string Mode
        {
            get
            {
                var retVal = GetSetting(_klarnaModeStoreSetting);
                return retVal;
            }
        }

        private string AppKey
        {
            get
            {
                var retVal = GetSetting(_klarnaAppKeyStoreSetting);
                return retVal;
            }
        }

        private string AppSecret
        {
            get
            {
                var retVal = GetSetting(_klarnaAppSecretStoreSetting);
                return retVal;
            }
        }

        private string TermsUrl
        {
            get
            {
                var retVal = GetSetting(_klarnaTermsUrl);
                return retVal;
            }
        }

        private string ConfirmationUrl
        {
            get
            {
                var retVal = GetSetting(_klarnaConfirmationUrl);
                return retVal;
            }
        }

        private string CheckoutUrl
        {
            get
            {
                var retVal = GetSetting(_klarnaCheckoutUrl);
                return retVal;
            }
        }

        private string PaymentActionType
        {
            get
            {
                var retVal = GetSetting(_klarnaPaymentActionType);
                return retVal;
            }
        }

        private string PurchaseCurrency
        {
            get
            {
                var retVal = GetSetting(_klarnaPurchaseCurrencyStoreSetting);
                return retVal;
            }
        }

        private string PurchaseCountyTwoLetterCode
        {
            get
            {
                var retVal = GetSetting(_klarnaPurchaseCountyTwoLetterCodeStoreSetting);
                return retVal;
            }
        }

        private string Locale
        {
            get
            {
                var retVal = GetSetting(_klarnaLocaleStoreSetting);
                return retVal;
            }
        }

        private bool IsTestMode
        {
            get
            {
                return Mode.ToLower() == "test";
            }
        }

        public override PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.PreparedForm; }
        }

        public override PaymentMethodGroupType PaymentMethodGroupType
        {
            get { return PaymentMethodGroupType.Alternative; }
        }

        public override ProcessPaymentResult ProcessPayment(ProcessPaymentEvaluationContext context)
        {
            var retVal = new ProcessPaymentResult();

            if (context.Order != null && context.Store != null && context.Payment != null)
            {
                string countryName = null;
                string currency = context.Order.Currency.ToString();

                if (context.Order.Addresses != null && context.Order.Addresses.Count > 0)
                {
                    var address = context.Order.Addresses.FirstOrDefault();
                    countryName = address.CountryName;
                }

                retVal = ProcessKlarnaOrder(context);
            }

            return retVal;
        }

        public override PostProcessPaymentResult PostProcessPayment(PostProcessPaymentEvaluationContext context)
        {
            return PostProcessKlarnaOrder(context);
        }

        public override CaptureProcessPaymentResult CaptureProcessPayment(CaptureProcessPaymentEvaluationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Payment == null)
                throw new ArgumentNullException("context.Payment");

            var retVal = new CaptureProcessPaymentResult();

            Uri resourceUri = new Uri(string.Format("{0}/{1}", GetCheckoutBaseUrl(), context.Payment.OuterId));
            var connector = Connector.Create(AppSecret);
            Order order = new Order(connector, resourceUri)
            {
                ContentType = _contentType
            };
            order.Fetch();

            var reservation = order.GetValue("reservation") as string;
            if (!string.IsNullOrEmpty(reservation))
            {
                try
                {
                    var configuration = GetConfiguration();
                    configuration.Eid = Convert.ToInt32(AppKey);
                    configuration.Secret = AppSecret;
                    configuration.IsLiveMode = !IsTestMode;

                    Api.Api api = new Api.Api(configuration);
                    var response = api.Activate(reservation);

                    retVal.NewPaymentStatus = context.Payment.PaymentStatus = PaymentStatus.Paid;
                    context.Payment.CapturedDate = DateTime.UtcNow;
                    context.Payment.IsApproved = true;
                    retVal.IsSuccess = true;
                    retVal.OuterId = context.Payment.OuterId = response.InvoiceNumber;
                }
                catch (Exception ex)
                {
                    retVal.ErrorMessage = ex.Message;
                }
            }
            else
            {
                retVal.ErrorMessage = "No reservation for this order";
            }

            return retVal;
        }

        public override VoidProcessPaymentResult VoidProcessPayment(VoidProcessPaymentEvaluationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Payment == null)
                throw new ArgumentNullException("context.Payment");

            var retVal = new VoidProcessPaymentResult();

            if (!context.Payment.IsApproved && (context.Payment.PaymentStatus == PaymentStatus.Authorized || context.Payment.PaymentStatus == PaymentStatus.Cancelled))
            {
                Uri resourceUri = new Uri(string.Format("{0}/{1}", GetCheckoutBaseUrl(), context.Payment.OuterId));
                var connector = Connector.Create(AppSecret);
                Order order = new Order(connector, resourceUri)
                {
                    ContentType = _contentType
                };
                order.Fetch();

                var reservation = order.GetValue("reservation") as string;
                if (!string.IsNullOrEmpty(reservation))
                {
                    try
                    {
                        var configuration = GetConfiguration();
                        configuration.Eid = Convert.ToInt32(AppKey);
                        configuration.Secret = AppSecret;
                        configuration.IsLiveMode = !IsTestMode;

                        Api.Api api = new Api.Api(configuration);
                        var result = api.CancelReservation(reservation);
                        if (result)
                        {
                            retVal.NewPaymentStatus = context.Payment.PaymentStatus = PaymentStatus.Voided;
                            context.Payment.VoidedDate = context.Payment.CancelledDate = DateTime.UtcNow;
                            context.Payment.IsCancelled = true;
                            retVal.IsSuccess = true;
                        }
                        else
                        {
                            retVal.ErrorMessage = "Payment was not canceled, try later";
                        }
                    }
                    catch (Exception ex)
                    {
                        retVal.ErrorMessage = ex.Message;
                    }
                }
            }
            else if (context.Payment.IsApproved)
            {
                retVal.ErrorMessage = "Payment already approved, use refund";
                retVal.NewPaymentStatus = PaymentStatus.Paid;
            }
            else if (context.Payment.IsCancelled)
            {
                retVal.ErrorMessage = "Payment already canceled";
                retVal.NewPaymentStatus = PaymentStatus.Voided;
            }

            return retVal;
        }

        public override RefundProcessPaymentResult RefundProcessPayment(RefundProcessPaymentEvaluationContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Payment == null)
                throw new ArgumentNullException("context.Payment");

            var retVal = new RefundProcessPaymentResult();

            if (context.Payment.IsApproved && (context.Payment.PaymentStatus == PaymentStatus.Paid || context.Payment.PaymentStatus == PaymentStatus.Cancelled))
            {
                var configuration = GetConfiguration();
                configuration.Eid = Convert.ToInt32(AppKey);
                configuration.Secret = AppSecret;
                configuration.IsLiveMode = !IsTestMode;

                Api.Api api = new Api.Api(configuration);

                var result = api.CreditInvoice(context.Payment.OuterId);
            }

            return retVal;
        }

        public override ValidatePostProcessRequestResult ValidatePostProcessRequest(NameValueCollection queryString)
        {
            var retVal = new ValidatePostProcessRequestResult();

            var klarnaOrder = queryString["klarna_order"];
            var sid = queryString["sid"];

            if (!string.IsNullOrEmpty(klarnaOrder) && !string.IsNullOrEmpty(sid))
            {
                var outerId = HttpUtility.UrlDecode(klarnaOrder).Split('/').LastOrDefault();
                if (!string.IsNullOrEmpty(outerId))
                {
                    retVal.IsSuccess = true;
                    retVal.OuterId = outerId;
                }
            }

            return retVal;
        }

        #region Private Methods

        private ProcessPaymentResult ProcessKlarnaOrder(ProcessPaymentEvaluationContext context)
        {
            var retVal = new ProcessPaymentResult();

            var cartItems = CreateKlarnaCartItems(context.Order);

            //Create cart
            var cart = new Dictionary<string, object> { { "items", cartItems } };
            var data = new Dictionary<string, object>
                    {
                        { "cart", cart }
                    };

            //Create klarna order "http://example.com" context.Store.Url
            var connector = Connector.Create(AppSecret);
            var merchant = new Dictionary<string, object>
                    {
                        { "id", AppKey },
                        { "terms_uri", string.Format("{0}/{1}", context.Store.Url, TermsUrl) },
                        { "checkout_uri", string.Format("{0}/{1}", context.Store.Url, CheckoutUrl) },
                        { "confirmation_uri", string.Format("{0}/{1}?sid=123&orderId={2}&", context.Store.Url, ConfirmationUrl, context.Order.Id) + "klarna_order={checkout.order.uri}" },
                        { "push_uri", string.Format("{0}/{1}?sid=123&orderId={2}&", context.Store.Url, "admin/api/paymentcallback", context.Order.Id) + "klarna_order={checkout.order.uri}" },
                        { "back_to_store_uri", context.Store.Url }
                    };

            var layout = new Dictionary<string, object>
                    {
                        { "layout", "desktop" }
                    };

            data.Add("purchase_country", PurchaseCountyTwoLetterCode);
            data.Add("purchase_currency", PurchaseCurrency);
            data.Add("locale", Locale);
            data.Add("merchant", merchant);
            data.Add("gui", layout);

            var order =
                new Order(connector)
                {
                    BaseUri = new Uri(GetCheckoutBaseUrl()),
                    ContentType = _contentType
                };
            order.Create(data);
            order.Fetch();

            //Gets snippet
            var gui = order.GetValue("gui") as JObject;
            var html = gui["snippet"].Value<string>();

            retVal.IsSuccess = true;
            retVal.NewPaymentStatus = context.Payment.PaymentStatus = PaymentStatus.Pending;
            retVal.HtmlForm = html;
            retVal.OuterId = context.Payment.OuterId = order.GetValue("id") as string;

            return retVal;
        }

        private PostProcessPaymentResult PostProcessKlarnaOrder(PostProcessPaymentEvaluationContext context)
        {
            var retVal = new PostProcessPaymentResult();

            Uri resourceUri = new Uri(string.Format("{0}/{1}", GetCheckoutBaseUrl(), context.OuterId));

            var connector = Connector.Create(AppSecret);

            Order order = new Order(connector, resourceUri)
            {
                ContentType = _contentType
            };

            order.Fetch();
            var status = order.GetValue("status") as string;

            if (status == "checkout_complete")
            {
                var data = new Dictionary<string, object> { { "status", "created" } };
                order.Update(data);
                order.Fetch();
                status = order.GetValue("status") as string;
            }

            if (status == "created" && IsSale())
            {
                var result = CaptureProcessPayment(new CaptureProcessPaymentEvaluationContext { Payment = context.Payment });

                retVal.NewPaymentStatus = context.Payment.PaymentStatus = PaymentStatus.Paid;
                context.Payment.OuterId = retVal.OuterId;
                context.Payment.IsApproved = true;
                context.Payment.CapturedDate = DateTime.UtcNow;
                retVal.IsSuccess = true;
            }
            else if (status == "created")
            {
                retVal.NewPaymentStatus = context.Payment.PaymentStatus = PaymentStatus.Authorized;
                context.Payment.OuterId = retVal.OuterId = context.OuterId;
                context.Payment.AuthorizedDate = DateTime.UtcNow;
                retVal.IsSuccess = true;
            }
            else
            {
                retVal.ErrorMessage = "order not created";
            }

            retVal.OrderId = context.Order.Id;

            return retVal;
        }

        private List<Dictionary<string, object>> CreateKlarnaCartItems(CustomerOrder order)
        {
            var cartItems = new List<Dictionary<string, object>>();
            foreach (var lineItem in order.Items)
            {
                var addedItem = new Dictionary<string, object>();

                addedItem.Add("type", "physical");

                if (!string.IsNullOrEmpty(lineItem.Name))
                {
                    addedItem.Add("name", lineItem.Name);
                }
                if (lineItem.Quantity > 0)
                {
                    addedItem.Add("quantity", lineItem.Quantity);
                }
                if (lineItem.Price > 0)
                {
                    addedItem.Add("unit_price", (int)((lineItem.Price + lineItem.Tax / lineItem.Quantity) * 100));
                    //addedItem.Add("total_price_excluding_tax", (int)(lineItem.Price * lineItem.Quantity * 100));
                }

                if (lineItem.Tax > 0)
                {
                    //addedItem.Add("total_price_including_tax", (int)((lineItem.Price * lineItem.Quantity + lineItem.Tax) * 100));
                    //addedItem.Add("total_tax_amount", (int)(lineItem.Tax * 100));
                    addedItem.Add("tax_rate", (int)(lineItem.TaxDetails.Sum(td => td.Rate) * 10000));
                }
                else
                {
                    addedItem.Add("tax_rate", 0);
                }

                addedItem.Add("discount_rate", 0);
                addedItem.Add("reference", lineItem.ProductId);

                cartItems.Add(addedItem);
            }

            if (order.Shipments != null && order.Shipments.Any(s => s.Sum > 0))
            {
                foreach (var shipment in order.Shipments.Where(s => s.Sum > 0))
                {
                    var addedItem = new Dictionary<string, object>();

                    addedItem.Add("type", "shipping_fee");
                    addedItem.Add("reference", "SHIPPING");
                    addedItem.Add("name", "Shipping Fee");
                    addedItem.Add("quantity", 1);
                    addedItem.Add("unit_price", (int)(shipment.Sum * 100));

                    addedItem.Add("tax_rate", 0);

                    cartItems.Add(addedItem);
                }
            }

            return cartItems;
        }

        private bool IsSale()
        {
            return PaymentActionType.Equals(_klarnaSalePaymentActionType);
        }

        private string GetCheckoutBaseUrl()
        {
            return IsTestMode ? _euroTestBaseUrl : _euroLiveBaseUrl;
        }

        private string GetApiBaseUrl()
        {
            return IsTestMode ? _euroTestBaseUrl : _euroLiveBaseUrl;
        }

        private Configuration GetConfiguration()
        {
            return new Configuration(GetCountryCode(), GetLanguageCode(), GetCurrencyCode(), GetEncoding());
        }

        private Encoding GetEncoding()
        {
            var retVal = Encoding.Sweden;

            switch (Locale)
            {
                case "da-dk":
                    retVal = Encoding.Denmark;
                    break;

                case "de-at":
                    retVal = Encoding.Austria;
                    break;

                case "nb-no":
                    retVal = Encoding.Norway;
                    break;

                case "fi-fi":
                    retVal = Encoding.Finland;
                    break;

                case "de-de":
                    retVal = Encoding.Germany;
                    break;

                case "sv-se":
                    retVal = Encoding.Sweden;
                    break;
            }

            return retVal;
        }

        private Currency.Code GetCurrencyCode()
        {
            var retVal = Currency.Code.SEK;

            switch (PurchaseCurrency)
            {
                case "DKK":
                    retVal = Currency.Code.DKK;
                    break;

                case "EUR":
                    retVal = Currency.Code.EUR;
                    break;

                case "NOK":
                    retVal = Currency.Code.NOK;
                    break;

                case "SEK":
                    retVal = Currency.Code.SEK;
                    break;
            }

            return retVal;
        }

        private Language.Code GetLanguageCode()
        {
            var retVal = Language.Code.SV;

            switch (Locale)
            {
                case "da-dk":
                    retVal = Language.Code.DA;
                    break;

                case "de-at":
                    retVal = Language.Code.DE;
                    break;

                case "nb-no":
                    retVal = Language.Code.NB;
                    break;

                case "fi-fi":
                    retVal = Language.Code.FI;
                    break;

                case "de-de":
                    retVal = Language.Code.DE;
                    break;

                case "sv-se":
                    retVal = Language.Code.SV;
                    break;
            }

            return retVal;
        }

        private Country.Code GetCountryCode()
        {
            var retVal = Country.Code.SE;

            switch (PurchaseCountyTwoLetterCode)
            {
                case "DK":
                    retVal = Country.Code.DK;
                    break;

                case "AT":
                    retVal = Country.Code.AT;
                    break;

                case "NO":
                    retVal = Country.Code.NO;
                    break;

                case "FI":
                    retVal = Country.Code.FI;
                    break;

                case "DE":
                    retVal = Country.Code.DE;
                    break;

                case "SE":
                    retVal = Country.Code.SE;
                    break;
            }

            return retVal;
        }

        #endregion
    }
}