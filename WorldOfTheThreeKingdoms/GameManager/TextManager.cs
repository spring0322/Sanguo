using Microsoft.Xna.Framework;
using Platforms;
using FontStashSharp;
using FontStashSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Tools;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameManager
{
    public struct FontPair
    {
        public string Name { get; set; }

        public int Size { get; set; }

        public string Style { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    public static class TextManager
    {

        // Proxy to FontManager
        // public static FontSystem fontSystem = null; 
        // public static SpriteFontBase font = null;

        public static Object lockObj = new Object();
        
        // [新增] 防止重复初始化的标志
        private static bool _isInitializing = false;
        private static string _lastInitializedFont = "";

        public static Dictionary<string, Microsoft.Xna.Framework.Graphics.Texture2D> texs = new Dictionary<string, Microsoft.Xna.Framework.Graphics.Texture2D>();

        public static void Init(string name, int size)
        {
            // 🔥 技术性修复：防御式检查 GraphicsDevice
            if (Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[TextManager] GraphicsDevice 为 null 或已释放，跳过 Init");
                return;
            }

            // [新增] 防止重复初始化检查
            lock (lockObj)
            {
                if (_isInitializing)
                {
                    System.Diagnostics.Debug.WriteLine($"[TextManager] 跳过重复初始化请求: {name}");
                    return;
                }
                
                string fontKey = $"{name}_{size}";
                // Check if FontManager has this font
                if (FontManager.Instance.GetFont(size) != null && _lastInitializedFont == fontKey)
                {
                    System.Diagnostics.Debug.WriteLine($"[TextManager] 字体已存在，跳过初始化: {name}");
                    return;
                }
                
                _isInitializing = true;
            }
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TextManager] 开始初始化字体: {name}, 大小: {size}");
                
                var bytes = Platform.Current.LoadFile(name);
                if (bytes == null || bytes.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[TextManager] 字体文件加载失败或为空: {name}");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[TextManager] 字体文件加载成功，大小: {bytes.Length} 字节");
                
                // Use FontManager
                FontManager.Instance.LoadFont(name, size);
                
                string fontKey = $"{name}_{size}";
                _lastInitializedFont = fontKey;
                System.Diagnostics.Debug.WriteLine($"[TextManager] 字体初始化成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TextManager] 字体初始化异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TextManager] 异常堆栈: {ex.StackTrace}");
            }
            finally
            {
                lock (lockObj)
                {
                    _isInitializing = false;
                }
            }
        }

        /// <summary>
        /// 重置字体管理器（用于GPU设备恢复）
        /// </summary>
        public static void Reset()
        {
            lock (lockObj)
            {
                FontManager.Instance.Reset();
                _lastInitializedFont = "";
                _isInitializing = false;
                
                // 清理纹理缓存
                foreach (var tex in texs.Values)
                {
                    if (tex != null && !tex.IsDisposed)
                    {
                        try { tex.Dispose(); } catch { }
                    }
                }
                texs.Clear();
                
                System.Diagnostics.Debug.WriteLine("[TextManager] 字体管理器已重置");
            }
        }

        public static void DrawTexts(string text, FontPair pair, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Color color, int space = 0, float scale = 1f, float? depth = null)
        {
            if (string.IsNullOrEmpty(text)) return;

            // [新增] 安全检查：验证Session及其SpriteBatch
            if (Session.Current == null || Session.Current.SpriteBatch == null)
            {
                // 仅在调试模式下输出，避免刷屏
                System.Diagnostics.Debug.WriteLineIf(false, "[TextManager] Session or SpriteBatch is null, skipping text draw.");
                return;
            }

            // [新增] 验证GraphicsDevice一致性
            // 这有助于检测SpriteBatch是否持有一个过期的、已被重置的GraphicsDevice引用
            if (Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[TextManager] Platform.GraphicsDevice 为 null 或已释放，停止绘制文本");
                return;
            }

            // [新增] 多线程保护：并在AI计算时禁止绘制文本，防止 VertexBuffer NRE
            if (Session.Current.IsWorking) return;

            if (Session.Current.SpriteBatch.GraphicsDevice != Platform.GraphicsDevice)
            {
                System.Diagnostics.Debug.WriteLine($"[TextManager] CRITICAL WARNING: SpriteBatch.GraphicsDevice ID ({Session.Current.SpriteBatch.GraphicsDevice.GetHashCode()}) does not match Platform.GraphicsDevice ID ({Platform.GraphicsDevice.GetHashCode()}). Skipping DRAW to prevent Crash!");
                return; // 这里应该返回，否则底层必然 Crash
            }

            // 初始化字体 (using FontManager)
            if (FontManager.Instance.GetFont(pair.Size) == null)
            {
                Init(pair.Name, pair.Size);
            }
            
            var font = FontManager.Instance.GetFont(pair.Size);
            if (font == null) return;

            // 处理文本
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var texs = text.Split('\n');

            // 绘制每一行
            try
            {
                for (int i = 0; i < texs.Length; i++)
                {
                    var te = texs[i];
                    if (!string.IsNullOrEmpty(te))
                    {
                        var drawPos = pos + new Vector2(0, i * pair.Size * scale);
                        var drawDepth = depth == null ? 0 : (float)depth;
                        // Use FontManager to draw
                        FontManager.Instance.DrawString(Session.Current.SpriteBatch, te, drawPos, color, scale, drawDepth);
                    }
                }
            }
            catch (NullReferenceException ne)
            {
                System.Diagnostics.Debug.WriteLine($"[TextManager] NullReferenceException in DrawTexts: {ne.Message}");
                // 🔧 自愈：发生异常时通知FontManager重置
                FontManager.Instance.Reset();
            }
            catch (Exception ex)
            {
                // 🔥 检测GPU设备移除异常
                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                    ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                // 捕获 SharpDXException 等其他异常
                System.Diagnostics.Debug.WriteLine($"[TextManager] Exception in DrawTexts: {ex.Message}");
                // 🔧 自愈
                FontManager.Instance.Reset();
                FontManager.Instance.Reset();
            }
        }

        public static List<GameManager.Bounds> DrawTextsReturnBounds(string text, FontPair pair, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Color color, int space = 0, float scale = 1f, float? depth = null)
        {
            List<GameManager.Bounds> bounds = new List<GameManager.Bounds>();
            GameManager.Bounds bound;

             // [新增] 安全检查：验证Session及其SpriteBatch
            if (Session.Current == null || Session.Current.SpriteBatch == null)
            {
                return bounds;
            }

            // [新增] 多线程保护：并在AI计算时禁止绘制文本，防止 VertexBuffer NRE
            if (Session.Current.IsWorking) return bounds;

            // 🔥 技术性修复：防御式检查
            if (Session.Current.SpriteBatch == null || Session.Current.SpriteBatch.GraphicsDevice == null || Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
            {
                return bounds;
            }

            if (Session.Current.SpriteBatch.GraphicsDevice != Platform.GraphicsDevice)
            {
                return bounds; // 不一致，跳过
            }

            var font = FontManager.Instance.GetFont(pair.Size);
            if (font == null)
            {
                Init(pair.Name, pair.Size);
                font = FontManager.Instance.GetFont(pair.Size);
            }

            if (font == null) return bounds;

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            var texs = text.Split('\n');

            try 
            {
                for (int i = 0; i < texs.Length; i++)
                {
                    var te = texs[i];

                    // Use FontStashSharp to draw text and calculate bounds
                    var drawPos = pos + new Vector2(0, i * pair.Size * scale);
                    // Use FontManager to draw
                    FontManager.Instance.DrawString(Session.Current.SpriteBatch, te, drawPos, color, scale, depth == null ? 0 : (float)depth);
                    
                    // Calculate bounds
                    var textSize = FontManager.Instance.MeasureString(te, scale);
                    bound = new GameManager.Bounds(drawPos.X, drawPos.Y, drawPos.X + textSize.X, drawPos.Y + textSize.Y);
                    bounds.Add(bound);
                }
            }
            catch (Exception ex)
            {
                // 🔥 检测GPU设备移除异常
                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                    ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                 System.Diagnostics.Debug.WriteLine($"[TextManager] Exception in DrawTextsReturnBounds: {ex.Message}");
                 FontManager.Instance.Reset(); // 🔧 自愈
            }

            return bounds;
        }
        public static List<GameManager.Bounds> DrawTextsReturnBounds(SpriteBatch batch, string text, FontPair pair, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Color color, int space = 0, float scale = 1f, float? depth = null)
        {
            List<GameManager.Bounds> bounds = new List<GameManager.Bounds>();
            GameManager.Bounds bound;
            
            // 🔥 技术性修复：防御式检查
            if (batch == null || batch.GraphicsDevice == null || Platform.GraphicsDevice == null || Platform.GraphicsDevice.IsDisposed)
            {
                return bounds;
            }

            // [新增] 多线程保护：并在AI计算时禁止绘制文本，防止 VertexBuffer NRE
            if (Session.Current.IsWorking) return bounds;

            if (batch.GraphicsDevice != Platform.GraphicsDevice)
            {
                return bounds; // 不一致，跳过
            }
            
            var font = FontManager.Instance.GetFont(pair.Size);
            if (font == null)
            {
                Init(pair.Name, pair.Size);
                font = FontManager.Instance.GetFont(pair.Size);
            }

            if (font == null) return bounds;

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            var texs = text.Split('\n');

            try
            {
                for (int i = 0; i < texs.Length; i++)
                {
                    var te = texs[i];

                    // Use FontStashSharp to draw text and calculate bounds
                    var drawPos = pos + new Vector2(0, i * pair.Size * scale);
                    // Use FontManager to draw
                    FontManager.Instance.DrawString(batch, te, drawPos, color, scale, depth == null ? 0 : (float)depth);
                    
                    // Calculate bounds
                    var textSize = FontManager.Instance.MeasureString(te, scale);
                    bound = new GameManager.Bounds(drawPos.X, drawPos.Y, drawPos.X + textSize.X, drawPos.Y + textSize.Y);
                    bounds.Add(bound);
                }
            }
            catch (Exception ex)
            {
                // 🔥 检测GPU设备移除异常
                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                    ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                System.Diagnostics.Debug.WriteLine($"[TextManager] DrawTextsReturnBounds (batch) 异常: {ex.Message}");
                FontManager.Instance.Reset(); // 🔧 自愈
            }

            return bounds;
        }

        public static List<GameManager.Bounds> CalcTextsBounds(string text, FontPair pair, Microsoft.Xna.Framework.Vector2 pos, int space = 0, float scale = 1f, float? depth = null)
        {
            List<GameManager.Bounds> bounds = new List<GameManager.Bounds>();
            try
            {
                GameManager.Bounds bound;
                var font = FontManager.Instance.GetFont(pair.Size);
                if (font == null)
                {
                    Init(pair.Name, pair.Size);
                    font = FontManager.Instance.GetFont(pair.Size);
                }

                if (font == null) return bounds;

                text = text.Replace("\r\n", "\n").Replace("\r", "\n");

                var texs = text.Split('\n');

                for (int i = 0; i < texs.Length; i++)
                {
                    var te = texs[i];

                    // Use FontManager to calculate bounds
                    var drawPos = pos + new Vector2(0, i * pair.Size * scale);
                    var textSize = FontManager.Instance.MeasureString(te, scale);
                    bound = new GameManager.Bounds(drawPos.X, drawPos.Y, drawPos.X + textSize.X, drawPos.Y + textSize.Y);
                    bounds.Add(bound);
                }
            }
            catch (Exception ex)
            {
                // 🔥 检测GPU设备移除异常
                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                    ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                System.Diagnostics.Debug.WriteLine($"[TextManager] CalcTextsBounds 异常: {ex.Message}");
                FontManager.Instance.Reset(); // 🔧 自愈：发生异常时重置字体
            }

            return bounds;
        }

        /// <summary>
        /// 将文字处理成自动换行的形式
        /// </summary>
        /// <param name="text">要处理的文字</param>
        /// <param name="pair">FontPair</param>
        /// <param name="lineWidth">行宽度</param>
        /// <param name="scale">缩放倍数</param>
        /// <returns>返回经过自动换行处理过的文字</returns>
        public static string HandleAutoWrap(string text, FontPair pair, float lineWidth, float scale)
        {
            try 
            {
                GameManager.Bounds bound;
                string autoWrapText = null;//换行后的文字
                
                var font = FontManager.Instance.GetFont(pair.Size);
                if (font == null)
                {
                    Init(pair.Name, pair.Size);
                    font = FontManager.Instance.GetFont(pair.Size);
                }

                if (font == null) return text; // 初始化失败直接返回原文本

                text = text.Replace("\r\n", "\n").Replace("\r", "\n");

                var texs = text.Split('\n');

                int currentIndex;//指向在一行文字内当前指向的处理到第几个字的索引
                string currentLine;//当前行需判断的文字
                for (int i = 0; i < texs.Length; i++)
                {
                    currentLine = null;
                    var te = texs[i];
                    currentIndex = 0;
                    for (int j = 0; j < te.Length; j++)
                    {

                        currentLine = te.Substring(currentIndex, j - currentIndex + 1);//取出当前索引位置前的所有文字用于判断这些文字是否超过行宽度
                        // Use FontManager to calculate bounds
                        var drawPos = new Vector2(0, i * pair.Size * scale);
                        // Use scale passed in
                        var textSize = FontManager.Instance.MeasureString(currentLine, scale);
                        bound = new GameManager.Bounds(drawPos.X, drawPos.Y, drawPos.X + textSize.X, drawPos.Y + textSize.Y);

                        if (bound.Width > lineWidth)//如果当前这些文字超过行宽的
                        {
                            autoWrapText += (currentLine.Substring(0, currentLine.Length - 1) + '\n');//换行，并将当前行所有文字存入修改后的自动换行变量中
                            currentIndex = j;
                            j--;//当前的字超过行宽度，需要倒回去一个字开始继续处理
                        }
                    }

                    autoWrapText += (currentLine + '\n');//将没有超界的文字加入总文字内
                }
                return autoWrapText;
            }
            catch (Exception ex)
            {
                // 🔥 检测GPU设备移除异常
                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
                    ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                System.Diagnostics.Debug.WriteLine($"[TextManager] HandleAutoWrap 异常: {ex.Message}");
                FontManager.Instance.Reset(); // 🔧 自愈
                return text;
            }
        }
        /*

    //public static void Init(string name)
    //{
    //    FontCollection fonts = new FontCollection();

    //    //string basePath = @"C:\Docs\Projects\Fonts-master\Fonts-master\tests\SixLabors.Fonts.Tests\Fonts\";

    //    var bytes = Platform.Current.LoadFile(name);

    //    using (var ms = new MemoryStream(bytes))
    //    {
    //        font = fonts.Install(ms);
    //    }

    //    //FontFamily font = fonts.Install(basePath + "FZLB_GBK.TTF");
    //}


    public static void DrawTexts(string text, FontPair pair, Microsoft.Xna.Framework.Vector2 pos, Microsoft.Xna.Framework.Color color, int space = 0, float scale = 1f, float? depth = null)
{

    if (font == null)
    {
        Init(pair.Name);
    }

    Microsoft.Xna.Framework.Graphics.Texture2D tex = null;

    lock (lockObj)
    {
        if (texs.ContainsKey(text))
        {
            tex = texs[text];
        }
        else
        {
            tex = RenderText(font, text, pair.Size);
            texs.Add(text, tex);
        }
    }

    if (tex == null)
    {

    }
    else
    {
        Session.Current.SpriteBatch.Draw(tex, pos, null, color, 0f, Microsoft.Xna.Framework.Vector2.Zero, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, depth == null ? 0 : (float)depth);
    }

}


public static Microsoft.Xna.Framework.Graphics.Texture2D RenderText(Font font, string text, int width, int height)
{
    string path = System.IO.Path.GetInvalidFileNameChars().Aggregate(text, (x, c) => x.Replace($"{c}", "-"));
    string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));

    using (Image<Rgba32> img = new Image<Rgba32>(width, height))
    {
        img.Mutate(x => x.Fill(Rgba32.White));

        IPathCollection shapes = SixLabors.Shapes.Temp.TextBuilder.GenerateGlyphs(text, new SixLabors.Primitives.PointF(50f, 4f), new RendererOptions(font, 72));
        img.Mutate(x => x.Fill(Rgba32.Black, shapes));

        using (var ms = new MemoryStream())
        {
            img.SaveAsPng(ms);
            return Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(Platform.GraphicsDevice, ms);
        }

        //Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

        //using (FileStream fs = File.Create(fullPath + ".png"))
        //{
        //    img.SaveAsPng(fs);
        //}
    }
}

public static Microsoft.Xna.Framework.Graphics.Texture2D RenderText(RendererOptions font, string text)
{
    GlyphBuilder builder = new GlyphBuilder();
    TextRenderer renderer = new TextRenderer(builder);
    SixLabors.Primitives.SizeF size = TextMeasurer.Measure(text, font);
    renderer.RenderText(text, font);

    return builder.Paths.SaveImage((int)size.Width + 20, (int)size.Height + 20, font.Font.Name, text + ".png");
}
public static Microsoft.Xna.Framework.Graphics.Texture2D RenderText(FontFamily font, string text, float pointSize = 12)
{
    return RenderText(new RendererOptions(new Font(font, pointSize), 96) { ApplyKerning = true, WrappingWidth = 340 }, text);
}

public static Microsoft.Xna.Framework.Graphics.Texture2D SaveImage(this IEnumerable<IPath> shapes, int width, int height, params string[] path)
{
    path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
    string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));

    using (Image<Rgba32> img = new Image<Rgba32>(width, height))
    {
        img.Mutate(x => x.Fill(Rgba32.Transparent));

        foreach (IPath s in shapes)
        {
            // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
            img.Mutate(x => x.Fill(Rgba32.HotPink, s.Translate(new Vector2(0, 0))));
        }
        // img.Draw(Color.LawnGreen, 1, shape);

        using (var ms = new MemoryStream())
        {
            img.SaveAsPng(ms);
            return Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(Platform.GraphicsDevice, ms);
        }

        // Ensure directory exists
        //Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

        //using (FileStream fs = File.Create(fullPath))
        //{
        //    img.SaveAsPng(fs);
        //}
    }
}

public static Microsoft.Xna.Framework.Graphics.Texture2D SaveImage(this IEnumerable<IPath> shapes, params string[] path)
{
    IPath shape = new ComplexPolygon(shapes.ToArray());
    shape = shape.Translate(shape.Bounds.Location * -1) // touch top left
            .Translate(new Vector2(10)); // move in from top left

    StringBuilder sb = new StringBuilder();
    IEnumerable<ISimplePath> converted = shape.Flatten();
    converted.Aggregate(sb, (s, p) =>
    {
        foreach (Vector2 point in p.Points)
        {
            sb.Append(point.X);
            sb.Append('x');
            sb.Append(point.Y);
            sb.Append(' ');
        }
        s.Append('\n');
        return s;
    });
    string str = sb.ToString();
    shape = new ComplexPolygon(converted.Select(x => new Polygon(new LinearLineSegment(x.Points.ToArray()))).ToArray());

    path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
    string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));
    // pad even amount around shape
    int width = (int)(shape.Bounds.Left + shape.Bounds.Right);
    int height = (int)(shape.Bounds.Top + shape.Bounds.Bottom);
    if (width < 1)
    {
        width = 1;
    }
    if (height < 1)
    {
        height = 1;
    }
    using (Image<Rgba32> img = new Image<Rgba32>(width, height))
    {
        img.Mutate(x => x.Fill(Rgba32.DarkBlue));

        // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
        img.Mutate(x => x.Fill(Rgba32.HotPink, shape));
        // img.Draw(Color.LawnGreen, 1, shape);

        // Ensure directory exists
        using (var ms = new MemoryStream())
        {
            img.SaveAsPng(ms);
            return Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(Platform.GraphicsDevice, ms);
        }
    }
}


private const float SMALLWIDTHSCALE = 0.54f;

private const float LITTLEWIDTHSCALE = 0.48f;

private static char[] SMALLCHARS = new char[] { '，', '。', '、', '“', '”', '"', ' ', '（', '）' };

private static char[] LITTLECHARS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.',
    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P', 'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z', 'X', 'C', 'V', 'B', 'N', 'M',
    ',', '(', ')', '*', '-', ':', ';', '[', ']', '/'};


private static Object LockObject = new object();

private static Dictionary<string, FontFace> FontFaceDics = new Dictionary<string, FontFace>();

private static Dictionary<FontPair, Dictionary<Char, Texture2D>> FontTextureDics = new Dictionary<FontPair, Dictionary<Char, Texture2D>>();

private static FontFace GetFontFace(FontPair pair)
{
    lock (LockObject)
    {
        //檢查字體是否存在
        if (FontFaceDics.ContainsKey(pair.Name))
        {

        }
        else
        {
            var bytes = Platform.Current.LoadFile(pair.Name);

            using (var ms = new MemoryStream(bytes))
            {
                var fontFace = new FontFace(ms);

                FontFaceDics.Add(pair.Name, fontFace);
            }
        }
        return FontFaceDics[pair.Name];
    }
}

private static Dictionary<Char, Texture2D> GetFontDics(FontPair pair)
{
    lock (LockObject)
    {
        //檢查指定字庫是否存在文字
        if (FontTextureDics.ContainsKey(pair))
        {

        }
        else
        {
            var textureDics = new Dictionary<char, Texture2D>();

            FontTextureDics.Add(pair, textureDics);
        }

        return FontTextureDics[pair];
    }
}

public static Vector2 GetWidthHeight(string text, FontPair pair, float scale = 1f)
{
    Vector2 already = Vector2.Zero;

    int pairWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale);

    int pairHeight = Convert.ToInt32(Convert.ToSingle(pair.Height) * scale);

    int SmallWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale * SMALLWIDTHSCALE);

    int LittleWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale * LITTLEWIDTHSCALE);

    Color? tmpColor = null;

    already.Y += pairHeight;

    for (int i = 0; i < text.Length; i++)
    {
        char ch = text[i];

        if (ch == '\n')
        {
            already.X = 0;
            already.Y += pairHeight;
            continue;
        }

        if (ch == '\r')
        {
            continue;
        }

        if (ch == '<')
        {
            int recent = text.IndexOf('>', i);

            if (recent > i)
            {
                var next = text[i + 1];

                if (next == '/')
                {
                    i = recent;
                    tmpColor = null;
                    continue;
                }
                else
                {
                    string co = text.Substring(i + 1, recent - i - 1);
                    var col = GetColor(co);
                    if (col == null)
                    {

                    }
                    else
                    {
                        i = recent;
                        tmpColor = col;
                        continue;
                    }
                }
            }

        }

        Vector2 ext = Vector2.Zero;

        int targetWidth = pairWidth;
        int targetHeight = pairHeight;

        if (SMALLCHARS.Contains(ch))
        {
            targetWidth = SmallWidth;
        }
        else if (LITTLECHARS.Contains(ch))
        {
            targetWidth = LittleWidth;
        }

        already.X += targetWidth;
    }
    return already;
}

private static Texture2D GetCharTexture(FontFace fontFace, Dictionary<Char, Texture2D> texs, FontPair pair, char ch)
{
    lock (LockObject)
    {
        if (texs.ContainsKey(ch))
        {

        }
        else
        {
            var tex = RenderTexture2D(ch, fontFace, pair);
            texs.Add(ch, tex);
        }
        return texs[ch];
    }
}

public static void DrawTexts(string text, FontPair pair, Vector2 pos, Color color, int space = 0, float scale = 1f, float? depth = null)
{
    var fontFace = GetFontFace(pair);

    var texs = GetFontDics(pair);

    if (fontFace == null || texs == null)
    {

    }
    else
    {
        Vector2 already = Vector2.Zero;

        int pairWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale);

        int pairHeight = Convert.ToInt32(Convert.ToSingle(pair.Height) * scale);

        int SmallWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale * SMALLWIDTHSCALE);

        int LittleWidth = Convert.ToInt32(Convert.ToSingle(pair.Width) * scale * LITTLEWIDTHSCALE);

        Color? tmpColor = null;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (ch == '\n')
            {
                already.X = 0;
                already.Y += pairHeight;
                continue;
            }

            if (ch == '\r')
            {
                continue;
            }

            if (ch == '<')
            {
                int recent = text.IndexOf('>', i);

                if (recent > i)
                {
                    var next = text[i + 1];

                    if (next == '/')
                    {
                        i = recent;
                        tmpColor = null;
                        continue;
                    }
                    else
                    {
                        string co = text.Substring(i + 1, recent - i - 1);
                        var col = GetColor(co);
                        if (col == null)
                        {

                        }
                        else
                        {
                            i = recent;
                            tmpColor = col;
                            continue;
                        }
                    }
                }

            }

            Vector2 ext = Vector2.Zero;

            Texture2D tex = GetCharTexture(fontFace, texs, pair, ch);

            int targetWidth = pairWidth;
            int targetHeight = pairHeight;

            if (SMALLCHARS.Contains(ch))
            {
                targetWidth = SmallWidth;
            }
            else if (LITTLECHARS.Contains(ch))
            {
                targetWidth = LittleWidth;  // tex == null ? LittleWidth : Convert.ToInt16(Convert.ToSingle(tex.Width) * scale);
            }

            if (tex == null)
            {

            }
            else
            {
                int width = targetWidth - tex.Width;

                int height = targetHeight - tex.Height;

                ext.X = width / 2;

                ext.Y = height / 2;

                var drawPos = pos + already + ext;

                if (depth == null)
                {
                    Session.Current.SpriteBatch.Draw(tex, drawPos, tmpColor == null ? color : (Color)tmpColor);
                }
                else
                {
                    Session.Current.SpriteBatch.Draw(tex, drawPos, null, tmpColor == null ? color : (Color)tmpColor, 0f, Vector2.Zero, scale, SpriteEffects.None, (float)depth);
                }
            }

            already.X += targetWidth;
        }
    }
}

private static Texture2D RenderTexture2D(char c, FontFace fontFace, FontPair pair)
{
    try
    {
        var surface = RenderSurface(c, fontFace, pair);

        var image = SaveAsImage(surface);

        using (var ms = new MemoryStream())
        {
            image.SaveAsPng(ms);
            return Texture2D.FromStream(Platform.GraphicsDevice, ms);
        }
    }
    catch (Exception ex)
    {
        // 🔥 检测GPU设备移除异常
        if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("DEVICE_REMOVED") || 
            ex.Message.Contains("device is lost") || ex.GetType().Name.Contains("SharpDXException"))
        {
            CacheManager.MarkDeviceLost(ex);
        }
        return null;
    }
}

private static unsafe Surface RenderSurface(char c, FontFace font, FontPair pair)
{
    var glyph = font.GetGlyph(c, pair.Size);
    var surface = new Surface
    {
        Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
        Width = glyph.RenderWidth,
        Height = glyph.RenderHeight,
        Pitch = glyph.RenderWidth
    };

    var stuff = (byte*)surface.Bits;
    for (int i = 0; i < surface.Width * surface.Height; i++)
        *stuff++ = 0;

    glyph.RenderTo(surface);

    return surface;
}

private static Image<Rgba32> SaveAsImage(Surface surface)
{
    int width = surface.Width;
    int height = surface.Height;
    int len = width * height;
    byte[] data = new byte[len];
    Marshal.Copy(surface.Bits, data, 0, len);
    byte[] pixels = new byte[len * 4];

    int index = 0;
    for (int i = 0; i < len; i++)
    {
        byte c = data[i];
        pixels[index++] = c;
        pixels[index++] = c;
        pixels[index++] = c;
        pixels[index++] = c;
    }

    var image = Image.LoadPixelData<Rgba32>(pixels, width, height);
    Marshal.FreeHGlobal(surface.Bits); //Give the memory back!
    return image;
}

private static Color? GetColor(string name)
{
    switch(name)
    {
        case "MediumBlue": return Color.MediumBlue;
        case "MediumOrchid": return Color.MediumOrchid;
        case "MediumPurple": return Color.MediumPurple;
        case "MediumSeaGreen": return Color.MediumSeaGreen;
        case "MediumSlateBlue": return Color.MediumSlateBlue;
        case "MediumSpringGreen": return Color.MediumSpringGreen;
        case "MediumTurquoise": return Color.MediumTurquoise;
        case "MediumVioletRed": return Color.MediumVioletRed;
        case "MonoGameOrange": return Color.MonoGameOrange;
        case "MintCream": return Color.MintCream;
        case "MistyRose": return Color.MistyRose;
        case "Moccasin": return Color.Moccasin;
        case "MediumAquamarine": return Color.MediumAquamarine;
        case "NavajoWhite": return Color.NavajoWhite;
        case "Navy": return Color.Navy;
        case "OldLace": return Color.OldLace;
        case "MidnightBlue": return Color.MidnightBlue;
        case "Maroon": return Color.Maroon;
        case "LightYellow": return Color.LightYellow;
        case "Linen": return Color.Linen;
        case "LawnGreen": return Color.LawnGreen;
        case "LemonChiffon": return Color.LemonChiffon;
        case "LightBlue": return Color.LightBlue;
        case "LightCoral": return Color.LightCoral;
        case "LightCyan": return Color.LightCyan;
        case "LightGoldenrodYellow": return Color.LightGoldenrodYellow;
        case "LightGray": return Color.LightGray;
        case "LightGreen": return Color.LightGreen;
        case "LightPink": return Color.LightPink;
        case "LightSalmon": return Color.LightSalmon;
        case "LightSeaGreen": return Color.LightSeaGreen;
        case "LightSkyBlue": return Color.LightSkyBlue;
        case "LightSlateGray": return Color.LightSlateGray;
        case "LightSteelBlue": return Color.LightSteelBlue;
        case "Olive": return Color.Olive;
        case "Lime": return Color.Lime;
        case "LimeGreen": return Color.LimeGreen;
        case "Magenta": return Color.Magenta;
        case "OliveDrab": return Color.OliveDrab;
        case "PaleGreen": return Color.PaleGreen;
        case "OrangeRed": return Color.OrangeRed;
        case "Silver": return Color.Silver;
        case "SkyBlue": return Color.SkyBlue;
        case "SlateBlue": return Color.SlateBlue;
        case "SlateGray": return Color.SlateGray;
        case "Snow": return Color.Snow;
        case "SpringGreen": return Color.SpringGreen;
        case "SteelBlue": return Color.SteelBlue;
        case "Tan": return Color.Tan;
        case "Teal": return Color.Teal;
        case "Thistle": return Color.Thistle;
        case "Tomato": return Color.Tomato;
        case "Turquoise": return Color.Turquoise;
        case "Violet": return Color.Violet;
        case "Wheat": return Color.Wheat;
        case "White": return Color.White;
        case "WhiteSmoke": return Color.WhiteSmoke;
        case "Yellow": return Color.Yellow;
        case "Sienna": return Color.Sienna;
        case "Orange": return Color.Orange;
        case "SeaShell": return Color.SeaShell;
        case "SandyBrown": return Color.SandyBrown;
        case "Orchid": return Color.Orchid;
        case "PaleGoldenrod": return Color.PaleGoldenrod;
        case "LavenderBlush": return Color.LavenderBlush;
        case "PaleTurquoise": return Color.PaleTurquoise;
        case "PaleVioletRed": return Color.PaleVioletRed;
        case "PapayaWhip": return Color.PapayaWhip;
        case "PeachPuff": return Color.PeachPuff;
        case "Peru": return Color.Peru;
        case "Pink": return Color.Pink;
        case "Plum": return Color.Plum;
        case "PowderBlue": return Color.PowderBlue;
        case "Purple": return Color.Purple;
        case "Red": return Color.Red;
        case "RosyBrown": return Color.RosyBrown;
        case "RoyalBlue": return Color.RoyalBlue;
        case "SaddleBrown": return Color.SaddleBrown;
        case "Salmon": return Color.Salmon;
        case "SeaGreen": return Color.SeaGreen;
        case "Lavender": return Color.Lavender;
        case "HotPink": return Color.HotPink;
        case "Ivory": return Color.Ivory;
        case "DarkGray": return Color.DarkGray;
        case "DarkGoldenrod": return Color.DarkGoldenrod;
        case "DarkCyan": return Color.DarkCyan;
        case "DarkBlue": return Color.DarkBlue;
        case "Cyan": return Color.Cyan;
        case "Crimson": return Color.Crimson;
        case "Cornsilk": return Color.Cornsilk;
        case "Khaki": return Color.Khaki;
        case "Coral": return Color.Coral;
        case "Chocolate": return Color.Chocolate;
        case "Chartreuse": return Color.Chartreuse;
        case "CadetBlue": return Color.CadetBlue;
        case "BurlyWood": return Color.BurlyWood;
        case "Brown": return Color.Brown;
        case "BlueViolet": return Color.BlueViolet;
        case "Blue": return Color.Blue;
        case "BlanchedAlmond": return Color.BlanchedAlmond;
        case "Black": return Color.Black;
        case "Bisque": return Color.Bisque;
        case "Beige": return Color.Beige;
        case "Azure": return Color.Azure;
        case "Aquamarine": return Color.Aquamarine;
        case "Aqua": return Color.Aqua;
        case "AntiqueWhite": return Color.AntiqueWhite;
        case "AliceBlue": return Color.AliceBlue;
        case "Transparent": return Color.Transparent;
        case "TransparentBlack": return Color.TransparentBlack;
        case "DarkGreen": return Color.DarkGreen;
        case "DarkKhaki": return Color.DarkKhaki;
        case "CornflowerBlue": return Color.CornflowerBlue;
        case "DarkOliveGreen": return Color.DarkOliveGreen;
        case "Indigo": return Color.Indigo;
        case "IndianRed": return Color.IndianRed;
        case "YellowGreen": return Color.YellowGreen;
        case "DarkMagenta": return Color.DarkMagenta;
        case "GreenYellow": return Color.GreenYellow;
        case "Green": return Color.Green;
        case "Gray": return Color.Gray;
        case "Goldenrod": return Color.Goldenrod;
        case "Gold": return Color.Gold;
        case "GhostWhite": return Color.GhostWhite;
        case "Gainsboro": return Color.Gainsboro;
        case "Fuchsia": return Color.Fuchsia;
        case "ForestGreen": return Color.ForestGreen;
        case "FloralWhite": return Color.FloralWhite;
        case "Honeydew": return Color.Honeydew;
        case "DodgerBlue": return Color.DodgerBlue;
        case "DimGray": return Color.DimGray;
        case "DeepSkyBlue": return Color.DeepSkyBlue;
        case "DeepPink": return Color.DeepPink;
        case "DarkViolet": return Color.DarkViolet;
        case "DarkTurquoise": return Color.DarkTurquoise;
        case "DarkSlateGray": return Color.DarkSlateGray;
        case "DarkSlateBlue": return Color.DarkSlateBlue;
        case "DarkSeaGreen": return Color.DarkSeaGreen;
        case "DarkSalmon": return Color.DarkSalmon;
        case "DarkRed": return Color.DarkRed;
        case "DarkOrchid": return Color.DarkOrchid;
        case "DarkOrange": return Color.DarkOrange;
        case "Firebrick": return Color.Firebrick;
        default: return null;
    }
}

*/
    }
}
