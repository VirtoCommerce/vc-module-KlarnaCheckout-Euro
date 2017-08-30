using System;
using System.Collections.ObjectModel;
using System.Linq;
using Klarna.Checkout.Euro.Managers;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Payment.Model;
using VirtoCommerce.Platform.Core.Settings;
using Newtonsoft.Json;
using System.IO;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using Moq;
using Klarna.Checkout;
using Klarna.Checkout.HTTP;
using System.Net;
using Klarna.Api;
using Klarna.Checkout.Euro.KlarnaApi;
using Xunit;

namespace PaymentMethods.Tests
{
    public class KlarnaCheckoutEuroTests
    {
        [Fact]
        public void SuccessProcessPayment()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(false);

            var processPaymentEvaluationContext = method.ProcessPayment(new ProcessPaymentEvaluationContext
                {
                    Order = order,
                    Payment = order.InPayments.First(),
                    Store = store
                });

            Assert.True(!string.IsNullOrEmpty(order.InPayments.First().OuterId));
            Assert.True(processPaymentEvaluationContext.IsSuccess);
            Assert.True(!string.IsNullOrEmpty(processPaymentEvaluationContext.HtmlForm));
        }

        [Fact]
        public void SuccessPostProcessPaymentAuthorize()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(false);

            method.ProcessPayment(new ProcessPaymentEvaluationContext
                {
                    Order = order,
                    Payment = order.InPayments.First(),
                    Store = store
                });

            var postProcessPaymentResult = method.PostProcessPayment(new PostProcessPaymentEvaluationContext
                {
                    Order = order,
                    Payment = order.InPayments.First(),
                    Store = store,
                    OuterId = order.InPayments.First().OuterId
            });

            Assert.True(postProcessPaymentResult.IsSuccess);
            Assert.Equal(PaymentStatus.Authorized, postProcessPaymentResult.NewPaymentStatus);
            Assert.Equal(PaymentStatus.Authorized, order.InPayments.First().PaymentStatus);
        }

        [Fact]
        public void SuccessPostProcessPaymentSale()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(true);

            method.ProcessPayment(new ProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store
            });

            var postProcessPaymentResult = method.PostProcessPayment(new PostProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store,
                OuterId = order.InPayments.First().OuterId
            });

            Assert.True(postProcessPaymentResult.IsSuccess);
            Assert.Equal(PaymentStatus.Paid, postProcessPaymentResult.NewPaymentStatus);
            Assert.Equal(PaymentStatus.Paid, order.InPayments.First().PaymentStatus);
        }

        [Fact]
        public void SuccessCapturePaymentTest()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(false);

            method.ProcessPayment(new ProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store
            });

            method.PostProcessPayment(new PostProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store,
                OuterId = order.InPayments.First().OuterId
            });

            var capturePaymentResult = method.CaptureProcessPayment(new CaptureProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First()
            });

            Assert.True(capturePaymentResult.IsSuccess);
            Assert.Equal(PaymentStatus.Paid, capturePaymentResult.NewPaymentStatus);
            Assert.Equal(PaymentStatus.Paid, order.InPayments.First().PaymentStatus);
        }

        [Fact]
        public void SuccessVoidPaymentTest()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(false);

            method.ProcessPayment(new ProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store
            });

            method.PostProcessPayment(new PostProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store,
                OuterId = order.InPayments.First().OuterId
            });

            var voidPaymentResult = method.VoidProcessPayment(new VoidProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First()
            });

            Assert.True(voidPaymentResult.IsSuccess);
            Assert.Equal(PaymentStatus.Voided, voidPaymentResult.NewPaymentStatus);
            Assert.Equal(PaymentStatus.Voided, order.InPayments.First().PaymentStatus);
        }

        [Fact]
        public void SuccessRefundPaymentTest()
        {
            var orderJson = File.ReadAllText(@"C:\PLATFORM\vc-module-KlarnaCheckout-Euro\PaymentMethods.Tests\order.json");
            var order = JsonConvert.DeserializeObject<CustomerOrder>(orderJson);
            order.Id = Guid.NewGuid().ToString();
            var store = new Store { Url = "http://localhost/storefront" };

            var method = GetMethod(true);

            method.ProcessPayment(new ProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store
            });

            method.PostProcessPayment(new PostProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First(),
                Store = store,
                OuterId = order.InPayments.First().OuterId
            });

            var voidPaymentResult = method.RefundProcessPayment(new RefundProcessPaymentEvaluationContext
            {
                Order = order,
                Payment = order.InPayments.First()
            });

            Assert.True(voidPaymentResult.IsSuccess);
            Assert.Equal(PaymentStatus.Refunded, voidPaymentResult.NewPaymentStatus);
            Assert.Equal(PaymentStatus.Refunded, order.InPayments.First().PaymentStatus);
        }

        private KlarnaCheckoutEuroPaymentMethod GetMethod(bool isSale)
        {
            var settings = new Collection<SettingEntry>();

            settings.AddRange(new[] {
                new SettingEntry { Name = "Klarna.Checkout.Euro.AppKey", ValueType = SettingValueType.Integer, Value = "1" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.SecretKey", ValueType = SettingValueType.SecureString, Value = "secret" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.Mode", Value = "test" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.TermsUrl", Value = "checkout/terms" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.CheckoutUrl", Value = "cart/checkout/#/shipping-address" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.ConfirmationUrl", Value = "cart/externalpaymentcallback" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.PurchaseCountyTwoLetterCode", Value = "SE" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.PurchaseCurrency", Value = "SEK" },
                new SettingEntry { Name = "Klarna.Checkout.Euro.Locale", Value = "sv-se" }
            });

            if (!isSale)
            {
                settings.Add(new SettingEntry { Name = "Klarna.Checkout.Euro.PaymentActionType", Value = "Authorization/Capture" });
            }
            else
            {
                settings.Add(new SettingEntry { Name = "Klarna.Checkout.Euro.PaymentActionType", Value = "Sale" });
            }

            var klarnaCheckoutEuroPaymentMethod = new KlarnaCheckoutEuroPaymentMethod
            {
                Settings = settings
            };

            Mock<IConnector> connector = new Mock<IConnector>();

            klarnaCheckoutEuroPaymentMethod.ApiConnector = GetMockConnector();
            klarnaCheckoutEuroPaymentMethod.KlarnaApi = GetMockKlarnaApi();

            return klarnaCheckoutEuroPaymentMethod;
        }

        private IKlarnaApi GetMockKlarnaApi()
        {
            var mockKlarnaApi = new Mock<IKlarnaApi>();

            mockKlarnaApi.Setup(k => k.Activate(It.IsAny<string>())).Returns(new ActivateReservationResponse { InvoiceNumber = "InvoiceNumber" });
            mockKlarnaApi.Setup(k => k.CancelReservation(It.IsAny<string>())).Returns(true);
            mockKlarnaApi.Setup(k => k.CreditInvoice(It.IsAny<string>())).Returns("RefundNumber");

            return mockKlarnaApi.Object;
        }

        private IConnector GetMockConnector()
        {
            var url = new Uri("http://klarna.com");
            var secret = "My Secret";
            var digest = new Digest();
            var httpTransportMock = new Mock<IHttpTransport>();

            var createdresponseMock = new Mock<IHttpResponse>();

            //Base
            PrepareBaseMock(httpTransportMock);
            PreparePostProcessMock(httpTransportMock);
            

            httpTransportMock.Setup(t => t.CreateRequest(It.IsAny<Uri>())).Returns((HttpWebRequest)WebRequest.Create("http://www.contoso.com/"));

            IConnector connector = new BasicConnector(httpTransportMock.Object, digest, secret, url);

            return connector;
        }

        private void PrepareBaseMock(Mock<IHttpTransport> httpTransportMock)
        {
            var url = new Uri("http://klarna.com");

            var responseMock = new Mock<IHttpResponse>();

            dynamic data =
                new
                {
                    gui = new { snippet = "iframe" },
                    id = "SomeOuterId",
                    status = "checkout_complete",
                    reservation = "reservationNumber"
                };
            string dataJson = JsonConvert.SerializeObject(data);

            responseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(r => r.Header("Location")).Returns(url.OriginalString);
            responseMock.SetupGet(r => r.Data).Returns(dataJson);

            httpTransportMock.Setup(t => t.Send(It.IsAny<HttpWebRequest>(), It.IsNotIn<string>("{\"status\":\"created\"}"))).Returns(responseMock.Object);
        }

        private void PreparePostProcessMock(Mock<IHttpTransport> httpTransportMock)
        {
            var url = new Uri("http://klarna.com");

            var responseMock = new Mock<IHttpResponse>();

            dynamic createdData =
                new
                {
                    gui = new { snippet = "iframe" },
                    id = "SomeOuterId",
                    status = "created"
                };
            string createdDataJson = JsonConvert.SerializeObject(createdData);

            responseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);
            responseMock.Setup(r => r.Header("Location")).Returns(url.OriginalString);
            responseMock.SetupGet(r => r.Data).Returns(createdDataJson);

            httpTransportMock.Setup(t => t.Send(It.IsAny<HttpWebRequest>(), It.Is<string>(p => p.Equals("{\"status\":\"created\"}")))).Returns(responseMock.Object);
        }
    }
}
