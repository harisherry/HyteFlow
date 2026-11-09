using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HyteFlow
{
    public enum ChassisGeometryStyle
    {
        PanoramicVisionShroudLcd = 0,
        HyteChamferedDualChamber = 1,
        PillarlessDualChamber = 2,
        FrontMeshTower = 3
    }

    public class CaseModelDefinition
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Brand { get; set; }
        public string FormFactor { get; set; } = "Panoramic Mid-Tower";
        public ChassisGeometryStyle GeometryStyle { get; set; }
        public string LocalAssetFileName { get; set; }
        public bool HasShroudLcdScreen { get; set; } = false;

        public int MaxTopFans { get; set; } = 3;
        public int MaxSideFans { get; set; } = 3;
        public int MaxBottomFans { get; set; } = 3;
        public int MaxRearFans { get; set; } = 1;

        public Point TopZoneCenter { get; set; } = new Point(0.45, 0.175);
        public Point SideZoneCenter { get; set; } = new Point(0.625, 0.44);
        public Point BottomZoneCenter { get; set; } = new Point(0.45, 0.77);
        public Point RearZoneCenter { get; set; } = new Point(0.305, 0.345);
        public Point CpuZoneCenter { get; set; } = new Point(0.455, 0.375);
        public Point GpuZoneCenter { get; set; } = new Point(0.445, 0.585);
    }

    public static class CaseCatalog
    {
        public static readonly List<CaseModelDefinition> Presets = new List<CaseModelDefinition>
        {
            new CaseModelDefinition
            {
                Id = "thermalright_tl_m10_vision",
                DisplayName = "Thermalright TL-M10 Vision (9.16\" LCD Panoramic)",
                Brand = "Thermalright",
                FormFactor = "Panoramic Vision mATX / Mid-Tower",
                GeometryStyle = ChassisGeometryStyle.PanoramicVisionShroudLcd,
                LocalAssetFileName = "thermalright_tl_m10_vision_3d.png",
                HasShroudLcdScreen = true,
                MaxTopFans = 3,
                MaxSideFans = 3,
                MaxBottomFans = 3,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "hyte-y70",
                DisplayName = "HYTE Y70 Touch / Dual-Chamber Panoramic",
                Brand = "HYTE",
                FormFactor = "3-Piece Panoramic Dual-Chamber ATX",
                GeometryStyle = ChassisGeometryStyle.HyteChamferedDualChamber,
                LocalAssetFileName = "hyte_y70_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 3,
                MaxSideFans = 3,
                MaxBottomFans = 3,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "lianli-o11-evo",
                DisplayName = "Lian Li O11 Dynamic EVO / O11 Vision",
                Brand = "Lian Li",
                FormFactor = "Pillarless Dual-Chamber Glass ATX",
                GeometryStyle = ChassisGeometryStyle.PillarlessDualChamber,
                LocalAssetFileName = "lianli_o11_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 3,
                MaxSideFans = 3,
                MaxBottomFans = 3,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "nzxt-h9-flow",
                DisplayName = "NZXT H9 Flow / Elite Panoramic",
                Brand = "NZXT",
                FormFactor = "Dual-Chamber Panoramic Mid-Tower",
                GeometryStyle = ChassisGeometryStyle.PillarlessDualChamber,
                LocalAssetFileName = "nzxt_h9_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 3,
                MaxSideFans = 3,
                MaxBottomFans = 3,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "corsair-5000d",
                DisplayName = "Corsair 5000D / 4000D High-Airflow",
                Brand = "Corsair",
                FormFactor = "High-Airflow Mesh ATX Tower",
                GeometryStyle = ChassisGeometryStyle.FrontMeshTower,
                LocalAssetFileName = "corsair_5000d_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 3,
                MaxSideFans = 3,
                MaxBottomFans = 2,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "montech-king-95",
                DisplayName = "Montech King 95 Pro Curved Glass",
                Brand = "Montech",
                FormFactor = "Curved Panoramic Dual-Chamber",
                GeometryStyle = ChassisGeometryStyle.PillarlessDualChamber,
                LocalAssetFileName = "montech_king_95_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 3,
                MaxSideFans = 2,
                MaxBottomFans = 3,
                MaxRearFans = 1
            },
            new CaseModelDefinition
            {
                Id = "phanteks-nv7",
                DisplayName = "Phanteks NV7 / NV5 Showcase Full-Tower",
                Brand = "Phanteks",
                FormFactor = "Showcase Pillarless Full-Tower",
                GeometryStyle = ChassisGeometryStyle.PanoramicVisionShroudLcd,
                LocalAssetFileName = "phanteks_nv7_3d.png",
                HasShroudLcdScreen = true,
                MaxTopFans = 3,
                MaxSideFans = 4,
                MaxBottomFans = 3,
                MaxRearFans = 2
            },
            new CaseModelDefinition
            {
                Id = "fractal-north",
                DisplayName = "Fractal Design North / XL Charcoal",
                Brand = "Fractal Design",
                FormFactor = "Walnut Slat Airflow Mid-Tower",
                GeometryStyle = ChassisGeometryStyle.FrontMeshTower,
                LocalAssetFileName = "fractal_north_3d.png",
                HasShroudLcdScreen = false,
                MaxTopFans = 2,
                MaxSideFans = 3,
                MaxBottomFans = 2,
                MaxRearFans = 1
            }
        };

        public static string GetAssetsDirectory()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return dir;
        }

        public static CaseModelDefinition FindOrCreatePreset(string idOrName)
        {
            if (string.IsNullOrWhiteSpace(idOrName))
                return Presets[0];

            var direct = Presets.FirstOrDefault(p =>
                string.Equals(p.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.DisplayName, idOrName, StringComparison.OrdinalIgnoreCase));
            if (direct != null) return direct;

            string slug = Regex.Replace(idOrName.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            var bySlug = Presets.FirstOrDefault(p => string.Equals(p.Id, slug, StringComparison.OrdinalIgnoreCase));
            if (bySlug != null) return bySlug;

            var custom = CreateDefinitionForQuery(idOrName);
            Presets.Add(custom);
            return custom;
        }

        public static CaseModelDefinition CreateDefinitionForQuery(string caseQuery)
        {
            string cleanName = (caseQuery ?? "Custom PC Case").Trim();
            string slug = Regex.Replace(cleanName.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            if (string.IsNullOrEmpty(slug)) slug = "custom_case";

            string fileName = $"{slug}_3d.png";
            bool hasLcd = cleanName.IndexOf("vision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cleanName.IndexOf("m10", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cleanName.IndexOf("lcd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cleanName.IndexOf("touch", StringComparison.OrdinalIgnoreCase) >= 0;

            ChassisGeometryStyle style = ChassisGeometryStyle.PillarlessDualChamber;
            if (hasLcd || cleanName.IndexOf("thermalright", StringComparison.OrdinalIgnoreCase) >= 0)
                style = ChassisGeometryStyle.PanoramicVisionShroudLcd;
            else if (cleanName.IndexOf("hyte", StringComparison.OrdinalIgnoreCase) >= 0)
                style = ChassisGeometryStyle.HyteChamferedDualChamber;
            else if (cleanName.IndexOf("corsair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("fractal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("lancool", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("mesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("flow", StringComparison.OrdinalIgnoreCase) >= 0 && cleanName.IndexOf("h9", StringComparison.OrdinalIgnoreCase) < 0)
                style = ChassisGeometryStyle.FrontMeshTower;

            return new CaseModelDefinition
            {
                Id = slug,
                DisplayName = cleanName,
                Brand = cleanName.Split(' ')[0],
                FormFactor = hasLcd ? "Panoramic Vision LCD Chassis" : "Custom 3D Studio Chassis",
                GeometryStyle = style,
                LocalAssetFileName = fileName,
                HasShroudLcdScreen = hasLcd
            };
        }

        public static BitmapSource LoadCaseImage(CaseModelDefinition model)
        {
            if (model == null) return null;
            string assetsDir = GetAssetsDirectory();
            string path = Path.Combine(assetsDir, model.LocalAssetFileName ?? "thermalright_tl_m10_vision_3d.png");

            if (!File.Exists(path))
                return null;

            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();

                // Validate image quality: reject tiny images or white-background close-up photos
                if (bi.PixelWidth < 360 || bi.PixelHeight < 280)
                    return null;

                if (HasBrightOrSquareCropBackdrop(bi))
                    return null;

                return bi;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasBrightOrSquareCropBackdrop(BitmapSource bmp)
        {
            try
            {
                var converted = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);
                int w = converted.PixelWidth;
                int h = converted.PixelHeight;
                int stride = w * 4;
                byte[] pixels = new byte[h * stride];
                converted.CopyPixels(pixels, stride, 0);

                // Sample corners to see if it has a bright/white catalog border instead of a dark studio stage
                Point[] sampleNorms = new[]
                {
                    new Point(0.02, 0.02),
                    new Point(0.98, 0.02),
                    new Point(0.02, 0.98),
                    new Point(0.98, 0.98),
                    new Point(0.50, 0.02),
                    new Point(0.02, 0.50)
                };

                int brightSamples = 0;
                foreach (var pt in sampleNorms)
                {
                    int x = Math.Max(0, Math.Min(w - 1, (int)(pt.X * w)));
                    int y = Math.Max(0, Math.Min(h - 1, (int)(pt.Y * h)));
                    int idx = y * stride + x * 4;
                    byte b = pixels[idx];
                    byte g = pixels[idx + 1];
                    byte r = pixels[idx + 2];
                    byte a = pixels[idx + 3];

                    double luma = (0.299 * r + 0.587 * g + 0.114 * b);
                    if (a > 180 && luma > 85)
                        brightSamples++;
                }

                return brightSamples >= 2;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<CaseModelDefinition> DownloadCaseRenderAsync(string caseQuery)
        {
            var customDef = CreateDefinitionForQuery(caseQuery);
            string savePath = Path.Combine(GetAssetsDirectory(), customDef.LocalAssetFileName);

            // Check if we already have a valid studio asset on disk for this preset
            var existingValid = LoadCaseImage(customDef);
            if (existingValid == null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        using (var wc = new WebClient())
                        {
                            wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HyteFlowStudio/3.0");
                            string q = Uri.EscapeDataString(customDef.DisplayName + " pc case full chassis three quarter view dark background png");
                            string html = wc.DownloadString($"https://www.bing.com/images/search?q={q}&qft=+filterui:photo-transparent+filterui:aspect-wide");

                            var matches = Regex.Matches(html, @"murl&quot;:&quot;(https?://[^&]+?\.(?:png|webp))&quot;", RegexOptions.IgnoreCase);
                            foreach (Match m in matches)
                            {
                                try
                                {
                                    string imgUrl = m.Groups[1].Value;
                                    byte[] data = wc.DownloadData(imgUrl);
                                    if (data != null && data.Length > 18000)
                                    {
                                        File.WriteAllBytes(savePath, data);
                                        if (LoadCaseImage(customDef) != null)
                                            break;
                                        File.Delete(savePath);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                });
            }

            var existingIdx = Presets.FindIndex(p => string.Equals(p.Id, customDef.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIdx >= 0) Presets[existingIdx] = customDef;
            else Presets.Insert(0, customDef);

            return customDef;
        }
    }
}
