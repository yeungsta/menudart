﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Models
{
    public class ActivateViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string ReturnEditPage { get; set; }
    }
}