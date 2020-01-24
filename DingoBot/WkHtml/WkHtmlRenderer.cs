using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DingoBot.WkHtml
{
    public class WkHtmlRenderer
    {
        public struct Crop
        {
            public int X, Y, Width, Height;
        }


        /// <summary>
        /// The cropping of the image.
        /// </summary>
        public Crop? Cropping { get; set; }

        /// <summary>
        /// Height to render the page
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// Width to render the page
        /// </summary>
        public int Width { get; set; } = 1024;

        /// <summary>
        /// Quality of the image
        /// </summary>
        public int Quality { get; set; } = 96;

        /// <summary>
        /// The output image format
        /// </summary>
        public string Format { get; set; } = "png";

        /// <summary>
        /// Should JavaScript be disabled?
        /// </summary>
        public bool DisableJavaScript { get; set; } = false;

        /// <summary>
        /// Path to the WkHtmlToImage executable
        /// </summary>
        public string WkHtmlToImagePath { get; }


        /// <summary>
        /// Creates a new WkHtmlRenderer
        /// </summary>
        /// <param name="tooling"></param>
        public WkHtmlRenderer(string tooling)
        {
            WkHtmlToImagePath = tooling;
        }

        /// <summary>
        /// Renders the path to a temporary file and reads the bytes once completed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<byte[]> RenderBytesAsync(Uri uri) {
            return this.RenderBytesAsync(uri.AbsoluteUri);
        }

        /// <summary>
        /// Renders the path to a temporary file and reads the bytes once completed.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<byte[]> RenderBytesAsync(string input) {

            //Get a temporary file name
            string temp = Path.GetTempFileName();

            try
            {
                //Render hte page out. If it doesnt exit with 0, it broke D:
                if (await RenderAsync(input, temp) != 0)
                    return null;

                //Read the bytes of the temporary file
                return await File.ReadAllBytesAsync(temp);
            }
            finally
            {
                //Delete the file once we are actually done
                File.Delete(temp);
            }
        }

        /// <summary>
        /// Renders the URI
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public Task<int> RenderAsync(Uri uri, string output) {
            return this.RenderAsync(uri.AbsoluteUri, output);
        }

        /// <summary>
        /// Renders the path
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public Task<int> RenderAsync(string input, string output)
        {
            //Prepare the token source
            var tokenCompletionSource = new TaskCompletionSource<int>();

            //Prepare the arguments and append the basics
            var args = new StringBuilder();
            args.Append(" --format ").Append(Format);
            args.Append(" --width ").Append(Width);
            args.Append(" --height ").Append(Height);
            args.Append(" --quality ").Append(Quality);
            args.Append(" --quiet ");

            //Append the Disable JS
            if (DisableJavaScript)
                args.Append(" --disable-javascript");

            //Append the crop
            if (Cropping.HasValue)
            {
                args.Append(" --crop-x ").Append(Cropping.Value.X);
                args.Append(" --crop-y ").Append(Cropping.Value.Y);
                args.Append(" --crop-w ").Append(Cropping.Value.Width);
                args.Append(" --crop-h ").Append(Cropping.Value.Height);
            }

            //Append all the cookies
            foreach(var kp in _cookies) {
                args.Append(" --cookie ").Append(kp.Key).Append(" ").Append(kp.Value);
            }

            //Finally, append the input and output
            args.Append(" \"").Append(input).Append("\"");
            args.Append(" \"").Append(output).Append("\"");

            var argstr = args.ToString();
            Console.WriteLine("Executing: {0}", this.WkHtmlToImagePath + " " + argstr);

            //Create the process
            var process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = {
                    FileName = this.WkHtmlToImagePath,
                    Arguments = argstr
                }
            };

            //Listen to when the process exits
            process.Exited += (sender, ags) =>
            {
                //Tell the task we are done and dispose the process
                tokenCompletionSource.SetResult(process.ExitCode);
                process.Dispose();
            };

            //Start the process and return the token source
            process.Start();
            return tokenCompletionSource.Task;
        }

        #region Cookies

        //Cookies to send
        private Dictionary<string, string> _cookies = new Dictionary<string, string>(0);

        /// <summary>
        /// Sets the cookie to submit before requesting the URL
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetCookie(string name, string value)
        {
            _cookies[name] = HttpUtility.UrlEncode(value);
        }

        /// <summary>
        /// Gets the cookie, returns null if it doesnt exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetCookie(string name)  {
            if (_cookies.TryGetValue(name, out var c)) return c;
            return null;
        }

        /// <summary>
        /// Deletes a particular cookie
        /// </summary>
        /// <param name="name"></param>
        public bool ClearCookie(string name)
        {
            return _cookies.Remove(name);
        }

        /// <summary>
        /// Clears all cookies
        /// </summary>
        public void ClearCookies()
        {
            _cookies.Clear();
        }
        #endregion

        /// <summary>
        /// Sets the width and height of the image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Sets the current cropping
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetCrop(int x, int y, int width, int height) {
            Cropping = new Crop()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
        }
    }
}
