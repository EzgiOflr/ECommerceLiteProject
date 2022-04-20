using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Repository;
using ECommerceLiteUI.Models;
using Mapster;

namespace ECommerceLiteUI.Controllers
{
    public class HomeController : Controller
    {
        CategoryRepo myCategoryRepo = new CategoryRepo();
        ProductRepo myProductRepo = new ProductRepo();

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
    }
}