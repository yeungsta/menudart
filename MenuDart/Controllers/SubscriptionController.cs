﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using MenuDart.Models;
using MenuDart.Composer;
using Stripe;

namespace MenuDart.Controllers
{
    public class SubscriptionController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Menu/Activate/5

        [Authorize]
        public ActionResult Activate(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //just activate the menu directly if user is on free trial
            if (Utilities.IsUserOnTrial(db, User))
            {
                if (!Utilities.ActivateMenu(id, menu, User.Identity.Name, 1, db, true))
                {
                    return RedirectToAction("Failed");
                }

                //save all changes to DB
                db.SaveChanges();

                //for view display. Use TempData since we're
                //using RedirectToAction() as opposed to View()
                TempData["Id"] = id;
                TempData["Name"] = menu.Name;
                TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);

                return RedirectToAction("ActivateCompleted");
            }

            //Else, continue w/ payment data collection...
            //find out how many active menus this owner already has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(menu.Owner, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            //display to user what the new billing will be
            ViewBag.NumActiveMenus = activeCount + 1;
            ViewBag.NewTotal = (activeCount + 1) * 7;
            ViewBag.Email = menu.Owner;
            ViewBag.StripeKey = Constants.StripePublishableKey;

            return View(menu);
        }

        //
        // POST: /Menu/Activate/5

        [Authorize]
        [HttpPost, ActionName("Activate")]
        public ActionResult ActivateConfirmed(int ActiveCount, string Email, string stripeToken, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            return RedirectToAction("Subscribe", "Subscription", new { id = id, subscribeAction = Constants.ActivateOne, email = Email, quantity = ActiveCount, token = stripeToken }); 
        }

        //
        // GET: /Menu/ActivateAll/5

        [Authorize]
        public ActionResult ActivateAll(string email, int quantity)
        {
            if (User.Identity.Name != email)
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            //display to user what the billing will be
            ViewBag.NumActiveMenus = quantity;
            ViewBag.NewTotal = quantity * 7;
            ViewBag.Email = email;
            ViewBag.StripeKey = Constants.StripePublishableKey;

            return View();
        }

        //
        // POST: /Menu/ActivateAll/5

        [Authorize]
        [HttpPost, ActionName("ActivateAll")]
        public ActionResult ActivateAllConfirmed(string Email, int Quantity, string stripeToken)
        {
            if (User.Identity.Name != Email)
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            return RedirectToAction("Subscribe", "Subscription", new { id = 0, subscribeAction = Constants.ActivateAll, email = Email, quantity = Quantity, token = stripeToken });
        }

        //
        // GET: /Menu/Deactivate/5

        [Authorize]
        public ActionResult Deactivate(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //just deactivate the menu directly if user is on free trial
            if (Utilities.IsUserOnTrial(db, User))
            {
                ViewBag.TrialWarning = true;

                return View(menu);
/*
                if (!DeactivateMenu(id, menu, User.Identity.Name, 1))
                {
                    return RedirectToAction("Failed");
                }

                //save all changes to DB
                db.SaveChanges();

                //for view display. Use TempData since we're
                //using RedirectToAction() as opposed to View()
                TempData["Id"] = id;
                TempData["Name"] = menu.Name;
                TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);

                return RedirectToAction("DeactivateCompleted");
 */ 
            }

            //find out how many remaining active menus this owner has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(menu.Owner, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            ViewBag.NumActiveMenus = activeCount - 1;
            ViewBag.NewTotal = (activeCount - 1) * 7;
            ViewBag.Email = menu.Owner;

            return View(menu);
        }

        //
        // POST: /Menu/Deactivate/5

        [Authorize]
        [HttpPost, ActionName("Deactivate")]
        public ActionResult DeactivateConfirmed(int ActiveCount, string Email, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            //just deactivate the menu directly if user is on free trial
            if (Utilities.IsUserOnTrial(db, User))
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                if (!DeactivateMenu(id, menu, User.Identity.Name, 1))
                {
                    return RedirectToAction("Failed");
                }

                //save all changes to DB
                db.SaveChanges();

                //for view display. Use TempData since we're
                //using RedirectToAction() as opposed to View()
                TempData["Id"] = id;
                TempData["Name"] = menu.Name;
                TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);

                return RedirectToAction("DeactivateCompleted");
            }

            return RedirectToAction("Subscribe", "Subscription", new { id = id, subscribeAction = Constants.DeactivateOne, email = Email, quantity = ActiveCount }); 
        }

        //
        // GET: /Subscription/DeactivateAll
        //deactivate all menus and cancel billing agreement

        [Authorize]
        public ActionResult DeactivateAll(string email)
        {
            if (User.Identity.Name != email)
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            //find out how many active menus this owner has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            ViewBag.NumActiveMenus = activeCount;
            ViewBag.Email = email;

            return View();
        }

        //
        // POST: /Menu/DeactivateAll/5

        [Authorize]
        [HttpPost, ActionName("DeactivateAll")]
        public ActionResult DeactivateAllConfirmed(string Email)
        {
            if (User.Identity.Name != Email)
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }

            return RedirectToAction("Subscribe", "Subscription", new { id = 0, subscribeAction = Constants.DeactivateAll, email = Email, quantity = 0 });
        }

        //
        // GET: /Subscription/Subscribe

        [Authorize]
        public ActionResult Subscribe(int id, string subscribeAction, string email, int quantity, string token)
        {
/*    TODO: ID will be 0 on activate alls?  
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation", "Menu");
            }
*/
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                Utilities.LogAppError("Can't retrieve user info.");
                return RedirectToAction("Failed");
            }

            //check if Stripe customer account exists
            if (string.IsNullOrEmpty(userInfo.PaymentCustomerId) || (!DoesStripeCustomerExist(userInfo.PaymentCustomerId)))
            {
                //create account
                if (!CreateNewStripeCustomer(email, token, quantity, userInfo))
                {
                    return RedirectToAction("Failed");
                }
            }
            else //Stripe customer account exists
            {
                //if there are no more active menus or we're de-activating all menus, then unsubscribe.
                if ((quantity == 0) || (subscribeAction == Constants.DeactivateAll))
                {
                    if (!CancelStripeSubscription(userInfo))
                    {
                        return RedirectToAction("Failed");
                    }
                }
                else
                {
                    //update subscription quantity
                    if (!UpdateStripeSubscription(email, token, quantity, userInfo))
                    {
                        return RedirectToAction("Failed");
                    }
                }
            }

            //Now update menu(s) in database according to action
            switch (subscribeAction)
            {
                case Constants.ActivateOne:
                    Menu menu = db.Menus.Find(id);

                    if (menu == null)
                    {
                        Utilities.LogAppError("Could not retrieve menu.");
                        return RedirectToAction("Failed");
                    }

                    if (!Utilities.ActivateMenu(id, menu, email, quantity, db, false))
                    {
                        return RedirectToAction("Failed");
                    }

                    //if user is on free trial, mark it as finished.
                    if (Utilities.IsUserOnTrial(db, User))
                    {
                        userInfo.TrialEnded = true;
                    }

                    //save all changes to DB
                    db.SaveChanges();

                    //for view display. Use TempData since we're
                    //using RedirectToAction() as opposed to View()
                    TempData["Id"] = id;
                    TempData["Name"] = menu.Name;
                    TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);

                    return RedirectToAction("ActivateCompleted");
                case Constants.ActivateAll:
                    if (!ActivateAllMenus(email, quantity))
                    {
                        return RedirectToAction("Failed");
                    }

                    //if user is on free trial, mark it as finished.
                    if (Utilities.IsUserOnTrial(db, User))
                    {
                        userInfo.TrialEnded = true;
                    }

                    //save all changes to DB
                    db.SaveChanges();

                    return RedirectToAction("ActivateAllCompleted");
                case Constants.DeactivateOne:
                    menu = db.Menus.Find(id);

                    if (menu == null)
                    {
                        Utilities.LogAppError("Could not retrieve menu.");
                        return RedirectToAction("Failed");
                    }

                    if (!DeactivateMenu(id, menu, email, quantity))
                    {
                        return RedirectToAction("Failed");
                    }

                    //save all changes to DB
                    db.SaveChanges();

                    //for view display. Use TempData since we're
                    //using RedirectToAction() as opposed to View()
                    TempData["Id"] = id;
                    TempData["Name"] = menu.Name;
                    TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);

                    return RedirectToAction("DeactivateCompleted");
                case Constants.DeactivateAll:
                    if (!DeactivateAllMenus(email))
                    {
                        return RedirectToAction("Failed");
                    }

                    //save all changes to DB
                    db.SaveChanges();

                    return RedirectToAction("DeactivateAllCompleted");
            }

            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Failed

        [Authorize]
        public ActionResult Failed()
        {
            return View();
        }

        //
        // GET: /Subscription/ActivateCompleted

        [Authorize]
        public ActionResult ActivateCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/ActivateAllCompleted

        [Authorize]
        public ActionResult ActivateAllCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/DeactivateCompleted

        [Authorize]
        public ActionResult DeactivateCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/DeactivateAllCompleted

        [Authorize]
        public ActionResult DeactivateAllCompleted()
        {
            return View();
        }
/*
        private bool ActivateMenu(int id, Menu menu, string email, int quantity)
        {
            //set menu as active
            menu.Active = true;

            V1 composer = new V1(menu);
            // re-compose the menu
            string fullURL = composer.CreateMenu();

            db.Entry(menu).State = EntityState.Modified;

            //for confirmation email
            IList<MenuAndLink> justActivatedMenusAndLinks = new List<MenuAndLink>();
            IList<MenuAndLink> totalActivatedMenusAndLinks = new List<MenuAndLink>();

            MenuAndLink item = new MenuAndLink();
            item.MenuName = menu.Name;
            item.MenuLink = fullURL;
            justActivatedMenusAndLinks.Add(item);

            //get total list of activated menus
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return false; }

            foreach (Menu singleMenu in allMenus)
            {
                item = new MenuAndLink();
                item.MenuName = singleMenu.Name;
                item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);

                if (singleMenu.Active)
                {
                    totalActivatedMenusAndLinks.Add(item);
                }
            }

            //send confirmation email to user
            try //TODO: remove for Production SMTP
            {
                new MailController().SendActivateEmail(email, quantity * Constants.CostPerMenu, justActivatedMenusAndLinks, totalActivatedMenusAndLinks).Deliver();
            }
            catch
            {
            }

            return true;
        }
*/
        private bool DeactivateMenu(int id, Menu menu, string email, int quantity)
        {
            //set menu as deactivated
            menu.Active = false;

            //deactivate the menu directory (delete menu but not logo file)
            Utilities.DeactivateDirectory(menu.MenuDartUrl);

            db.Entry(menu).State = EntityState.Modified;

            //get status of all menus this owner has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return false; }

            //list of menu names and links for confirmation email
            IList<MenuAndLink> remainingActiveMenusAndLinks = new List<MenuAndLink>();
            IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

            foreach (Menu singleMenu in allMenus)
            {
                MenuAndLink item = new MenuAndLink();
                item.MenuName = singleMenu.Name;
                item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);

                if (singleMenu.Active)
                {
                    remainingActiveMenusAndLinks.Add(item);
                }
                else
                {
                    deactivatedMenusAndLinks.Add(item);
                }
            }

            //send confirmation email to user
            try //TODO: remove for Production SMTP
            {
                new MailController().SendDeactivateEmail(email, quantity * Constants.CostPerMenu, remainingActiveMenusAndLinks, deactivatedMenusAndLinks).Deliver();
            }
            catch
            {
            }

            return true;
        }

        private bool ActivateAllMenus(string email, int quantity)
        {
            //set all menu(s) this owner has as active
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return false; }

            //list of menu names and links for confirmation email
            IList<MenuAndLink> justActivatedMenusAndLinks = new List<MenuAndLink>();
            //not used by view when all menus are activated, so just keep empty
            IList<MenuAndLink> totalActivatedMenusAndLinks = new List<MenuAndLink>();

            foreach (Menu singleMenu in allMenus)
            {
                //set menu as active
                singleMenu.Active = true;

                V1 composer = new V1(singleMenu);
                // re-compose the menu
                string fullURL = composer.CreateMenu();

                db.Entry(singleMenu).State = EntityState.Modified;

                //info for confirmation email
                MenuAndLink item = new MenuAndLink();
                item.MenuName = singleMenu.Name;
                item.MenuLink = fullURL;
                justActivatedMenusAndLinks.Add(item);
            }

            //send confirmation email to user
            try //TODO: remove for Production SMTP
            {
                new MailController().SendActivateEmail(email, quantity * Constants.CostPerMenu, justActivatedMenusAndLinks, totalActivatedMenusAndLinks).Deliver();
            }
            catch
            {
            }

            return true;
        }

        private bool DeactivateAllMenus(string email)
        {
            //set all menu(s) this owner has as inactive
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return false; }

            //list of menu names and links for confirmation email
            //remaining active menus will be empty since we're deactivating all
            IList<MenuAndLink> remainingActiveMenusAndLinks = new List<MenuAndLink>();
            IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

            foreach (Menu singleMenu in allMenus)
            {
                //set menu as deactivated
                singleMenu.Active = false;

                //deactivate the menu directory (delete menu but not logo file)
                Utilities.DeactivateDirectory(singleMenu.MenuDartUrl);

                db.Entry(singleMenu).State = EntityState.Modified;

                //info for confirmation email
                MenuAndLink item = new MenuAndLink();
                item.MenuName = singleMenu.Name;
                item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);
                deactivatedMenusAndLinks.Add(item);
            }

            //send confirmation email to user
            try //TODO: remove for Production SMTP
            {
                new MailController().SendDeactivateEmail(email, 0, remainingActiveMenusAndLinks, deactivatedMenusAndLinks).Deliver();
            }
            catch
            {
            }

            return true;
        }
        
        private bool CancelStripeSubscription(UserInfo userInfo)
        {
            var customerService = new StripeCustomerService();
            DateTime? periodEnd;
            DateTime? periodStart;

            try
            {
                //cancels subscription at the end of the current period
                StripeSubscription stripeSubscription = customerService.CancelSubscription(userInfo.PaymentCustomerId);

                //save profile ID and profile status with user info
                userInfo.Subscribed = false;
                userInfo.PaymentCustomerStatus = stripeSubscription.Status;
                periodEnd = stripeSubscription.PeriodEnd;
                periodStart = stripeSubscription.PeriodStart;

                db.Entry(userInfo).State = EntityState.Modified;
            }
            catch (Exception e)
            {
                Utilities.LogAppError("Subscription cancel failed: " + e.Message);
                return false;
            }

            try
            {
                //get the most recent charge
                var chargeService = new StripeChargeService();
                IEnumerable<StripeCharge> response = chargeService.List(1, 0, userInfo.PaymentCustomerId);

                if ((response != null) && (response.Count() > 0))
                {
                    StripeCharge charge = response.ElementAt(0);

                    //refund the charge (if paid)
                    if ((charge != null) && charge.Paid.HasValue && charge.Paid.Value)
                    {
                        int daysIntoPeriod = 0;

                        if (DateTime.Today.Date > periodStart.Value)
                        {
                            daysIntoPeriod = DateTime.Today.Date.Subtract(periodStart.Value).Days;
                        }

                        int numDaysInPeriod = periodEnd.Value.Date.Subtract(periodStart.Value.Date).Days;
                        float ratePerDayInCents = charge.AmountInCents.Value / numDaysInPeriod;

                        float amountOwed = daysIntoPeriod * ratePerDayInCents;

                        int? refundAmount = 0;

                        //round amount to nearest int
                        refundAmount = charge.AmountInCents.Value - Convert.ToInt32(amountOwed);

                        StripeCharge stripeCharge = chargeService.Refund(charge.Id, refundAmount);
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.LogAppError("Refund failed: " + e.Message);
                return false;
            }

            return true;
        }

        private bool UpdateStripeSubscription(string email, string token, int quantity, UserInfo userInfo)
        {
            var myUpdatedSubscription = new StripeCustomerUpdateSubscriptionOptions();

            //if token has been created, then replace the previous one (new credit card)
            if (!string.IsNullOrEmpty(token))
            {
                myUpdatedSubscription.TokenId = token;
            }

            myUpdatedSubscription.PlanId = Constants.StripeSubscriptionPlan;
            myUpdatedSubscription.Quantity = quantity;

            var customerService = new StripeCustomerService();

            try
            {
                StripeSubscription stripeSubscription = customerService.UpdateSubscription(userInfo.PaymentCustomerId, myUpdatedSubscription);

                //save profile ID and profile status with user info
                userInfo.Subscribed = true;
                userInfo.PaymentCustomerStatus = stripeSubscription.Status;

                db.Entry(userInfo).State = EntityState.Modified;
            }
            catch (Exception e)
            {
                Utilities.LogAppError("Subscription modify failed: " + e.Message);
                return false;
            }

            return true;
        }

        private bool DoesStripeCustomerExist(string customerId)
        {
            try
            {
                var customerService = new StripeCustomerService();

                StripeCustomer stripeCustomer = customerService.Get(customerId);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private bool CreateNewStripeCustomer(string email, string token, int quantity, UserInfo userInfo)
        {
            //create new customer
            var myCustomer = new StripeCustomerCreateOptions();

            myCustomer.Email = email;
            myCustomer.TokenId = token;
            myCustomer.PlanId = Constants.StripeSubscriptionPlan;
            myCustomer.Quantity = quantity;

            var customerService = new StripeCustomerService();

            try
            {
                StripeCustomer stripeCustomer = customerService.Create(myCustomer);

                //save profile ID and profile status with user info
                userInfo.Subscribed = true;
                userInfo.PaymentCustomerId = stripeCustomer.Id;
                userInfo.PaymentCustomerStatus = stripeCustomer.StripeSubscription.Status;
                db.Entry(userInfo).State = EntityState.Modified;
            }
            catch (Exception e)
            {
                Utilities.LogAppError("Create/Subscribe failed: " + e.Message);
                return false;
            }

            return true;
        }

        private UserInfo GetUserInfo(string email)
        {
            //retrieve user info entry
            IOrderedQueryable<UserInfo> userFound = from userInfo in db.UserInfo
                                                    where userInfo.Name == email
                                                    orderby userInfo.Name ascending
                                                    select userInfo;

            //there must be a user info entry found
            if ((userFound == null) || (userFound.Count() == 0))
            {
                return null;
            }

            //should only be one in the list
            IList<UserInfo> userInfoList = userFound.ToList();

            return userInfoList[0];
        }
    }
}
