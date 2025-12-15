using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameManager;
using WorldOfTheThreeKingdoms.GameScreens; 

namespace WorldOfTheThreeKingdoms
{
/// <summary>
/// 双击菜单渲染器 (裁剪修复版)
/// 解决文字被截断、只显示一个字的问题
/// </summary>
public class DoubleClickMenuManager
{
private static DoubleClickMenuManager _instance;
public static DoubleClickMenuManager Instance
{
get
{
if (_instance == null) _instance = new DoubleClickMenuManager();
return _instance;
}
}

private SpriteBatch _uiSpriteBatch;
private bool _isMenuVisible = false;
private Rectangle _menuBounds;
private List<AdvisorMenuItem> _currentItems;
private List<Rectangle> _itemBounds;
private Texture2D _menuBackground;
private Texture2D _menuItemNormal;

// 反射相关
private bool _hasDiagnosedTextManager = false;
private MethodInfo _drawTextMethod;
private int _hoveredItemIndex = -1;
private double _menuOpenTimestamp;
private const double SAFETY_DELAY_MS = 200;
private const int MENU_WIDTH = 180;
private const int MENU_ITEM_HEIGHT = 32;
private const int MENU_PADDING = 4;
private const int BORDER_THICKNESS = 2;

// 状态备份
private BlendState _prevBlendState;
private DepthStencilState _prevDepthStencilState;
private RasterizerState _prevRasterizerState;
private SamplerState _prevSamplerState;
private Rectangle _prevScissorRect; // [新增] 备份裁剪区域

private DoubleClickMenuManager()
{
_currentItems = new List<AdvisorMenuItem>();
_itemBounds = new List<Rectangle>();
}

public void Initialize()
{
InitializeGraphics();
}

public void ShowMenu(List<AdvisorMenuItem> items, Point position, GameTime gameTime)
{
if (items == null || items.Count == 0) return;

if (!_hasDiagnosedTextManager)
{
LocateTextManager();
_hasDiagnosedTextManager = true;
}

_currentItems = items;
_isMenuVisible = true;

if (gameTime != null)
{
_menuOpenTimestamp = gameTime.TotalGameTime.TotalMilliseconds;
}

CalculateLayout(position);
_hoveredItemIndex = -1;
}

public void CloseMenu()
{
_isMenuVisible = false;
_currentItems.Clear();
_itemBounds.Clear();
}

public void Update(GameTime gameTime)
{
if (!_isMenuVisible) return;

if (gameTime != null)
{
if (gameTime.TotalGameTime.TotalMilliseconds - _menuOpenTimestamp < SAFETY_DELAY_MS) return;
}

var mouseState = Mouse.GetState();
var mousePos = new Point(mouseState.X, mouseState.Y);

UpdateHoverState(mousePos);

if (mouseState.LeftButton == ButtonState.Pressed)
{
if (!_menuBounds.Contains(mousePos))
{
CloseMenu();
}
else if (_hoveredItemIndex != -1)
{
var item = _currentItems[_hoveredItemIndex];
var action = item.OnClick;

// 先关闭菜单，再执行动作，防止新菜单被误关
CloseMenu(); 
try { action?.Invoke(); } catch { }
}
}

if (Keyboard.GetState().IsKeyDown(Keys.Escape)) CloseMenu();
}

private void UpdateHoverState(Point mousePos)
{
_hoveredItemIndex = -1;

if (_menuBounds.Contains(mousePos))
{
for (int i = 0; i < _itemBounds.Count; i++)
{
if (_itemBounds[i].Contains(mousePos))
{
_hoveredItemIndex = i;
break;
}
}
}
}

private void CalculateLayout(Point position)
{
try
{
int itemCount = _currentItems.Count;
int totalHeight = (itemCount * MENU_ITEM_HEIGHT) + (MENU_PADDING * 2);
int totalWidth = MENU_WIDTH + (MENU_PADDING * 2);

// [安全检查] 防止空引用异常
if (Session.MainGame == null || Session.MainGame.GraphicsDevice == null)
{
Console.WriteLine("[MenuUI] GraphicsDevice 为 null，使用默认布局");
// 使用默认布局，不依赖GraphicsDevice
_menuBounds = new Rectangle(position.X, position.Y, totalWidth, totalHeight);
_itemBounds.Clear();
for (int i = 0; i < itemCount; i++)
{
var rect = new Rectangle(
position.X + MENU_PADDING,
position.Y + MENU_PADDING + (i * MENU_ITEM_HEIGHT),
MENU_WIDTH - (MENU_PADDING * 2),
MENU_ITEM_HEIGHT - MENU_PADDING);
_itemBounds.Add(rect);
}
return;
}

var viewport = Session.MainGame.GraphicsDevice.Viewport;
int x = position.X;
int y = position.Y;

if (y + totalHeight > viewport.Height) y -= totalHeight;
if (x + totalWidth > viewport.Width) x -= totalWidth;
if (y < 0) y = 0;
if (x < 0) x = 0;

_menuBounds = new Rectangle(x, y, totalWidth, totalHeight);

_itemBounds.Clear();
for (int i = 0; i < itemCount; i++)
{
var rect = new Rectangle(
x + MENU_PADDING,
y + MENU_PADDING + (i * MENU_ITEM_HEIGHT),
MENU_WIDTH - (MENU_PADDING * 2),
MENU_ITEM_HEIGHT - MENU_PADDING);
_itemBounds.Add(rect);
}
}
catch (Exception ex)
{
Console.WriteLine($"[MenuUI] CalculateLayout 异常: {ex.Message}");
// 异常时使用最简单的布局
_menuBounds = new Rectangle(position.X, position.Y, 200, 100);
_itemBounds.Clear();
}
}

public void Draw(SpriteBatch gameSpriteBatch)
{
if (!_isMenuVisible) return;

// [延迟初始化] 如果图形资源还没准备好，尝试重新初始化
if (_menuBackground == null || _uiSpriteBatch == null)
{
InitializeGraphics();
if (_menuBackground == null || _uiSpriteBatch == null)
{
Console.WriteLine("[MenuUI] 图形资源未就绪，跳过绘制");
return;
}
}

// [安全检查] 防止空引用异常
if (Session.MainGame == null)
{
Console.WriteLine("[MenuUI] Session.MainGame 为 null，跳过绘制");
return;
}

var device = Session.MainGame.GraphicsDevice;
if (device == null)
{
Console.WriteLine("[MenuUI] GraphicsDevice 为 null，跳过绘制");
return;
}

// 1. 保存所有状态 (包括裁剪区域)
SaveRenderState(device);

try
{
// 2. [核心修复] 强制解除裁剪限制！
// 必须在 Begin 之前或之中重置 ScissorRectangle
device.ScissorRectangle = device.Viewport.Bounds;

// 3. 开启绘制
// 使用 Immediate 模式 (兼容 TextManager)
// RasterizerState.CullNone 会禁用裁剪剔除
_uiSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone );

// 背景
_uiSpriteBatch.Draw(_menuBackground, _menuBounds, Color.White);
DrawBorder(_uiSpriteBatch, _menuBounds, Color.Gold, BORDER_THICKNESS);

for (int i = 0; i < _currentItems.Count; i++)
{
var item = _currentItems[i];
var bounds = _itemBounds[i];
var isHover = (i == _hoveredItemIndex);

// 按钮
var btnColor = isHover ? Color.Gold : Color.Gray; 
_uiSpriteBatch.Draw(_menuItemNormal, bounds, btnColor);

// 文字
string text = item.Title ?? "选项";
if (_drawTextMethod != null)
{
Vector2 textPos = new Vector2(bounds.X + 12, bounds.Y + 6);

// [保险] 在调用 TextManager 前再次确认状态
// 因为 TextManager 内部可能也会修改状态
device.ScissorRectangle = device.Viewport.Bounds;

TryInvokeDrawText(_uiSpriteBatch, text, textPos, Color.Black);
}
}
}
catch (Exception ex)
{
Console.WriteLine($"[MenuUI] Draw 异常: {ex.Message}");
}
finally
{
try
{
_uiSpriteBatch.End();
}
catch (Exception ex)
{
Console.WriteLine($"[MenuUI] SpriteBatch.End 异常: {ex.Message}");
}

// 4. 恢复所有状态 (归还给 CacheManager)
try
{
RestoreRenderState(device);
}
catch (Exception ex)
{
Console.WriteLine($"[MenuUI] RestoreRenderState 异常: {ex.Message}");
}
}
}

private void SaveRenderState(GraphicsDevice device)
{
_prevBlendState = device.BlendState;
_prevDepthStencilState = device.DepthStencilState;
_prevRasterizerState = device.RasterizerState;
_prevSamplerState = device.SamplerStates[0];
_prevScissorRect = device.ScissorRectangle; // 备份裁剪区
}

private void RestoreRenderState(GraphicsDevice device)
{
device.BlendState = _prevBlendState;
device.DepthStencilState = _prevDepthStencilState;
device.RasterizerState = _prevRasterizerState;
device.SamplerStates[0] = _prevSamplerState;
device.ScissorRectangle = _prevScissorRect; // 恢复裁剪区
}

private void InitializeGraphics()
{
try
{
// [安全检查] 确保所有必要对象都存在
if (Session.MainGame == null)
{
Console.WriteLine("[MenuUI] Session.MainGame 为 null，跳过图形初始化");
return;
}

var gd = Session.MainGame.GraphicsDevice;
if (gd == null)
{
Console.WriteLine("[MenuUI] GraphicsDevice 为 null，跳过图形初始化");
return;
}

_uiSpriteBatch = new SpriteBatch(gd);

_menuBackground = CreateColorTexture(gd, new Color(20, 20, 20, 240));
_menuItemNormal = CreateColorTexture(gd, Color.White); 

Console.WriteLine("[MenuUI] 图形初始化成功");
}
catch (Exception ex)
{
Console.WriteLine($"[MenuUI] InitializeGraphics 异常: {ex.Message}");
}
}

private void LocateTextManager()
{
try
{
var assembly = Assembly.GetAssembly(typeof(Session));
Type textMgrType = null;

foreach (var t in assembly.GetTypes()) 
{
if (t.Name.Contains("TextManager")) { textMgrType = t; break; }
}

if (textMgrType != null) 
{
var methods = textMgrType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
foreach (var m in methods) 
{
var pars = m.GetParameters();
if (m.Name.Contains("DrawText") && pars.Length >= 2 && pars[0].ParameterType.Name.Contains("SpriteBatch")) 
{
_drawTextMethod = m;
return; 
}
}
}
}
catch {}
}

private void TryInvokeDrawText(SpriteBatch sb, string text, Vector2 pos, Color color)
{
try
{
var parameters = _drawTextMethod.GetParameters();
object[] args = new object[parameters.Length];

for (int i = 0; i < parameters.Length; i++)
{
var pType = parameters[i].ParameterType;
var name = parameters[i].Name.ToLower();

if (pType == typeof(SpriteBatch)) args[i] = sb;
else if (pType == typeof(string)) args[i] = text;
else if (pType == typeof(Vector2)) args[i] = pos;
else if (pType == typeof(float) && name.Contains("x")) args[i] = pos.X;
else if (pType == typeof(float) && name.Contains("y")) args[i] = pos.Y;
else if (pType == typeof(Color)) args[i] = color;
}

_drawTextMethod.Invoke(null, args);
}
catch {}
}

private Texture2D CreateColorTexture(GraphicsDevice device, Color color)
{
var tex = new Texture2D(device, 1, 1);
tex.SetData(new[] { color });
return tex;
}

private void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
{
sb.Draw(_menuItemNormal, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
sb.Draw(_menuItemNormal, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
sb.Draw(_menuItemNormal, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
sb.Draw(_menuItemNormal, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
}
}
}