using System;
using System.Collections.Generic;
using System.IO;
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
        HyteChamferedDualChamber = 0,
        PillarlessDualChamber = 1,
        FrontMeshTower = 2
    }

    public class CaseModelDefinition
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public ChassisGeometryStyle GeometryStyle { get; set; }
        public string LocalAssetFileName { get; set; }
        public string FallbackImageUrl { get; set; }

        public Point TopZoneCenter { get; set; } = new Point(0.46, 0.20);
        public Point SideZoneCenter { get; set; } = new Point(0.75, 0.46);
        public Point BottomZoneCenter { get; set; } = new Point(0.48, 0.80);
        public Point RearZoneCenter { get; set; } = new Point(0.18, 0.36);
        public Point CpuZoneCenter { get; set; } = new Point(0.45, 0.42);
        public Point GpuZoneCenter { get; set; } = new Point(0.46, 0.63);
    }

    public static class CaseCatalog
    {
        public static readonly List<CaseModelDefinition> Presets = new List<CaseModelDefinition>
        {
            new CaseModelDefinition
            {
                Id = "hyte-y70",
                DisplayName = "HYTE Y70 (Dual-Chamber 3D Panoramic)",
                GeometryStyle = ChassisGeometryStyle.HyteChamferedDualChamber,
                LocalAssetFileName = "hyte_y70_3d.png",
                TopZoneCenter = new Point(0.46, 0.19),
                SideZoneCenter = new Point(0.76, 0.47),
                BottomZoneCenter = new Point(0.48, 0.81),
                RearZoneCenter = new Point(0.18, 0.36),
                CpuZoneCenter = new Point(0.45, 0.41),
                GpuZoneCenter = new Point(0.46, 0.62)
            },
            new CaseModelDefinition
            {
                Id = "hyte-y60",
                DisplayName = "HYTE Y60 (Panoramic Corner Glass)",
                GeometryStyle = ChassisGeometryStyle.HyteChamferedDualChamber,
                LocalAssetFileName = "hyte_y60_3d.png",
                TopZoneCenter = new Point(0.46, 0.20),
                SideZoneCenter = new Point(0.76, 0.48),
                BottomZoneCenter = new Point(0.48, 0.81),
                RearZoneCenter = new Point(0.18, 0.38),
                CpuZoneCenter = new Point(0.45, 0.42),
                GpuZoneCenter = new Point(0.46, 0.63)
            },
            new CaseModelDefinition
            {
                Id = "lianli-o11-evo",
                DisplayName = "Lian Li O11 Dynamic EVO (Dual-Chamber)",
                GeometryStyle = ChassisGeometryStyle.PillarlessDualChamber,
                LocalAssetFileName = "lianli_o11_3d.png",
                TopZoneCenter = new Point(0.46, 0.18),
                SideZoneCenter = new Point(0.75, 0.46),
                BottomZoneCenter = new Point(0.46, 0.80),
                RearZoneCenter = new Point(0.17, 0.36),
                CpuZoneCenter = new Point(0.44, 0.41),
                GpuZoneCenter = new Point(0.45, 0.62)
            },
            new CaseModelDefinition
            {
                Id = "nzxt-h9-flow",
                DisplayName = "NZXT H9 Flow (Dual-Chamber Panoramic)",
                GeometryStyle = ChassisGeometryStyle.PillarlessDualChamber,
                LocalAssetFileName = "nzxt_h9_3d.png",
                TopZoneCenter = new Point(0.46, 0.18),
                SideZoneCenter = new Point(0.76, 0.46),
                BottomZoneCenter = new Point(0.46, 0.80),
                RearZoneCenter = new Point(0.17, 0.36),
                CpuZoneCenter = new Point(0.44, 0.41),
                GpuZoneCenter = new Point(0.45, 0.62)
            },
            new CaseModelDefinition
            {
                Id = "corsair-5000d",
                DisplayName = "Corsair 5000D / 4000D Airflow (Mid-Tower)",
                GeometryStyle = ChassisGeometryStyle.FrontMeshTower,
                LocalAssetFileName = "corsair_5000d_3d.png",
                TopZoneCenter = new Point(0.45, 0.18),
                SideZoneCenter = new Point(0.80, 0.48),
                BottomZoneCenter = new Point(0.45, 0.79),
                RearZoneCenter = new Point(0.17, 0.34),
                CpuZoneCenter = new Point(0.42, 0.40),
                GpuZoneCenter = new Point(0.43, 0.60)
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

        public static BitmapSource LoadCaseImage(CaseModelDefinition model)
        {
            if (model == null) return null;
            string assetsDir = GetAssetsDirectory();
            string path = Path.Combine(assetsDir, model.LocalAssetFileName ?? "hyte_y70_3d.png");

            if (!File.Exists(path))
            {
                string fallback = Path.Combine(assetsDir, "hyte_y70_3d.png");
                if (File.Exists(fallback) && model.Id.StartsWith("hyte"))
                    path = fallback;
            }

            if (!File.Exists(path)) return null;

            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<CaseModelDefinition> DownloadCaseRenderAsync(string caseQuery)
        {
            string cleanName = (caseQuery ?? "Custom Case").Trim();
            string slug = Regex.Replace(cleanName.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            if (string.IsNullOrEmpty(slug)) slug = "custom_case";

            string fileName = $"{slug}_3d.png";
            string savePath = Path.Combine(GetAssetsDirectory(), fileName);

            ChassisGeometryStyle style = ChassisGeometryStyle.PillarlessDualChamber;
            if (cleanName.IndexOf("hyte", StringComparison.OrdinalIgnoreCase) >= 0)
                style = ChassisGeometryStyle.HyteChamferedDualChamber;
            else if (cleanName.IndexOf("corsair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("fractal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("lancool", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     cleanName.IndexOf("mesh", StringComparison.OrdinalIgnoreCase) >= 0)
                style = ChassisGeometryStyle.FrontMeshTower;

            await Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HyteFlowStudio/2.0");
                        string q = Uri.EscapeDataString(cleanName + " pc case black product render transparent png");
                        string html = wc.DownloadString($"https://www.bing.com/images/search?q={q}&qft=+filterui:photo-transparent");

                        var match = Regex.Match(html, @"murl&quot;:&quot;(https?://[^&]+?\.(?:png|jpg|webp))&quot;", RegexOptions.IgnoreCase);
                        if (!match.Success)
                            match = Regex.Match(html, @"murl&quot;:&quot;(https?://[^&]+?)&quot;", RegexOptions.IgnoreCase);

                        if (match.Success)
                        {
                            string imgUrl = match.Groups[1].Value;
                            byte[] data = wc.DownloadData(imgUrl);
                            if (data != null && data.Length > 4096)
                            {
                                File.WriteAllBytes(savePath, data);
                            }
                        }
                    }
                }
                catch { }
            });

            var customDef = new CaseModelDefinition
            {
                Id = slug,
                DisplayName = cleanName + " (3D View)",
                GeometryStyle = style,
                LocalAssetFileName = fileName
            };

            var existing = Presets.FindIndex(p => p.Id == slug);
            if (existing >= 0) Presets[existing] = customDef;
            else Presets.Add(customDef);

            return customDef;
        }
    }
}
