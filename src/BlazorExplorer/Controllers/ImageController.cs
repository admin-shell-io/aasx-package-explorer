/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using AnyUi;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable MergeIntoPattern

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BlazorUI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        // GET: api/<ImageController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] { "value1", "value2" };
        }

        // GET api/<ImageController>/5
        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            // image?
            var img = AnyUiImage.FindImage(id);
            if (img != null)
            {
                if (img.BitmapInfo?.PngData != null)
                {
                    return base.File(img.BitmapInfo.PngData, "image/png");
                }
            }

            // default case
            // ReSharper disable ConvertToUsingDeclaration
            using (var stream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("BlazorUI.Resources.sample.png"))
            {
                // dead-csharp off
                // if (stream == null)
                //    return NotFound();
                // dead-csharp on

                using (MemoryStream ms = new MemoryStream())
                {
                    if (stream != null)
                    {
                        stream.CopyTo(ms);
                    }
                    else
                    {
                        var b = new Bitmap(1, 1);
                        b.SetPixel(0, 0, Color.White);
                        b.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                    var bb = ms.ToArray();
                    return base.File(bb, "image/png");
                }
            }
            // ReSharper enable ConvertToUsingDeclaration
        }

        // POST api/<ImageController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ImageController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ImageController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
