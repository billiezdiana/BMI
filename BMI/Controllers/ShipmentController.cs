﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BMI.Models;
using BMI.Data;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using ExcelDataReader;

namespace BMI.Controllers
{
    public class ShipmentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private IWebHostEnvironment Environment;
        private IConfiguration Configuration;

        public ShipmentController(ApplicationDbContext db, IWebHostEnvironment _environment, IConfiguration _configuration)
        {
            _db = db;
            Environment = _environment;
            Configuration = _configuration;
        }


        public IActionResult Index()
        {
            var list = _db.PO.OrderByDescending(e => e.po).ToList();
            return View(list);
        }

        //[HttpPost]
        //public IActionResult IdExist(Shipmentmodel shipmentmodel)
        //{
        //    var unique = _db.Shipment.SingleOrDefault(m => m.id_shipment == shipmentmodel.id_shipment);
        //    if (unique != null)
        //    {
        //        return Json(false);
        //    }
        //    return Json(true);
        //}

        [HttpPost]
        public IActionResult POExist(POModel POModel)
        {
            var unique = _db.PO.SingleOrDefault(m => m.po == POModel.po);
            if (unique != null)
            {
                return Json(false);
            }
            return Json(true);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Create(POModel POModel)
        {
            if (ModelState.IsValid)
            {
                _db.PO.Add(POModel);
                _db.SaveChanges();
                TempData["msg"] = "New Shipment Succesfully Added";
                TempData["result"] = "success";
                return Redirect(Request.Headers["Referer"].ToString());
            }
            TempData["msg"] = "New Shipment Failed to Added";
            TempData["result"] = "failed";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Update(POModel POModel)
        {
            if (ModelState.IsValid)
            {
                var po = _db.PO.Find(POModel.po);

                po.etd = POModel.etd;
                po.eta = POModel.eta;
                po.document_date = POModel.document_date;
                po.ocean_carrier = POModel.ocean_carrier;
                po.container = POModel.container;
                po.voyage_no = POModel.voyage_no;
                po.house_bol = POModel.house_bol;
                po.vessel_name = POModel.vessel_name;
                po.inv_no = POModel.inv_no;
                po.fda_no = POModel.fda_no;
                po.seal_no = POModel.seal_no;
                po.destination = POModel.destination;
                po.port_loading = "Surabaya";
                po.updated_at = DateTime.Now;
                _db.Entry(po).State = EntityState.Modified;
                _db.SaveChanges();

                TempData["msg"] = "Shipment Succesfully Updated";
                TempData["result"] = "success";
                return Redirect(Request.Headers["Referer"].ToString());
            }
            TempData["msg"] = "Shipment Failed to Updated";
            TempData["result"] = "failed";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Delete(int id)
        {
            var obj = _db.Shipment_detail.Find(id);
            _db.Shipment_detail.Remove(obj);
            _db.SaveChanges();
            TempData["msg"] = "Shipment Succesfully Deleted";
            TempData["result"] = "success";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Getshipment(string po)
        {
            var obj = _db.PO.Find(po);
            return Json(obj);
        }

        public IActionResult Detail(string po)
        {
            ViewBag.po = po;
            var obj = _db.Shipment_detail
                .Where(a => a.po == po)
                .Include(c => c.MasterBMIModel)
                .ToList();
            return View(obj);
        }


        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Import(IFormFile postedFile, string po )
        {
            if (postedFile != null)
            {
                var allowedExtensions = new[] { ".xls", ".xlsx" };
                string fileName = Path.GetFileName(postedFile.FileName);
                var checkextension = Path.GetExtension(fileName).ToLower();

                if (allowedExtensions.Contains(checkextension))
                {
                    List<ShipmentDetailModel> shipmentdata = new List<ShipmentDetailModel>();
                    string path = Path.Combine(this.Environment.WebRootPath, "Uploads");

                    string filePath = Path.Combine(path, fileName);
                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        postedFile.CopyTo(stream);
                    }

                    // For .net core, the next line requires the NuGet package, 
                    //System.Text.Encoding.CodePages
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
                    {
                        {
                            IExcelDataReader excelDataReader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
                            if (excelDataReader.FieldCount == 4)
                            {

                                var conf = new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = a => new ExcelDataTableConfiguration
                                    {
                                        UseHeaderRow = true
                                    }
                                };

                                DataSet dataSet = excelDataReader.AsDataSet(conf);
                                DataRowCollection row = dataSet.Tables["Shipment"].Rows;
                                List<object> rowDataList = null;

                                foreach (DataRow item in row)
                                {
                                    rowDataList = item.ItemArray.ToList();
                                    shipmentdata.Add(new ShipmentDetailModel
                                    {
                                        po = po,
                                        bmi_code = Convert.ToString(rowDataList[0]),
                                        batch = Convert.ToString(rowDataList[1]),
                                        qty = Convert.ToInt32(rowDataList[2]),
                                        index = Convert.ToSingle(rowDataList[3]),
                                        created_at = DateTime.Now
                                    });
                                }
                                _db.Shipment_detail.AddRange(shipmentdata);
                                _db.SaveChanges();
                                TempData["msg"] = "File Succesfully Uploaded";
                                TempData["result"] = "success";
                                return Redirect(Request.Headers["Referer"].ToString());
                            }
                            //jika kolom lebih besar dari 4
                            TempData["msg"] = "Field Column not Match";
                            TempData["result"] = "failed";
                            return Redirect(Request.Headers["Referer"].ToString());
                        }
                    }
                }
                //jika tidak sesuai extension
                TempData["msg"] = "Field Extension must excel file format 'xlsx or xls'";
                TempData["result"] = "failed";
                return Redirect(Request.Headers["Referer"].ToString());

            }
            //jika file kosong
            TempData["msg"] = "File Empty";
            TempData["result"] = "failed";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Getitem(int id)
        {
            var obj = _db.Shipment_detail.Find(id);
            return Json(obj);
        }


        public IActionResult Updateitem(ShipmentDetailModel shipmentDetailModel)
        {
            if (ModelState.IsValid)
            {
                var shipment = _db.Shipment_detail.Find(shipmentDetailModel.id_shipment_detail);
                shipment.qty = shipmentDetailModel.qty;
                shipment.batch = shipmentDetailModel.batch;
                shipment.updated_at = DateTime.Now;
                _db.Entry(shipment).State = EntityState.Modified;
                _db.SaveChanges();
                TempData["msg"] = "Item Succesfully Updated";
                TempData["result"] = "success";
                return Redirect(Request.Headers["Referer"].ToString());
            }
            TempData["msg"] = "Item Failed to Update";
            TempData["result"] = "failed";
            return Redirect(Request.Headers["Referer"].ToString());
        }












    }
}
