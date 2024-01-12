// Import namespaces we will use
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Drawing;

namespace Tutorial_FaceRecognition
{
    internal class Program
    {
        // Computer vision client
        private static ComputerVisionClient cvClient;
        // Folder path containing the source images
        static string folderPath = "C:\\temp\\images\\face_recognition";
        // Folder path where we want the analysed images saved
        static string peopleDirectory = $"C:\\temp\\images\\face_recognition\\PEOPLE";
        static string notpeopleDirectory = $"C:\\temp\\images\\face_recognition\\NOT PEOPLE";

        static async Task Main(string[] args)
        {
            try
            {
                // Get Azure account settings from appsettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                string cogSvcKey = configuration["CognitiveServiceKey"];

                // Authenticate client
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                // Get all image files (jpg only) we want to analyse and use a counter to keep track of the image name
                string[] imageFiles = Directory.EnumerateFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly).ToArray();
                int counter = 1;

                foreach (string imageFile in imageFiles)
                {
                    // Send image for analysis
                    await AnalyzeImage(imageFile, counter);
                    counter++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task AnalyzeImage(string imageFile, int counter)
        {
            try
            {
                Console.WriteLine($"Analyzing {imageFile}");

                // Specify features to be retrieved 
                List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
                {
                     VisualFeatureTypes.Description,
                     VisualFeatureTypes.Faces
                };

                // Do image analysis
                using (var imageData = File.OpenRead(imageFile))
                {
                    // Prepare image files
                    string fileName = Path.GetFileName(imageFile);
                    string newFileName = string.Empty;
                    string destinationFileName = string.Empty;

                    // Send image for analysis by Azure AI
                    var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);
                    // Get faces data
                    var faces = analysis.Faces;
                    // Get description data
                    var caption = analysis.Description.Captions;

                    // Prepare image for drawing
                    Image image = Image.FromFile(imageFile);
                    Graphics graphics = Graphics.FromImage(image);
                    Pen pen = new Pen(Color.Red, 5);
                    Font font = new Font("Arial", 16);
                    SolidBrush brush = new SolidBrush(Color.WhiteSmoke);

                    if (faces.Count > 0)
                    {
                        for (int i = 0; i < faces.Count; i++)
                        {
                            // Draw object bounding box
                            var r = faces[i].FaceRectangle;
                            Rectangle rect = new Rectangle(r.Left, r.Top, r.Width, r.Height);
                            graphics.DrawRectangle(pen, rect);
                        }

                        // If there is a description (caption) for the image
                        if (caption != null)
                        {
                            // Save the edited image to the people directory
                            // With a filename that corresponds to the description
                            newFileName = $"{counter}-{caption[0].Text}.jpg";
                            destinationFileName = Path.Combine(peopleDirectory, newFileName);
                        }
                        else
                        {
                            destinationFileName = Path.Combine(peopleDirectory, fileName);
                        }

                        image.Save(destinationFileName);
                    }
                    else
                    {
                        if (caption != null)
                        {
                            // Save the edited image to the not people directory
                            // With a filename that corresponds to the description
                            newFileName = $"{counter}-{caption[0].Text}.jpg";
                            // Copy the image to the not people folder
                            destinationFileName = Path.Combine(notpeopleDirectory, newFileName);
                            File.Copy(imageFile, destinationFileName, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}