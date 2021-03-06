﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace MenuDart.Models
{
    public class Menu
    {
        public int ID { get; set; }
        [Required(ErrorMessage = "You must enter a restaurant name.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "You must enter the city of your restaurant.")]
        public string City { get; set; }
        public bool Active { get; set; }
        public bool ChangesUnpublished { get; set; }
        public string Website { get; set; }
        public string AboutTitle { get; set; }
        public string AboutText { get; set; }
        public string Template { get; set; }
        public string Owner { get; set; }
        public string MenuDartUrl { get; set; }
        public string PreviousMenuDartUrl { get; set; }
        //populated only if one location, or shared across all locations
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Yelp { get; set; }
        [Column(TypeName = "xml")]
        public string Locations { get; set; }
        [Column(TypeName = "xml")]
        public string MenuTree { get; set; }
    }

    public class Location
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        [RegularExpression(@"^[A-Za-z]{2}", ErrorMessage = "State must be a 2-character abbreviation.")]
        public string State { get; set; }
        [RegularExpression(@"^\d{5}", ErrorMessage = "Zip code must be a 5-digit number.")]
        public string Zip { get; set; }
        public string MapImgUrl { get; set; }
        public string MapLink { get; set; }
        public string Hours { get; set; }
        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [DataType(DataType.Url)]
        public string Facebook { get; set; }
        [DataType(DataType.Url)]
        public string Twitter { get; set; }
        [DataType(DataType.Url)]
        public string Yelp { get; set; }
    }

    [XmlInclude(typeof(MenuLeaf))]
    public class MenuNode
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Text { get; set; }
        public List<MenuNode> Branches { get; set; }
    }

    public class MenuLeaf : MenuNode
    {
        public string Description { get; set; }
        public decimal? Price { get; set; }
    }

    public class Template
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
    }

    public class TempMenu
    {
        public int ID { get; set; }
        public string SessionId { get; set; }
        public int MenuId { get; set; }
        public System.DateTime DateCreated { get; set; }
    }

    public class UserInfo
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public bool Subscribed { get; set; }
        public bool TrialEnded { get; set; }
        public bool TrialExpWarningSent { get; set; }
        public string PaymentCustomerId { get; set; }
        public string PaymentCustomerStatus { get; set; }
    }

    public class MenuDartDBContext : DbContext
    {
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TempMenu> TempMenus { get; set; }
        public DbSet<UserInfo> UserInfo { get; set; }

        //we won't be storing Locations in it's SQL table, however. Just as XML
        //in the Menus table.
        public DbSet<Location> Locations { get; set; }

        //we won't be storing MenuNodes in it's SQL table, however. Just as XML
        //in the Menus table.
        public DbSet<MenuNode> MenuTree { get; set; }

#if Staging  //map to staging database tables
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Menu>().ToTable("Menus_staging");
            modelBuilder.Entity<Template>().ToTable("Templates_staging");
            modelBuilder.Entity<TempMenu>().ToTable("TempMenus_staging");
            modelBuilder.Entity<UserInfo>().ToTable("UserInfoes_staging");
        }
#endif


    }
}