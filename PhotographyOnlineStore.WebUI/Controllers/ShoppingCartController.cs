﻿using PhotographyOnlineStore.Core.Contracts;
using PhotographyOnlineStore.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PayPal.Api;
using MvcApplication1.Models;
using PhotographyOnlineStore.Models;
using Order = PhotographyOnlineStore.Core.Models.Order;
using Microsoft.AspNet.Identity;

namespace PhotographyOnlineStore.WebUI.Controllers
{
    public class ShoppingCartController : Controller
    {
        IShoppingCartService shoppingCartService;
        IOrderService orderService;
        IRepository<Customer> allCustomersContext;

        public ShoppingCartController(IShoppingCartService ShoppingCartService, IOrderService OrderService, IRepository<Customer> customerContext)
        {
            this.shoppingCartService = ShoppingCartService;
            this.orderService = OrderService;
            this.allCustomersContext = customerContext;
        }
        // GET: ShoppingCart2
        public ActionResult Index()
        {
            var model = shoppingCartService.GetShoppingCartItems(this.HttpContext);
            return View(model);
        }

        public ActionResult AddToShoppingCart(string Id)
        {
           
            shoppingCartService.AddToShoppingCart(this.HttpContext, Id);

            return RedirectToAction("Index");
           
        }
        public ActionResult AddQuantity(string Id)
        {
            shoppingCartService.AddOneItem(this.HttpContext, Id);

            return RedirectToAction("Index");
        }

        public ActionResult ReduceQuantity(string Id)
        {
            shoppingCartService.ReduceOneItem(this.HttpContext, Id);

            return RedirectToAction("Index");
        }
        public ActionResult RemoveFromShoppingCart(string Id)
        {
            shoppingCartService.RemoveFromShoppingCart(this.HttpContext, Id);

            return RedirectToAction("Index");
        }

        public PartialViewResult ShoppingCartSummary()
        {

            var shoppingCartSummary = shoppingCartService.GetShoppingCartSummary(this.HttpContext);

            return PartialView(shoppingCartSummary);
        }

        public PartialViewResult _ShoppingCartListPartialView()
        {
            var shoppingCartItemList = shoppingCartService.GetShoppingCartItems(this.HttpContext);

            return PartialView(shoppingCartItemList);
        }
        [Authorize]
        public ActionResult Checkout()
        {
       
            List<Customer> customers = allCustomersContext.Collection().ToList();
            var userName = User.Identity.GetUserName();
            var currentUser = customers.FirstOrDefault(c => c.Email == userName);
            
            //           System.Diagnostics.Debug.WriteLine("userName: " + userName);

            if (userName.Length > 0)
            {
                ViewBag.Message = currentUser.FirstName + " " + currentUser.LastName;
                Order order = new Order();
                order.FirstName = currentUser.FirstName == null ? null : currentUser.FirstName;
                order.Surname = currentUser.LastName == null ? null : currentUser.LastName;
                order.Email = currentUser.Email == null ? null : currentUser.Email;
                order.Street = currentUser.Street == null ? null : currentUser.Street;
                order.City = currentUser.City == null ? null : currentUser.City;
                order.State = currentUser.State == null ? null : currentUser.State;
                order.ZipCode = currentUser.ZipCode == null ? null : currentUser.ZipCode;
                return View(order);
            }
            else
            {
                ViewBag.Message = "Customer";
                return View();
            }
        }
        [HttpPost]
        [Authorize]
        public ActionResult Checkout(Order order)
        {
            var basketItems = shoppingCartService.GetShoppingCartItems(this.HttpContext);
            order.OrderStatus = "Order Created";
            order.Email = User.Identity.Name;
            //TODO: process payment...

            order.OrderStatus = "Payment Processed";
            orderService.CreateOrder(order, basketItems);
            //clear the basket
            shoppingCartService.ClearShoppingCart(this.HttpContext);

            //And finally redirect the user to the thank you page with order id.
            return RedirectToAction("Thankyou", new { OrderId = order.Id });
        }

        public void CloseOrder(Order order)
        {
            var basketItems = shoppingCartService.GetShoppingCartItems(this.HttpContext);
            order.OrderStatus = "Order Created";

            order.OrderStatus = "Payment Processed";
            orderService.CreateOrder(order, basketItems);
            //clear the basket
            shoppingCartService.ClearShoppingCart(this.HttpContext);
        }


        //  Handle Payment below:


        public ActionResult PaymentWithCreditCard(Order order)
        {

            CloseOrder(order);

            //create and item for which you are taking payment
            //if you need to add more items in the list
            //Then you will need to create multiple item objects or use some loop to instantiate object
            Item item = new Item();
            item.name = "Demo Item";
            //item.currency = "USD";
            item.price = "5";
            item.quantity = "1";
            item.sku = "sku";

            //Now make a List of Item and add the above item to it
            //you can create as many items as you want and add to this list
            List<Item> itms = new List<Item>();
            itms.Add(item);
            ItemList itemList = new ItemList();
            itemList.items = itms;

            //Address for the payment
            Address billingAddress = new Address();
            billingAddress.city = "NewYork";
            billingAddress.country_code = "US";
            billingAddress.line1 = "23rd street kew gardens";
            billingAddress.postal_code = "43210";
            billingAddress.state = "NY";


            //Now Create an object of credit card and add above details to it
            CreditCard crdtCard = new CreditCard();
            crdtCard.billing_address = billingAddress;
            crdtCard.cvv2 = "874";
            crdtCard.expire_month = 1;
            crdtCard.expire_year = 2020;
            crdtCard.first_name = "Aman";
            crdtCard.last_name = "Thakur";
            crdtCard.number = "1234567890123456";
            crdtCard.type = "discover";

            // Specify details of your payment amount.
            Details details = new Details();
            details.shipping = "1";
            details.subtotal = "5";
            details.tax = "1";

            // Specify your total payment amount and assign the details object
            Amount amnt = new Amount();
            amnt.currency = "USD";
            // Total = shipping tax + subtotal.
            amnt.total = "7";
            amnt.details = details;

            // Now make a trasaction object and assign the Amount object
            Transaction tran = new Transaction();
            tran.amount = amnt;
            tran.description = "Description about the payment amount.";
            tran.item_list = itemList;
            tran.invoice_number = "your invoice number which you are generating";

            // Now, we have to make a list of trasaction and add the trasactions object
            // to this list. You can create one or more object as per your requirements

            List<Transaction> transactions = new List<Transaction>();
            transactions.Add(tran);

            // Now we need to specify the FundingInstrument of the Payer
            // for credit card payments, set the CreditCard which we made above

            FundingInstrument fundInstrument = new FundingInstrument();
            fundInstrument.credit_card = crdtCard;

            // The Payment creation API requires a list of FundingIntrument

            List<FundingInstrument> fundingInstrumentList = new List<FundingInstrument>();
            fundingInstrumentList.Add(fundInstrument);

            // Now create Payer object and assign the fundinginstrument list to the object
            Payer payr = new Payer();
            payr.funding_instruments = fundingInstrumentList;
            payr.payment_method = "credit_card";

            // finally create the payment object and assign the payer object & transaction list to it
            Payment pymnt = new Payment();
            pymnt.intent = "sale";
            pymnt.payer = payr;
            pymnt.transactions = transactions;

            try
            {
                //getting context from the paypal, basically we are sending the clientID and clientSecret key in this function 
                //to the get the context from the paypal API to make the payment for which we have created the object above.

                //Code for the configuration class is provided next

                // Basically, apiContext has a accesstoken which is sent by the paypal to authenticate the payment to facilitator account. An access token could be an alphanumeric string

                APIContext apiContext = PaypalConfiguration.GetAPIContext();

                // Create is a Payment class function which actually sends the payment details to the paypal API for the payment. The function is passed with the ApiContext which we received above.

                Payment createdPayment = pymnt.Create(apiContext);

                //if the createdPayment.State is "approved" it means the payment was successfull else not

                if (createdPayment.state.ToLower() != "approved")
                {
                    return View("FailureView");
                }
            }
            catch (PayPal.PayPalException ex)
            {
                PaypalLogger.Log("Error: " + ex.Message);
                return View("FailureView");
            }

            return View("SuccessView");
        }


        public ActionResult PaymentWithPaypal(Order order)
        {

            CloseOrder(order);

            //getting the apiContext as earlier
            APIContext apiContext = PaypalConfiguration.GetAPIContext();

            try
            {
                string payerId = Request.Params["PayerID"];

                if (string.IsNullOrEmpty(payerId))
                {
                    //this section will be executed first because PayerID doesn't exist

                    //it is returned by the create function call of the payment class

                    // Creating a payment

                    // baseURL is the url on which paypal sendsback the data.

                    // So we have provided URL of this controller only

                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/Paypal/PaymentWithPayPal?";

                    //guid we are generating for storing the paymentID received in session

                    //after calling the create function and it is used in the payment execution

                    var guid = Convert.ToString((new Random()).Next(100000));

                    //CreatePayment function gives us the payment approval url

                    //on which payer is redirected for paypal acccount payment

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);

                    //get links returned from paypal in response to Create function call

                    var links = createdPayment.links.GetEnumerator();

                    string paypalRedirectUrl = null;

                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;

                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment
                            paypalRedirectUrl = lnk.href;
                        }
                    }

                    // saving the paymentID in the key guid
                    Session.Add(guid, createdPayment.id);

                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This section is executed when we have received all the payments parameters

                    // from the previous call to the function Create

                    // Executing a payment

                    var guid = Request.Params["guid"];

                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);

                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }

                }
            }
            catch (Exception ex)
            {
                PaypalLogger.Log("Error: " + ex.Message);
                return View("FailureView");
            }

            return View("SuccessView");
        }

        private PayPal.Api.Payment payment;

        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            this.payment = new Payment() { id = paymentId };
            return this.payment.Execute(apiContext, paymentExecution);
        }

        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {

            //similar to credit card create itemlist and add item objects to it
            var itemList = new ItemList() { items = new List<Item>() };

            itemList.items.Add(new Item()
            {
                name = "Item Name",
                currency = "USD",
                price = "5",
                quantity = "1",
                sku = "sku"
            });

            var payer = new Payer() { payment_method = "paypal" };

            // Configure Redirect Urls here with RedirectUrls object
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl,
                return_url = redirectUrl
            };

            // similar as we did for credit card, do here and create details object
            var details = new Details()
            {
                tax = "1",
                shipping = "1",
                subtotal = "5"
            };

            // similar as we did for credit card, do here and create amount object
            var amount = new Amount()
            {
                currency = "USD",
                total = "7", // Total must be equal to sum of shipping, tax and subtotal.
                details = details
            };

            var transactionList = new List<Transaction>();

            transactionList.Add(new Transaction()
            {
                description = "Transaction description.",
                invoice_number = "your invoice number",
                amount = amount,
                item_list = itemList
            });

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };

            // Create a payment using a APIContext
            return this.payment.Create(apiContext);

        }


        public ActionResult PaymentWithBank(Order order)
        {
            CloseOrder(order);

            return View("SuccessView");

        }


            public ActionResult SuccessView()
        {
            return View("SuccessView");
        }


        //create view (partial view) for the thank you page. Template: Empty
        public ActionResult ThankYou(string OrderId)
        {
            ViewBag.OrderId = OrderId;
            return View();
        }

    }
}

