﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BMI.Models
{
    public class ProductionInputModel
    {
        [Key]
        public int id_productioninput { get; set; }
        public string po_bmi { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime date { get; set; }
        public POModel POModel { get; set; }
        [ForeignKey("POModel")]
        public string po { get; set; }
        public string raw_source { get; set; }

        public Masterdatamodel Masterdatamodel { get; set; }
        [ForeignKey("Masterdatamodel")]
        public string sap_code { get; set; }
        public float qty { get; set; }
        public string landing_site { get; set; }
        
        
        #nullable enable
        public string? saved { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }

        
    }
}
