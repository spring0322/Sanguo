using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using GameObjects.Influences;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;

namespace TroopDetailPlugin
{
	internal class TroopDetail
	{
		internal PlatformTexture BackgroundTexture;

		internal Point BackgroundSize;

		internal FreeText TroopNameText;

		internal Rectangle PortraitClient;

		internal Troop ShowingTroop;

		internal List<LabelText> LabelTexts;

		internal FreeRichText OtherPersonText;

		internal Rectangle OtherPersonClient;

		internal FreeRichText CombatMethodText;

		internal Rectangle CombatMethodClient;

		internal FreeRichText StuntText;

		internal Rectangle StuntClient;

		internal FreeRichText InfluenceText;

		internal Rectangle InfluenceClient;
        
		private bool isShowing;

		private Point DisplayOffset;

		private Rectangle BackgroundDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.DisplayOffset.X, this.DisplayOffset.Y, this.BackgroundSize.X, this.BackgroundSize.Y);
				return rectangle;
			}
		}

		private Rectangle CombatMethodDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.CombatMethodText.DisplayOffset.X, this.CombatMethodText.DisplayOffset.Y, this.CombatMethodText.ClientWidth, this.CombatMethodText.ClientHeight);
				return rectangle;
			}
		}

		private Rectangle InfluenceDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.InfluenceText.DisplayOffset.X, this.InfluenceText.DisplayOffset.Y, this.InfluenceText.ClientWidth, this.InfluenceText.ClientHeight);
				return rectangle;
			}
		}

		public bool IsShowing
		{
			get
			{
				bool flag = this.isShowing;
				return flag;
			}
			set
			{
				this.isShowing = value;
				bool kind = !value;
				if (kind)
				{
                    kind = Session.MainGame.mainGameScreen.PopUndoneWork().Kind == UndoneWorkKind.SubDialog;
					if (kind)
					{
						Session.MainGame.mainGameScreen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
						Session.MainGame.mainGameScreen.OnMouseLeftDown -= new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
						Session.MainGame.mainGameScreen.OnMouseRightUp -= new Screen.MouseRightUp(this.screen_OnMouseRightUp);
						this.OtherPersonText.Clear();
						this.CombatMethodText.Clear();
						this.StuntText.Clear();
						this.InfluenceText.Clear();
					}
					else
					{
						throw new Exception("The UndoneWork is not a SubDialog.");
					}
				}
				else
				{
					Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.SubDialog, DialogKind.TroopDetail));
					Session.MainGame.mainGameScreen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
					Session.MainGame.mainGameScreen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
					Session.MainGame.mainGameScreen.OnMouseRightUp += new Screen.MouseRightUp(this.screen_OnMouseRightUp);
				}
			}
		}

		private Rectangle OtherPersonDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.OtherPersonText.DisplayOffset.X, this.OtherPersonText.DisplayOffset.Y, this.OtherPersonText.ClientWidth, this.OtherPersonText.ClientHeight);
				return rectangle;
			}
		}

		private Rectangle PortraitDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.PortraitClient.X + this.DisplayOffset.X, this.PortraitClient.Y + this.DisplayOffset.Y, this.PortraitClient.Width, this.PortraitClient.Height);
				return rectangle;
			}
		}

		private Rectangle StuntDisplayPosition
		{
			get
			{
				Rectangle rectangle = new Rectangle(this.StuntText.DisplayOffset.X, this.StuntText.DisplayOffset.Y, this.StuntText.ClientWidth, this.StuntText.ClientHeight);
				return rectangle;
			}
		}

		public TroopDetail()
		{
			this.LabelTexts = new List<LabelText>();
			this.OtherPersonText = new FreeRichText();
			this.CombatMethodText = new FreeRichText();
			this.StuntText = new FreeRichText();
			this.InfluenceText = new FreeRichText();
		}

		internal void Draw()
		{
			bool showingTroop = this.ShowingTroop != null;
			if (showingTroop)
			{
				Rectangle? nullable = null;
				CacheManager.Draw(this.BackgroundTexture, this.BackgroundDisplayPosition, nullable, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
				nullable = null;

				// 绘制原有的小头像 - 1:1 比例
				// 🔥 性能优化：避免在 Draw 循环中分配 Rectangle
				// 日期：2026-02-12
				// 原始尺寸：64x80（4:5 比例）
				// 修改为：64x64（1:1 比例）
				try
				{
					if (this.ShowingTroop.Leader != null)
					{
						// 直接内联计算，避免属性调用和中间变量分配
						int x = this.PortraitClient.X + this.DisplayOffset.X;
						int y = this.PortraitClient.Y + this.DisplayOffset.Y;
						int size = this.PortraitClient.Width;  // 使用宽度作为正方形边长
						
						// 绘制头像（移除测试矩形）
						CacheManager.DrawZhsanAvatar(
							this.ShowingTroop.Leader, 
							new Rectangle(x, y, size, size),  // 1:1 比例
							0.199f, 
							PortraitSize.Small
						);
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"[TroopDetail] 头像绘制失败: {ex.Message}");
				}

				// 暂时禁用立绘显示
				// this.DrawPortraitAndTitle();

				this.TroopNameText.Draw(0.1999f);
				List<LabelText>.Enumerator enumerator = this.LabelTexts.GetEnumerator();
				try
				{
					while (true)
					{
						showingTroop = enumerator.MoveNext();
						if (!showingTroop)
						{
							break;
						}
						LabelText current = enumerator.Current;
						current.Label.Draw(0.1999f);
						current.Text.Draw(0.1999f);
					}
				}
				finally
				{
					enumerator.Dispose();
				}
				this.OtherPersonText.Draw(0.1999f);
				this.CombatMethodText.Draw(0.1999f);
				this.StuntText.Draw(0.1999f);
				this.InfluenceText.Draw(0.1999f);
			}
		}

		private void DrawPortraitAndTitle()
		{
			// 暂时禁用立绘显示，专注于修复原有头像
			// 如果需要立绘功能，可以在原有头像正常显示后再启用
			
			/*
			if (this.ShowingTroop?.Leader == null) return;

			try
			{
				// 获取菜单区域
				Rectangle menuRect = this.BackgroundDisplayPosition;
				
				// 设定尺寸参数 - 使用更小的尺寸避免遮挡
				int portraitSize = 60;     // 头像大小，更小一点
				int titleHeight = 18;      // 下方称号条的高度
				int spacing = 2;           // 头像和称号条之间的间距

				// 计算位置 - 放在原有头像的下方
				Rectangle portraitRect = new Rectangle(
					menuRect.X + 30,       // 与原有头像X位置对齐
					menuRect.Y + 140,      // 在原有头像下方，避免冲突
					portraitSize,
					portraitSize);

				Rectangle titleRect = new Rectangle(
					portraitRect.X,
					portraitRect.Bottom + spacing, // 放在头像正下方
					portraitSize,                  // 宽度和头像一致
					titleHeight);

				// 获取像素纹理用于绘制背景
				var pixelTexture = GetPixelTexture();
				if (pixelTexture != null)
				{
					// 绘制头像背景 (半透明黑底衬托)
					CacheManager.Draw(pixelTexture, portraitRect, null, new Color(0, 0, 0, 180), 0f, Vector2.Zero, SpriteEffects.None, 0.198f);
				}
				
				// 绘制立绘 - 使用Small尺寸
				CacheManager.DrawZhsanAvatar(this.ShowingTroop.Leader, portraitRect, 0.197f, PortraitSize.Small);
				
				// 画头像边框
				DrawBorder(portraitRect, 1, Color.Gray);

				// 绘制下方称号条
				string titleText = GetPersonTitle();
				
				// 背景色：使用势力颜色作为底色
				Color factionColor = this.ShowingTroop.BelongedFaction?.FactionColor ?? Color.DarkGray;
				if (pixelTexture != null)
				{
					CacheManager.Draw(pixelTexture, titleRect, null, factionColor, 0f, Vector2.Zero, SpriteEffects.None, 0.198f);
				}

				// 绘制称号文字
				if (!string.IsNullOrEmpty(titleText))
				{
					// 判定文字颜色 (深底白字，浅底黑字)
					bool isBright = (0.299 * factionColor.R + 0.587 * factionColor.G + 0.114 * factionColor.B) > 150;
					Color textColor = isBright ? Color.Black : Color.White;

					// 计算文字居中坐标
					Vector2 textSize = Session.Current.Font.MeasureString(titleText);
					float scale = 0.8f; // 使用稍小的字体
					if (textSize.X * scale > titleRect.Width - 4)
					{
						scale = (titleRect.Width - 4) / textSize.X;
					}

					Vector2 textPos = new Vector2(
						titleRect.X + (titleRect.Width - textSize.X * scale) / 2,
						titleRect.Y + (titleRect.Height - textSize.Y * scale) / 2);

					// 绘制文字
					CacheManager.DrawString(Session.Current.Font, titleText, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.196f);
				}

				// 画称号条边框
				DrawBorder(titleRect, 1, Color.Gray);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[TroopDetail] DrawPortraitAndTitle 异常: {ex.Message}");
			}
			*/
		}

		private string GetPersonTitle()
		{
			if (this.ShowingTroop?.Leader == null) return "无称号";

			// 优先显示个人称号（非战斗称号）
			foreach (var title in this.ShowingTroop.Leader.Titles)
			{
				if (!title.Kind.Combat) // 非战斗称号就是个人称号
				{
					return title.Name;
				}
			}

			// 如果没有个人称号，显示战斗称号
			foreach (var title in this.ShowingTroop.Leader.Titles)
			{
				if (title.Kind.Combat)
				{
					return title.Name;
				}
			}

			return "无称号";
		}

		private void DrawBorder(Rectangle rect, int thickness, Color color)
		{
			var pixelTexture = GetPixelTexture();
			if (pixelTexture != null)
			{
				// 上边框
				CacheManager.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.195f);
				// 下边框
				CacheManager.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.195f);
				// 左边框
				CacheManager.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.195f);
				// 右边框
				CacheManager.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.195f);
			}
		}

		/// <summary>
		/// 获取像素纹理用于绘制纯色背景和边框
		/// </summary>
		private PlatformTexture GetPixelTexture()
		{
			// 尝试获取现有的纹理文件
			try
			{
				return CacheManager.GetTempTexture(@"Content\Textures\GameComponents\TroopDetail\Data\Background.png");
			}
			catch
			{
				// 如果获取失败，返回null，调用方会处理
				return null;
			}
		}

		internal void Initialize()
		{
			
		}

		private void screen_OnMouseLeftDown(Point position)
		{
			bool currentPageIndex = !StaticMethods.PointInRectangle(position, this.OtherPersonDisplayPosition);
			if (currentPageIndex)
			{
				currentPageIndex = !StaticMethods.PointInRectangle(position, this.CombatMethodDisplayPosition);
				if (currentPageIndex)
				{
					currentPageIndex = !StaticMethods.PointInRectangle(position, this.StuntDisplayPosition);
					if (currentPageIndex)
					{
						currentPageIndex = !StaticMethods.PointInRectangle(position, this.InfluenceDisplayPosition);
						if (!currentPageIndex)
						{
							currentPageIndex = this.InfluenceText.CurrentPageIndex >= this.InfluenceText.PageCount - 1;
							if (currentPageIndex)
							{
								currentPageIndex = this.InfluenceText.CurrentPageIndex != this.InfluenceText.PageCount - 1;
								if (!currentPageIndex)
								{
									this.InfluenceText.FirstPage();
								}
							}
							else
							{
								this.InfluenceText.NextPage();
							}
						}
					}
					else
					{
						currentPageIndex = this.StuntText.CurrentPageIndex >= this.StuntText.PageCount - 1;
						if (currentPageIndex)
						{
							currentPageIndex = this.StuntText.CurrentPageIndex != this.StuntText.PageCount - 1;
							if (!currentPageIndex)
							{
								this.StuntText.FirstPage();
							}
						}
						else
						{
							this.StuntText.NextPage();
						}
					}
				}
				else
				{
					currentPageIndex = this.CombatMethodText.CurrentPageIndex >= this.CombatMethodText.PageCount - 1;
					if (currentPageIndex)
					{
						currentPageIndex = this.CombatMethodText.CurrentPageIndex != this.CombatMethodText.PageCount - 1;
						if (!currentPageIndex)
						{
							this.CombatMethodText.FirstPage();
						}
					}
					else
					{
						this.CombatMethodText.NextPage();
					}
				}
			}
			else
			{
				currentPageIndex = this.OtherPersonText.CurrentPageIndex >= this.OtherPersonText.PageCount - 1;
				if (currentPageIndex)
				{
					currentPageIndex = this.OtherPersonText.CurrentPageIndex != this.OtherPersonText.PageCount - 1;
					if (!currentPageIndex)
					{
						this.OtherPersonText.FirstPage();
					}
				}
				else
				{
					this.OtherPersonText.NextPage();
				}
			}
		}

		private void screen_OnMouseMove(Point position, bool leftDown)
		{
		}

		private void screen_OnMouseRightUp(Point position)
		{
			this.IsShowing = false;
		}

		internal void SetPosition(ShowPosition showPosition)
		{
			Rectangle rectangle = new Rectangle(0, 0, Session.MainGame.mainGameScreen.viewportSize.X, Session.MainGame.mainGameScreen.viewportSize.Y);
			Rectangle centerRectangle = new Rectangle(0, 0, this.BackgroundSize.X, this.BackgroundSize.Y);
			ShowPosition showPosition1 = showPosition;
			switch (showPosition1)
			{
				case ShowPosition.Center:
				{
					centerRectangle = StaticMethods.GetCenterRectangle(rectangle, centerRectangle);
					break;
				}
				case ShowPosition.Top:
				{
					centerRectangle = StaticMethods.GetTopRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.Left:
				{
					centerRectangle = StaticMethods.GetLeftRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.Right:
				{
					centerRectangle = StaticMethods.GetRightRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.Bottom:
				{
					centerRectangle = StaticMethods.GetBottomRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.TopLeft:
				{
					centerRectangle = StaticMethods.GetTopLeftRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.TopRight:
				{
					centerRectangle = StaticMethods.GetTopRightRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.BottomLeft:
				{
					centerRectangle = StaticMethods.GetBottomLeftRectangle(rectangle, centerRectangle);
					break;
				}
                case ShowPosition.BottomRight:
				{
					centerRectangle = StaticMethods.GetBottomRightRectangle(rectangle, centerRectangle);
					break;
				}
			}
			this.DisplayOffset = new Point(centerRectangle.X, centerRectangle.Y);
			this.TroopNameText.DisplayOffset = this.DisplayOffset;
			List<LabelText>.Enumerator enumerator = this.LabelTexts.GetEnumerator();
			try
			{
				while (true)
				{
					bool flag = enumerator.MoveNext();
					if (!flag)
					{
						break;
					}
					LabelText current = enumerator.Current;
					current.Label.DisplayOffset = this.DisplayOffset;
					current.Text.DisplayOffset = this.DisplayOffset;
				}
			}
			finally
			{
				enumerator.Dispose();
			}
			this.OtherPersonText.DisplayOffset = new Point(this.DisplayOffset.X + this.OtherPersonClient.X, this.DisplayOffset.Y + this.OtherPersonClient.Y);
			this.CombatMethodText.DisplayOffset = new Point(this.DisplayOffset.X + this.CombatMethodClient.X, this.DisplayOffset.Y + this.CombatMethodClient.Y);
			this.StuntText.DisplayOffset = new Point(this.DisplayOffset.X + this.StuntClient.X, this.DisplayOffset.Y + this.StuntClient.Y);
			this.InfluenceText.DisplayOffset = new Point(this.DisplayOffset.X + this.InfluenceClient.X, this.DisplayOffset.Y + this.InfluenceClient.Y);
		}

		internal void SetTroop(Troop troop)
		{
			bool leader;
			this.ShowingTroop = troop;
			this.TroopNameText.Text = troop.DisplayName;
			List<LabelText>.Enumerator enumerator = this.LabelTexts.GetEnumerator();
			try
			{
				while (true)
				{
					leader = enumerator.MoveNext();
					if (!leader)
					{
						break;
					}
					LabelText current = enumerator.Current;
					try
					{
						current.Text.Text = StaticMethods.GetPropertyValue(troop, current.PropertyName).ToString();
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"[TroopDetail] 获取属性 {current.PropertyName} 失败: {ex.Message}");
						current.Text.Text = "获取失败";
					}
				}
			}
			finally
			{
				enumerator.Dispose();
			}
			this.OtherPersonText.AddText("其他人物", this.OtherPersonText.TitleColor);
			this.OtherPersonText.AddNewLine();
			int personCount = troop.PersonCount - 1;
			this.OtherPersonText.AddText(string.Concat(personCount.ToString(), "人"), this.OtherPersonText.SubTitleColor);
			this.OtherPersonText.AddNewLine();
			IEnumerator enumerator1 = troop.Persons.GetEnumerator();
			try
			{
				while (true)
				{
					leader = enumerator1.MoveNext();
					if (!leader)
					{
						break;
					}
					Person person = enumerator1.Current is Person ? (Person)enumerator1.Current : null;
                    if (person != null)
                    {
                        leader = person != troop.Leader;
                        if (leader)
                        {
                            this.OtherPersonText.AddText(person.Name);
                            this.OtherPersonText.AddNewLine();
                        }
                    }
				}
			}
			finally
			{
				IDisposable disposable = enumerator1 as IDisposable;
				leader = disposable == null;
				if (!leader)
				{
					disposable.Dispose();
				}
			}
			this.OtherPersonText.ResortTexts();
			this.CombatMethodText.AddText("部队战法", this.CombatMethodText.TitleColor);
			this.CombatMethodText.AddNewLine();
			personCount = troop.CombatMethods.Count;
			this.CombatMethodText.AddText(string.Concat(personCount.ToString(), "种"), this.CombatMethodText.SubTitleColor);
			this.CombatMethodText.AddNewLine();
			Dictionary<int, CombatMethod>.ValueCollection.Enumerator enumerator2 = troop.CombatMethods.CombatMethods.Values.GetEnumerator();
			try
			{
				while (true)
				{
					leader = enumerator2.MoveNext();
					if (!leader)
					{
						break;
					}
					CombatMethod combatMethod = enumerator2.Current;
					this.CombatMethodText.AddText(combatMethod.Name, this.CombatMethodText.SubTitleColor2);
					personCount = combatMethod.Combativity - troop.DecrementOfCombatMethodCombativityConsuming;
					this.CombatMethodText.AddText(string.Concat(" 战意消耗", personCount.ToString()), this.CombatMethodText.SubTitleColor3);
					
					// 🔥 2026-03-18 移除：天气提示应该在选择目标时显示，而不是在详情面板中
					// 原因：天气判断应该基于目标地块，而不是部队当前位置
					// 解决：与计略保持一致，只在选择目标时进行天气检查
					
					this.CombatMethodText.AddNewLine();
				}
			}
			finally
			{
				enumerator2.Dispose();
			}
			this.CombatMethodText.ResortTexts();
			this.StuntText.AddText("部队特技", this.StuntText.TitleColor);
			this.StuntText.AddNewLine();
			personCount = troop.Stunts.Count;
			this.StuntText.AddText(string.Concat(personCount.ToString(), "种"), this.StuntText.SubTitleColor);
			this.StuntText.AddNewLine();
			Dictionary<int, Stunt>.ValueCollection.Enumerator enumerator3 = troop.Stunts.Stunts.Values.GetEnumerator();
			try
			{
				while (true)
				{
					leader = enumerator3.MoveNext();
					if (!leader)
					{
						break;
					}
					Stunt stunt = enumerator3.Current;
					this.StuntText.AddText(stunt.Name, this.StuntText.SubTitleColor2);
					personCount = stunt.Combativity;
					this.StuntText.AddText(string.Concat(" 战意消耗", personCount.ToString()), this.StuntText.SubTitleColor3);
					this.StuntText.AddNewLine();
				}
			}
			finally
			{
				enumerator3.Dispose();
			}
			this.StuntText.ResortTexts();
			this.InfluenceText.AddText("部队特性", this.InfluenceText.TitleColor);
			this.InfluenceText.AddNewLine();
			this.InfluenceText.AddText(this.ShowingTroop.Army.Kind.Name, this.InfluenceText.SubTitleColor);
			this.InfluenceText.AddNewLine();
			Dictionary<int, Influence>.ValueCollection.Enumerator enumerator4 = this.ShowingTroop.Army.Kind.Influences.Influences.Values.GetEnumerator();
			try
			{
				while (true)
				{
					leader = enumerator4.MoveNext();
					if (!leader)
					{
						break;
					}
					Influence influence = enumerator4.Current;
					this.InfluenceText.AddText(influence.Name, this.InfluenceText.SubTitleColor2);
					this.InfluenceText.AddText(influence.Description, this.InfluenceText.SubTitleColor3);
					this.InfluenceText.AddNewLine();
				}
			}
			finally
			{
				enumerator4.Dispose();
			}
			this.InfluenceText.ResortTexts();
		}
	}
}

