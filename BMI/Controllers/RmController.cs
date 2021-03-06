﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BMI.Data;
using BMI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BMI.Controllers
{
    public class RmController : Controller
    {
        private readonly ApplicationDbContext _db;

        private IWebHostEnvironment Environment;
        private IConfiguration Configuration;

        public RmController(ApplicationDbContext db,IWebHostEnvironment _environment, IConfiguration _configuration)
        {
            _db = db;
            Environment = _environment;
            Configuration = _configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List(string status)
        {
            if (status == "in_plant" || status == "otw" || status == "closed")
            {
                var list = _db.Rm
                    .Where(x => x.status == status)
                    .OrderByDescending(e => e.created_at)
                    .ToList();

                if (status == "in_plant")
                {
                    ViewBag.status = "In Plant";
                }
                else if (status == "otw")
                {
                    ViewBag.status = "On The Water";
                }
                else
                {
                    ViewBag.status = "Closed";
                }
                return View(list);
            }
            return NotFound();
        }

        public IActionResult Detail (string raw_source)
        {
            var list = _db.Rm_detail
                .Where(x => x.raw_source == raw_source)
                .Include(x => x.Masterdatamodel)
                .OrderByDescending(e => e.id_raw)
                .ToList();
            ViewBag.raw_source = raw_source;
            return View(list);
        }

        public IActionResult Getdata(string raw_source)
        {
            var obj = _db.Rm.Find(raw_source);
            return Json(obj);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Update(RmModel rmModel)
        {
            if (ModelState.IsValid)
            {
                var list_rm = _db.Rm.Where(x => x.raw_source == rmModel.raw_source).ToList();
                var list_rm_detail = _db.Rm_detail.Where(x => x.raw_source == rmModel.raw_source).ToList();
                if (rmModel.status == "In Plant")
                {
                    var status = "in_plant";
                    list_rm.ForEach(a =>
                    {
                        a.eta = rmModel.eta;
                        a.etd = rmModel.etd;
                        a.container = rmModel.container;
                        a.status = status;
                        a.updated_at = DateTime.Now;
                    });
                    list_rm_detail.ForEach(a =>
                    {
                        a.qty_received = a.qty_pl;
                        a.updated_at = DateTime.Now;
                    });
                    _db.SaveChanges();
                    TempData["msg"] = "Item Succesfully Updated";
                    TempData["result"] = "success";
                    return RedirectToAction("List", new { status = status });
                }
                else if (rmModel.status == "On The Water")
                {
                    var status = "otw";
                    list_rm.ForEach(a =>
                    {
                        a.eta = rmModel.eta;
                        a.etd = rmModel.etd;
                        a.container = rmModel.container;
                        a.status = status;
                        a.updated_at = DateTime.Now;
                    });
                    _db.SaveChanges();
                    TempData["msg"] = "Item Succesfully Updated";
                    TempData["result"] = "success";
                    return RedirectToAction("List", new { status = status });
                }
                else
                {
                    var status = "closed";
                    list_rm.ForEach(a =>
                    {
                        a.eta = rmModel.eta;
                        a.etd = rmModel.etd;
                        a.container = rmModel.container;
                        a.status = status;
                        a.updated_at = DateTime.Now;
                    });
                    _db.SaveChanges();
                    TempData["msg"] = "Item Succesfully Updated";
                    TempData["result"] = "success";
                    return RedirectToAction("List", new { status = status });
                }
            }
            TempData["msg"] = "Item Failed Updated";
            TempData["result"] = "failed";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Import(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                var allowedExtensions = new[] { ".xls", ".xlsx" };
                string fileName = Path.GetFileName(postedFile.FileName);
                var checkextension = Path.GetExtension(fileName).ToLower();

                if (allowedExtensions.Contains(checkextension))
                {
                    List<RmDetailModel> rm_detail = new List<RmDetailModel>();
                    List<RmModel> rm = new List<RmModel>();
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
                            if (excelDataReader.FieldCount == 10)
                            {

                                var conf = new ExcelDataSetConfiguration()
                                {
                                    ConfigureDataTable = a => new ExcelDataTableConfiguration
                                    {
                                        UseHeaderRow = true
                                    }
                                };

                                DataSet dataSet = excelDataReader.AsDataSet(conf);
                                DataRowCollection row = dataSet.Tables["Rm"].Rows;
                                List<object> rowDataList = null;

                                foreach (DataRow item in row)
                                {
                                    rowDataList = item.ItemArray.ToList();
                                    var raw_source = Convert.ToString(rowDataList[3]);
                                    var unique = _db.Rm.FirstOrDefault(m => m.raw_source == raw_source);
                                    if (unique == null)
                                    {
                                        var obj = new RmModel
                                        {
                                            raw_source = Convert.ToString(rowDataList[3]),
                                            etd = Convert.ToDateTime(rowDataList[0]),
                                            eta = Convert.ToDateTime(rowDataList[1]),
                                            container = Convert.ToString(rowDataList[2]),
                                            created_at = DateTime.Now,
                                            status = "otw"
                                        };
                                        _db.Rm.Add(obj);
                                        _db.SaveChanges();
                                    }
                                }


                                foreach (DataRow item in row)
                                {
                                    rowDataList = item.ItemArray.ToList();
                                    rm_detail.Add(new RmDetailModel
                                    {
                                        raw_source = Convert.ToString(rowDataList[3]),
                                        landing_site = Convert.ToString(rowDataList[4]),
                                        sap_code = Convert.ToString(rowDataList[5]),
                                        cases = Convert.ToInt32(rowDataList[6]),
                                        qty_pl = Convert.ToSingle(rowDataList[7]),
                                        usd_price = Convert.ToSingle(rowDataList[8]),
                                        ex_rate = Convert.ToSingle(rowDataList[9]),
                                        created_at = DateTime.Now
                                    });
                                }
                                _db.Rm_detail.AddRange(rm_detail);
                                _db.SaveChanges();
                                TempData["msg"] = "File Succesfully Uploaded";
                                TempData["result"] = "success";
                                return RedirectToAction("List", new { status = "otw" });
                            }
                            TempData["msg"] = "Field Column not Match";
                            TempData["result"] = "failed";
                            return RedirectToAction("List", new { status = "otw" });
                        }
                    }
                }
                //jika tidak sesuai extension
                TempData["msg"] = "File Extension must excel file format 'xlsx or xls'";
                TempData["result"] = "failed";
                return RedirectToAction("List", new { status = "otw" });
            }
            TempData["msg"] = "File Empty";
            TempData["result"] = "failed";
            return RedirectToAction("List", new { status = "otw" });
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Delete(string raw_source,string status)
        {
            var rm_detail = _db.Rm_detail.Where(x => x.raw_source == raw_source).ToList();
            _db.Rm_detail.RemoveRange(rm_detail);
            var rm = _db.Rm.Find(raw_source);
            _db.Rm.Remove(rm);
            _db.SaveChanges();
            TempData["msg"] = "Item Succesfully Deleted";
            TempData["result"] = "success";
            return RedirectToAction("List", new { status = status });
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Updatedetail(RmDetailModel obj)
        {
            _db.Rm_detail.Update(obj);
            _db.SaveChanges();
            TempData["msg"] = "Item Succesfully Updated";
            TempData["result"] = "success";
            return RedirectToAction("Detail",new { raw_source = obj.raw_source });
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Duplicateitem(RmDetailModel obj)
        {
            if (obj.id_raw == 0)
            {
                _db.Rm_detail.Add(obj);
                _db.SaveChanges();
                TempData["msg"] = "Item Succesfully Duplicated";
                TempData["result"] = "success";
                return RedirectToAction("Detail", new { raw_source = obj.raw_source });
            }
            else
            {
                TempData["msg"] = "Item Failed Duplicated";
                TempData["result"] = "failed";
                return RedirectToAction("Detail", new { raw_source = obj.raw_source });
            }
        }

        public IActionResult Getdetailitem(int id)
        {
            var obj = _db.Rm_detail.Find(id);
            return Json(obj);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Deleteitem(int id,string raw_source)
        {
            var obj = _db.Rm_detail.Find(id);
            _db.Rm_detail.Remove(obj);
            _db.SaveChanges();
            TempData["msg"] = "Item Succesfully Deleted";
            TempData["result"] = "success";
            return RedirectToAction("Detail", new { raw_source = raw_source });
        }


        public IActionResult Adddestroy(AdjustmentRawModel adjustmentRawModel)
        {
            if (ModelState.IsValid)
            {
                _db.AdjustmentRaw.Add(adjustmentRawModel);
                _db.SaveChanges();
                TempData["msg"] = "Raw Material Succesfully Destroy";
                TempData["result"] = "success";
                return Redirect(Request.Headers["Referer"].ToString());
            }
            TempData["msg"] = "Raw Material Failed to Destroy";
            TempData["result"] = "failed";
            return Redirect(Request.Headers["Referer"].ToString());
        }



    }
}
