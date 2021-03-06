﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BMI.Models
{
    public class AdjustmentFGModel
    {   
        [Key]
        public int id_adjustmentFG { get; set; }
        public MasterBMIModel MasterBMIModel { get; set; }
        [ForeignKey("MasterBMIModel")]
        public string bmi_code { get; set; }
        public string raw_source { get; set; }
        public int qty { get; set; }
        public POModel POModel { get; set; }
        [ForeignKey("POModel")]
        public string po { get; set; }
        #nullable enable
        public string? reason { get; set; }
        public string? status { get; set; }

        [NotMapped]
        public string? pt { get; set; }
    }
}
