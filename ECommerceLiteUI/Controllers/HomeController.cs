using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Repository;
using ECommerceLiteUI.Models;
using Mapster;
using ECommerceLiteBLL.Account;
using ECommerceLiteEntity.Models;
using QRCoder;
using System.Drawing;
using ECommerceLiteBLL.Settings;
using ECommerceLiteEntity.ViewModels;

namespace ECommerceLiteUI.Controllers
{
    public class HomeController : BaseController
    {
        CategoryRepo myCategoryRepo = new CategoryRepo();
        ProductRepo myProductRepo = new ProductRepo();
        AdminRepo myAdminRepo = new AdminRepo();
        CustomerRepo myCustomerRepo = new CustomerRepo();
        OrderRepo myOrderRepo = new OrderRepo();
        OrderDetailRepo myOrderDetailRepo = new OrderDetailRepo();

        public ActionResult Index()
        {
            // Ana kategorilerden 4 tanesini viewbag ile sayfaya gönderelim
            var categoryList = myCategoryRepo.AsQueryable()
                .Where(x => x.BaseCategoryId == null).Take(4).ToList();

            ViewBag.CategoryList = categoryList.OrderByDescending(x => x.Id).ToList();

            //ürünler
            var productList = myProductRepo.AsQueryable()
                .Where(x => !x.IsDeleted && x.Quantity >= 1).Take(10).ToList();
            List<ProductViewModel> model = new List<ProductViewModel>();
            //MAPSTER İLE MAPLEDİK! aşağıdkai foreachin mapster ile daha kısa hali

            //foreach linq sorgusu
            productList.ForEach(x =>
            {
                var item = x.Adapt<ProductViewModel>();
                item.GetCategory();
                item.GetProductPictures();
                model.Add(item);
            });

            //üstteki ile aynı.

            //foreach (var item in productList)
            //{
            //    //mapster :S 
            //    //model.Add(item.Adapt<ProductViewModel>());
            //    var product = new ProductViewModel()
            //    {
            //        Id = item.Id,
            //        CategoryId = item.CategoryId,
            //        ProductName = item.ProductName,
            //        Description = item.Description,
            //        Quantity = item.Quantity,
            //        Discount = item.Discount,
            //        RegisterDate = item.RegisterDate,
            //        Price = item.Price,
            //        ProductCode = item.ProductCode
            //        //isDeleted alanını viewmodelin içine eklemeyi unuttuk. Çünkü
            //        // isDeleted alanını daha dün ekledik. Viewmodeli geçen hafta oluşturduk
            //    };
            //    product.GetCategory();
            //    product.GetProductPictures();
            //    model.Add(product);
            //}

            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult AddToCart(int id)
        {
            try
            {
                //sessiona'a eklenecek 
                //session oturum demektir. asp.net mvc session nedir?

                var shoppingCart = Session["ShoppingCart"] as List<ProductViewModel>;
                if (shoppingCart == null)
                {
                    shoppingCart = new List<ProductViewModel>();
                }
                //buraya geri dönecegiz.
                if (id > 0)
                {
                    var product = myProductRepo.GetById(id);
                    if (product == null)
                    {
                        TempData["AddToCartFailed"] = "Ürün eklemesi başarısızdır.Lütfen tekrar deneyiniz";
                        //product null mı geldi Logla
                        return RedirectToAction("Index", "Home");

                    }
                    //tamam ekleme yapılacak//var productAddToCart = product.Adapt<ProductViewModel>();
                    var productAddToCart = new ProductViewModel()
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Description = product.Description,
                        CategoryId = product.CategoryId,
                        Discount = product.Discount,
                        Price = product.Price,
                        Quantity = product.Quantity,
                        RegisterDate = product.RegisterDate,
                        ProductCode = product.ProductCode,
                    };
                    if (shoppingCart.Count(x => x.Id == product.Id) > 0)
                    {
                        shoppingCart.FirstOrDefault(x => x.Id == product.Id).Quantity++;

                    }
                    else
                    {
                        productAddToCart.Quantity = 1;
                        shoppingCart.Add(productAddToCart);
                    }
                    //ÖNEMLİ: SESSİONDA BU LİSTEYİ
                    Session["ShoppingCart"] = shoppingCart;
                    TempData["AddToCartSuccess"] = "Ürün sepete eklendi";
                    return RedirectToAction("index", "Home");
                }
                else
                {
                    TempData["AddToCartFailed"] = "Ürün eklemesi başarısızdır. Lütfen tekrar deneyiniz";
                    //loglama yap id düzgün gelmedi

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception)
            {

                //ex loglanacak
                TempData["AddToCartFailed"] = "Ürün eklemesi başarısızdır. Lütfen tekrar deneyiniz";
                return RedirectToAction("Index", "Home");
            }

        }

        [Authorize]
        public async Task<ActionResult> Buy()
        {
            try
            {
                //1) Eğer müşteri değilsen isen alışveriş yapamazsin!
                var user = MembershipTools.GetUser();
                var customer = myCustomerRepo.AsQueryable().FirstOrDefault(x => x.UserId == user.Id); //select cekiyor sql de select from  admins
                //where UserId= 'buraya userin id si gelir'.
                if (customer == null)
                {
                    TempData["BuyFailed"] = "Alışveriş yapabilmeniz için Müşteri bilgileriniz ile giriş yapmanız gereklidir!";
                    return RedirectToAction("Index", "Home");
                }
                //shoppingcart null mı değil mi?

                var shoppingcart = Session["ShoppingCart"] as List<ProductViewModel>;
                if (shoppingcart == null)
                {
                    TempData["BuyFailed"] = "Alışveriş yapabilmeniz için Sepetinize ürün eklemeniz gereklidir!";
                    return RedirectToAction("Index", "Home");
                }
                ////shoppingcart içerisin de ürün var mı?
                //if (shoppingcart.Count == 0)
                //{
                //    TempData["BuyFailed"] = "Alışveriş sepetinizde ürün bulunmamaktadır";
                //    return RedirectToAction("Index", "Home");
                //}
                //artık alışveriş tamamlansın.
                Order customerOrder = new Order()
                {
                    CustomerTCNumber = customer.TCNumber,
                    IsDeleted = false,
                    OrderNumber = customer.TCNumber   //burayı düzeltecegim.

                };
                //ınsert yapılsın
                int orderInsertResult = myOrderRepo.Insert(customerOrder);
                if (orderInsertResult > 0)
                {
                    //siparişin detayları orderdatile eklenmeli
                    int orderDetailsInsertResult = 0;
                    foreach (var item in shoppingcart)
                    {
                        OrderDetail customerOrderDetail = new OrderDetail()
                        {
                            OrderId = customerOrder.Id,
                            IsDeleted = false,
                            ProductId = item.Id,
                            ProductPrice = item.Price,
                            Quantity = item.Quantity,
                            Discount = item.Discount

                        };

                        //Total Count Hesabi:
                        if (item.Discount > 0)
                        {
                            customerOrderDetail.TotalPrice =
                                customerOrderDetail.Quantity *
                                (customerOrderDetail.ProductPrice -
                                (customerOrderDetail.ProductPrice *
                                ((decimal)customerOrderDetail.Discount / 100)
                                ));
                        }
                        else
                        {
                            //3 adet telefon
                            customerOrderDetail.TotalPrice =
                                customerOrderDetail.Quantity * customerOrderDetail.ProductPrice;
                        }

                        //orderdetail tabloya insert edilsin
                        orderDetailsInsertResult += myOrderDetailRepo.Insert(customerOrderDetail);

                    }
                    //OrderDetailsInsertResult büyükse sıfırdan 
                    if (orderDetailsInsertResult > 0 && orderDetailsInsertResult == shoppingcart.Count)
                    {

                        //  QR kod eklenmiş email gönderilecek.
                        #region SendOrderEmailWithQR


                        string siteUrl =
                        Request.Url.Scheme + Uri.SchemeDelimiter
                      + Request.Url.Host
                      + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port);
                        siteUrl += "/Home/Order/" + customerOrder.Id;

                        QRCodeGenerator QRGenerator = new QRCodeGenerator();
                        QRCodeData QRData = QRGenerator.CreateQrCode(siteUrl, QRCodeGenerator.ECCLevel.Q);
                        QRCode QRCode = new QRCode(QRData);
                        Bitmap QRBitmap = QRCode.GetGraphic(60);
                        byte[] bitmapArray = BitmapToByteArray(QRBitmap);

                        List<OrderDetail> orderDetailList =
           new List<OrderDetail>();
                        orderDetailList = myOrderDetailRepo.AsQueryable()
                            .Where(x => x.OrderId == customerOrder.Id).ToList();

                        string message = $"Merhaba {user.Name} {user.Surname} <br/><br/>" +
           $"{orderDetailList.Sum(x=> x.Quantity)} adet ürünlerinizin siparişini aldık.<br/><br/>" +
           $"Toplam Tutar:{orderDetailList.Sum(x => x.TotalPrice).ToString()} ₺ <br/> <br/>" +
           $"<table><tr><th>Ürün Adı</th><th>Adet</th><th>Birim Fiyat</th><th>Toplam</th></tr>";
                        foreach (var item in orderDetailList)
                        {
                            message += $"<tr><td>{myProductRepo.GetById(item.ProductId).ProductName}</td><td>{item.Quantity}</td><td>{item.TotalPrice}</td></tr>";
                        }


                        message += "</table><br/>Siparişinize ait QR kodunuz aşağıdadır. <br/><br/>";

                        SiteSettings.SendMail(bitmapArray, new MailModel()
                        {
                            To = user.Email,
                            Subject = "ECommerceLite - Siparişiniz alındı.",
                            Message = message
                        });
                        #endregion

                        TempData["BuySuccess"] = "Siparişiniz oluşturuldu. Sipariş numarası: " +customerOrder.OrderNumber;
                        
                        //temizlik
                        Session["shoppingcart"] = null;
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        //sistem yöneticisine orderId detayı verilerek
                        //email gönderilsin. eklenmeyen ürünleri acilen eklesinler.
                        var message = $"merhaba Admin, <br/>" +
                            $"Aşağıdaki bilgileri verilen siparişin kendisi oluşturulmasına rağmen detaylarından bazıları oluşturulamadı." +
                            $"Acilen müdahale edelim.<br/><br/>" +
                            $"OrderId:{customerOrder.Id} <br/>";
                        for (int i = 1; i <= shoppingcart.Count; i++)
                        {
                            message += $"{i}- Id: {shoppingcart[i].Id}"
                                + $"Birim Fiyat: {shoppingcart[i].Price}-"
                                + $"Sipariş adedi: {shoppingcart[i].Quantity}-"
                                + $"İndirimi: {shoppingcart[i].Discount}-" +
                                $"<br/><br/>";
                        }

                        await SiteSettings.SendMail(new MailModel()
                        {
                            To = "nayazilim303@gmail.com",
                            Subject = "ECommerceLite 303 SİPARİŞ DETAY SORUNU  ",
                            Message = message

                        });
                    }
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                //ex loglanacak
                TempData["BuyFailed"] = "Beklenmedik bir hata nedeniyle siparişiniz oluşturulamadı";
                return RedirectToAction("Index", "Home");
            }



        }
    }
}