using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using WinForms = System.Windows.Forms;
using Wpf = System.Windows.Media;
using Wpf3D = System.Windows.Media.Media3D;

namespace DeepBlue
{
    // 方案一 · 3D 翻页动画
    // WPF Viewport3D 真实透视：封面绕书脊（Y 轴）翻起 0→180°，掠过纸页上方后落定。
    // 世界坐标：书脊 x=0；右页 x∈[0.02,1.57]，封面 x∈[0,1.57] z=0.03；
    // 纸页双联 z=0.005；硬壳底板 z=-0.06。相机 (0,0,4.6) FoV 44°。
    public class PageFlip : WinForms.Panel
    {
        private Wpf3D.AxisAngleRotation3D _rot;
        private Wpf.SolidColorBrush _shade;
        private WinForms.Timer _timer;
        private int _t0;
        private int _dur;
        private Action _onDone;

        public PageFlip()
        {
            BackColor = Ui.ForestMist;
            Dock = DockStyle.Fill;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { BuildScene(); }
            catch (Exception) { _rot = null; }
        }

        private void BuildScene()
        {
            double H = 1.11;   // 半高
            double P = 1.57;   // 半宽（单页）
            double Zp = 0.005; // 纸页
            double Zc = 0.03;  // 封面
            double Zb = -0.06; // 底板

            Wpf.SolidColorBrush shell =
                new Wpf.SolidColorBrush(Wpf.Color.FromRgb(11, 54, 36));
            Wpf.SolidColorBrush paper =
                new Wpf.SolidColorBrush(Wpf.Color.FromRgb(251, 248, 241));

            Wpf3D.Model3DGroup group = new Wpf3D.Model3DGroup();
            group.Children.Add(new Wpf3D.AmbientLight(
                new Wpf.Color() { ScA = 1, ScR = 0.78f, ScG = 0.78f, ScB = 0.76f }));
            group.Children.Add(new Wpf3D.DirectionalLight(
                new Wpf.Color() { ScA = 1, ScR = 0.3f, ScG = 0.29f, ScB = 0.26f },
                new Wpf3D.Vector3D(0.08, 0.35, -1)));

            // 硬壳底板（略大一圈）
            group.Children.Add(new Wpf3D.GeometryModel3D(
                Quad(-P - 0.055, P + 0.055, -H - 0.05, H + 0.05, Zb),
                new Wpf3D.DiffuseMaterial(shell)));
            // 左右纸页（书脊留细缝）
            group.Children.Add(new Wpf3D.GeometryModel3D(
                Quad(-P, -0.012, -H, H, Zp), new Wpf3D.DiffuseMaterial(paper)));
            group.Children.Add(new Wpf3D.GeometryModel3D(
                Quad(0.012, P, -H, H, Zp), new Wpf3D.DiffuseMaterial(paper)));

            // 右页动态阴影：封面抬起时纸页自封面投影中渐次显亮（纵深线索）
            _shade = new Wpf.SolidColorBrush(Wpf.Color.FromRgb(6, 30, 19));
            _shade.Opacity = 0;
            group.Children.Add(new Wpf3D.GeometryModel3D(
                Quad(0.012, P, -H, H, Zp + 0.007),
                new Wpf3D.DiffuseMaterial(_shade)));

            // 封面：正面插画金字贴图，背面深绿内面，绕书脊翻起
            _rot = new Wpf3D.AxisAngleRotation3D(new Wpf3D.Vector3D(0, 1, 0), 0);
            Wpf3D.GeometryModel3D cover = new Wpf3D.GeometryModel3D(
                Quad(0, P, -H, H, Zc),
                new Wpf3D.DiffuseMaterial(
                    new Wpf.ImageBrush(CoverTexture()) { Stretch = Wpf.Stretch.Fill }));
            cover.BackMaterial = new Wpf3D.DiffuseMaterial(shell);
            cover.Transform = new Wpf3D.RotateTransform3D(_rot);
            group.Children.Add(cover);

            // 书整体上移 12px，与展开态 BookPanel 书壳位置（y=22 起）对齐
            group.Transform = new Wpf3D.TranslateTransform3D(0, 0.048, 0);

            System.Windows.Controls.Viewport3D viewport =
                new System.Windows.Controls.Viewport3D();
            viewport.Camera = new Wpf3D.PerspectiveCamera(
                new Wpf3D.Point3D(0, 0, 4.6),
                new Wpf3D.Vector3D(0, 0, -1),
                new Wpf3D.Vector3D(0, 1, 0), 44);
            viewport.Children.Add(new Wpf3D.ModelVisual3D() { Content = group });

            ElementHost host = new ElementHost();
            host.Dock = DockStyle.Fill;
            host.BackColorTransparent = true;
            host.Child = viewport;
            Controls.Add(host);
        }

        private static Wpf3D.MeshGeometry3D Quad(
            double x0, double x1, double y0, double y1, double z)
        {
            Wpf3D.MeshGeometry3D m = new Wpf3D.MeshGeometry3D();
            m.Positions.Add(new Wpf3D.Point3D(x0, y0, z));
            m.Positions.Add(new Wpf3D.Point3D(x1, y0, z));
            m.Positions.Add(new Wpf3D.Point3D(x1, y1, z));
            m.Positions.Add(new Wpf3D.Point3D(x0, y1, z));
            m.TextureCoordinates.Add(new System.Windows.Point(0, 1));
            m.TextureCoordinates.Add(new System.Windows.Point(1, 1));
            m.TextureCoordinates.Add(new System.Windows.Point(1, 0));
            m.TextureCoordinates.Add(new System.Windows.Point(0, 0));
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(1); m.TriangleIndices.Add(2);
            m.TriangleIndices.Add(0); m.TriangleIndices.Add(2); m.TriangleIndices.Add(3);
            return m;
        }

        // 3D 贴图：封面四角透明圆角以深林绿垫底，避免翻页途中出现透角
        private static Wpf.Imaging.BitmapImage CoverTexture()
        {
            System.Drawing.Bitmap src = Assets.Cover;
            using (System.Drawing.Bitmap tex = new System.Drawing.Bitmap(
                src.Width, src.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (System.Drawing.Graphics tg =
                System.Drawing.Graphics.FromImage(tex))
            {
                tg.Clear(System.Drawing.Color.FromArgb(11, 54, 36));
                tg.DrawImage(src, 0, 0, src.Width, src.Height);
                return ToBitmapImage(tex);
            }
        }

        private static Wpf.Imaging.BitmapImage ToBitmapImage(System.Drawing.Bitmap bmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                Wpf.Imaging.BitmapImage bi = new Wpf.Imaging.BitmapImage();
                bi.BeginInit();
                bi.CacheOption = Wpf.Imaging.BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
        }

        public void Play(int durationMs, Action onDone)
        {
            _dur = durationMs;
            _onDone = onDone;
            if (_rot == null)
            {
                if (_onDone != null) _onDone();
                return;
            }
            _t0 = Environment.TickCount;
            if (_timer == null)
            {
                _timer = new WinForms.Timer();
                _timer.Interval = 15;
                _timer.Tick += OnTick;
            }
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            double t = (Environment.TickCount - _t0) / (double)_dur;
            if (t >= 1)
            {
                _timer.Stop();
                if (_rot != null) _rot.Angle = 180;
                if (_shade != null) _shade.Opacity = 0;
                Action done = _onDone;
                _onDone = null;
                if (done != null) done();
                return;
            }
            double a = 180 * EaseFlip(t);
            _rot.Angle = a;
            // 封面抬起 → 右页从封影中显亮；越过约 115° 后完全受光
            if (_shade != null)
                _shade.Opacity = 0.34 * Math.Max(0, 1 - a / 115);
        }

        // 硬壳开合手感：快速掀离纸面 → 中段匀速掠过 → 末段长缓收（前重后轻）
        private static double EaseFlip(double t)
        {
            double easeOut = 1 - (1 - t) * (1 - t) * (1 - t);
            double easeIn = t * t * t;
            return 0.68 * easeOut + 0.32 * easeIn;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
