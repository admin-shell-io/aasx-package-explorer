using AnyUi;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
            return new string[] { "value1", "value2" };
        }

        // GET api/<ImageController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

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

                // LINUX

                //if (img?.BitmapInfo?.ImageSource is BitmapSource bitmap)
                //{
                //    BitmapEncoder encoder = new PngBitmapEncoder();
                //    var bmp = new WriteableBitmap(bitmap);
                //    encoder.Frames.Add(BitmapFrame.Create(bmp));

                //    using (var memStream = new System.IO.MemoryStream())
                //    {
                //        encoder.Save(memStream);
                //        var ba = memStream.ToArray();
                //        return base.File(ba, "image/png");
                //    }
                //}
            }

            // default case
            // var bb = System.IO.File.ReadAllBytes(@"C:\MIHO\Develop\Aasx\repo\sample.png");
            using (var stream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("BlazorUI.Resources.sample.png"))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    var bb = ms.ToArray();
                    return base.File(bb, "image/png");
                }
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> GetUserImageFile()
        //{
        //    return new Microsoft.AspNetCore.Mvc.FileContentResult(new byte[] { 1, 2, 3 }, "image.png");
        //}


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
