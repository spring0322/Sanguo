using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameObjects;
using GameGlobal;

namespace GameScreens
{
    /// <summary>
    /// 人员列表界面示例
    /// 展示如何使用军师感知系统进行UI显示
    /// </summary>
    public class PersonListScreen : GameScreen
    {
        private List<Person> displayPersons;
        private Vector2 listStartPosition;
        private int selectedIndex = -1;
        private int scrollOffset = 0;
        private const int VISIBLE_ROWS = 15;
        private const int ROW_HEIGHT = 25;

        // UI资源
        private SpriteFont font;
        private Texture2D backgroundTexture;
        private Texture2D selectionTexture;

        public PersonListScreen()
        {
            this.IsPopup = true;
            this.listStartPosition = new Vector2(50, 100);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            
            // 加载UI资源
            this.font = this.content.Load<SpriteFont>("Fonts/DefaultFont");
            this.backgroundTexture = this.content.Load<Texture2D>("Textures/ListBackground");
            this.selectionTexture = this.content.Load<Texture2D>("Textures/Selection");
            
            // 初始化人员列表
            RefreshPersonList();
        }

        /// <summary>
        /// 刷新人员列表
        /// </summary>
        private void RefreshPersonList()
        {
            try
            {
                displayPersons = new List<Person>();
                
                Faction currentFaction = Session.Current.Scenario.CurrentFaction;
                if (currentFaction?.Persons != null)
                {
                    foreach (Person person in currentFaction.Persons.GetList())
                    {
                        if (person != null && person.Alive)
                        {
                            displayPersons.Add(person);
                        }
                    }
                }

                // 根据军师观测的忠诚度排序
                if (currentFaction?.IsPlayer == true && currentFaction.Advisor != null)
                {
                    SortByAdvisorPerception(currentFaction.Advisor);
                }
                else
                {
                    // 无军师时按真实忠诚度排序
                    displayPersons.Sort((a, b) => b.Loyalty.CompareTo(a.Loyalty));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonListScreen] RefreshPersonList 失败: {ex.Message}");
                displayPersons = new List<Person>();
            }
        }

        /// <summary>
        /// 根据军师感知排序
        /// </summary>
        private void SortByAdvisorPerception(Person advisor)
        {
            displayPersons.Sort((a, b) =>
            {
                int loyaltyA = AdvisorDataHelper.GetObservedValue(advisor, a, a.Loyalty, "Loyalty");
                int loyaltyB = AdvisorDataHelper.GetObservedValue(advisor, b, b.Loyalty, "Loyalty");
                return loyaltyB.CompareTo(loyaltyA); // 降序排列
            });
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            HandleInput();
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            try
            {
                KeyboardState keyboardState = Keyboard.GetState();
                MouseState mouseState = Mouse.GetState();

                // 键盘导航
                if (keyboardState.IsKeyDown(Keys.Up) && selectedIndex > 0)
                {
                    selectedIndex--;
                    AdjustScrollForSelection();
                }
                else if (keyboardState.IsKeyDown(Keys.Down) && selectedIndex < displayPersons.Count - 1)
                {
                    selectedIndex++;
                    AdjustScrollForSelection();
                }

                // 鼠标选择
                HandleMouseSelection(mouseState);

                // 刷新列表
                if (keyboardState.IsKeyDown(Keys.F5))
                {
                    RefreshPersonList();
                }

                // 切换显示模式（调试用）
                if (keyboardState.IsKeyDown(Keys.F1))
                {
                    ToggleDisplayMode();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonListScreen] HandleInput 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理鼠标选择
        /// </summary>
        private void HandleMouseSelection(MouseState mouseState)
        {
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            
            for (int i = 0; i < Math.Min(VISIBLE_ROWS, displayPersons.Count - scrollOffset); i++)
            {
                Rectangle rowBounds = new Rectangle(
                    (int)listStartPosition.X,
                    (int)listStartPosition.Y + 30 + i * ROW_HEIGHT,
                    600,
                    ROW_HEIGHT
                );

                if (rowBounds.Contains(mousePos))
                {
                    selectedIndex = scrollOffset + i;
                    
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        OnPersonSelected(displayPersons[selectedIndex]);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 调整滚动位置
        /// </summary>
        private void AdjustScrollForSelection()
        {
            if (selectedIndex < scrollOffset)
            {
                scrollOffset = selectedIndex;
            }
            else if (selectedIndex >= scrollOffset + VISIBLE_ROWS)
            {
                scrollOffset = selectedIndex - VISIBLE_ROWS + 1;
            }
        }

        /// <summary>
        /// 人员被选中时的处理
        /// </summary>
        private void OnPersonSelected(Person person)
        {
            try
            {
                // 显示详细信息或执行相关操作
                ShowPersonDetails(person);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonListScreen] OnPersonSelected 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示人员详细信息
        /// </summary>
        private void ShowPersonDetails(Person person)
        {
            Faction faction = Session.Current.Scenario.CurrentFaction;
            Person advisor = faction?.Advisor;

            if (faction?.IsPlayer == true && advisor != null)
            {
                // 显示军师观测的详细信息
                var observationData = new
                {
                    ObservedLoyalty = AdvisorDataHelper.GetObservedValue(advisor, person, person.Loyalty, "Loyalty"),
                    LoyaltyDisplay = AdvisorDataHelper.GetLoyaltyString(advisor, person),
                    ObservedIntelligence = AdvisorDataHelper.GetObservedAbility(advisor, person, "Intelligence"),
                    ObservedCommand = AdvisorDataHelper.GetObservedAbility(advisor, person, "Command"),
                    Accuracy = AdvisorDataHelper.GetAccuracyAssessment(advisor)
                };

                System.Diagnostics.Debug.WriteLine($"[详细信息] {person.Name}:");
                System.Diagnostics.Debug.WriteLine($"  军师观测忠诚度: {observationData.ObservedLoyalty} (显示: {observationData.LoyaltyDisplay})");
                System.Diagnostics.Debug.WriteLine($"  军师观测智力: {observationData.ObservedIntelligence}");
                System.Diagnostics.Debug.WriteLine($"  军师观测统率: {observationData.ObservedCommand}");
                System.Diagnostics.Debug.WriteLine($"  军师准确度: {observationData.Accuracy}");
            }
        }

        /// <summary>
        /// 切换显示模式（调试功能）
        /// </summary>
        private void ToggleDisplayMode()
        {
            // 这里可以实现在军师观测模式和真实值模式之间切换
            // 用于调试和对比
        }

        public override void Draw(GameTime gameTime)
        {
            try
            {
                spriteBatch.Begin();

                // 绘制背景
                DrawBackground();

                // 绘制标题和军师信息
                DrawHeader();

                // 绘制列表标题
                PersonListUIHelper.DrawListHeader(
                    new Vector2(listStartPosition.X, listStartPosition.Y + 30),
                    spriteBatch,
                    font
                );

                // 绘制人员列表
                DrawPersonList();

                // 绘制滚动条
                DrawScrollbar();

                // 绘制说明信息
                DrawInstructions();

                spriteBatch.End();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonListScreen] Draw 失败: {ex.Message}");
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// 绘制背景
        /// </summary>
        private void DrawBackground()
        {
            if (backgroundTexture != null)
            {
                Rectangle screenBounds = new Rectangle(0, 0, 
                    GraphicsDevice.Viewport.Width, 
                    GraphicsDevice.Viewport.Height);
                spriteBatch.Draw(backgroundTexture, screenBounds, Color.White * 0.8f);
            }
        }

        /// <summary>
        /// 绘制标题和军师信息
        /// </summary>
        private void DrawHeader()
        {
            // 绘制标题
            string title = "人员列表 (基于军师感知)";
            spriteBatch.DrawString(font, title, listStartPosition, Color.White);

            // 绘制军师信息
            PersonListUIHelper.DrawAdvisorInfo(
                new Vector2(listStartPosition.X + 300, listStartPosition.Y),
                spriteBatch,
                font
            );
        }

        /// <summary>
        /// 绘制人员列表
        /// </summary>
        private void DrawPersonList()
        {
            for (int i = 0; i < Math.Min(VISIBLE_ROWS, displayPersons.Count - scrollOffset); i++)
            {
                int personIndex = scrollOffset + i;
                Person person = displayPersons[personIndex];

                Vector2 rowPosition = new Vector2(
                    listStartPosition.X,
                    listStartPosition.Y + 55 + i * ROW_HEIGHT
                );

                Rectangle rowBounds = new Rectangle(
                    (int)rowPosition.X,
                    (int)rowPosition.Y,
                    600,
                    ROW_HEIGHT
                );

                // 绘制选中背景
                if (personIndex == selectedIndex && selectionTexture != null)
                {
                    spriteBatch.Draw(selectionTexture, rowBounds, Color.Blue * 0.3f);
                }

                // 绘制人员行
                PersonListUIHelper.DrawPersonRowWithBackground(
                    person,
                    rowBounds,
                    spriteBatch,
                    font,
                    backgroundTexture
                );
            }
        }

        /// <summary>
        /// 绘制滚动条
        /// </summary>
        private void DrawScrollbar()
        {
            if (displayPersons.Count > VISIBLE_ROWS)
            {
                int scrollbarX = (int)listStartPosition.X + 620;
                int scrollbarY = (int)listStartPosition.Y + 55;
                int scrollbarHeight = VISIBLE_ROWS * ROW_HEIGHT;

                // 滚动条背景
                Rectangle scrollbarBg = new Rectangle(scrollbarX, scrollbarY, 10, scrollbarHeight);
                spriteBatch.Draw(backgroundTexture, scrollbarBg, Color.Gray * 0.5f);

                // 滚动条滑块
                float thumbHeight = (float)VISIBLE_ROWS / displayPersons.Count * scrollbarHeight;
                float thumbY = (float)scrollOffset / displayPersons.Count * scrollbarHeight;
                
                Rectangle thumb = new Rectangle(
                    scrollbarX,
                    scrollbarY + (int)thumbY,
                    10,
                    Math.Max(10, (int)thumbHeight)
                );
                spriteBatch.Draw(backgroundTexture, thumb, Color.White * 0.8f);
            }
        }

        /// <summary>
        /// 绘制操作说明
        /// </summary>
        private void DrawInstructions()
        {
            Vector2 instructionPos = new Vector2(listStartPosition.X, listStartPosition.Y + 55 + VISIBLE_ROWS * ROW_HEIGHT + 20);
            
            string[] instructions = {
                "操作说明:",
                "↑↓ - 选择人员",
                "鼠标点击 - 选择并查看详情",
                "F5 - 刷新列表",
                "F1 - 切换显示模式 (调试)"
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                Color color = i == 0 ? Color.Yellow : Color.LightGray;
                spriteBatch.DrawString(font, instructions[i], 
                    new Vector2(instructionPos.X, instructionPos.Y + i * 15), color);
            }
        }
    }
}