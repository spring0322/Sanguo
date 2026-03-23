using WorldOfTheThreeKingdoms.GameGlobal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GameObjects
{

    public class PropertyComparer : IComparer<GameObject>
    {
        private bool isNumber;
        private string propertyName;
        private bool SmallToBig;
        private int itemID;

        public PropertyComparer(string propertyName, bool isNumber, bool SmallToBig)
        {
            this.propertyName = propertyName;
            this.isNumber = isNumber;
            this.SmallToBig = SmallToBig;
            this.itemID = -1;
        }

        public PropertyComparer(string propertyName, bool isNumber, bool SmallToBig, int itemID)
        {
            this.propertyName = propertyName;
            this.isNumber = isNumber;
            this.SmallToBig = SmallToBig;
            this.itemID = itemID;
        }


        public int Compare(GameObject x, GameObject y)
        {
            if ((x == null) || (y == null))
            {
                return 0;
            }

            if (x.Equals(y))
            {
                return 0;
            }
            int result = 0;

            Object objX, objY;

            if (itemID < 0)
            {
                objX = StaticMethods.GetPropertyValue(x, this.propertyName);
                objY = StaticMethods.GetPropertyValue(y, this.propertyName);
            }
            else
            {
                objX = StaticMethods.GetMethodValue(x, this.propertyName, new object[] { this.itemID });
                objY = StaticMethods.GetMethodValue(y, this.propertyName, new object[] { this.itemID });
            }

            if (this.isNumber)
            {
                //try
                //{
                long longResult = 0;
                if (this.propertyName == "DisplayedAge")
                {
                    if (objX.Equals("--") && objY.Equals("--"))
                    {
                        longResult = 1;
                    }
                    else
                    {
                        long lObjX; long lObjY;
                        if (long.TryParse(objX.ToString(), out lObjX) && long.TryParse(objY.ToString(), out lObjY))
                        {
                            longResult = lObjX - lObjY;

                            if (longResult > 0)
                            {
                                result = 1;
                            }
                            else if (longResult < 0)
                            {
                                result = -1;
                            }
                            else
                            {
                                result = 0;
                            }
                        }
                        else
                        {
                            float fObjX; float fObjY;
                            if (float.TryParse(objX.ToString(), out fObjX) && float.TryParse(objY.ToString(), out fObjY))
                            {
                                result = fObjX.CompareTo(fObjY);
                            }
                            else
                            {
                                result = -1;
                            }
                        }
                    }
                }
                else
                {
                    long lObjX; long lObjY;
                    if (long.TryParse(objX.ToString(), out lObjX) && long.TryParse(objY.ToString(), out lObjY))
                    {
                        longResult = lObjX - lObjY;

                        if (longResult > 0)
                        {
                            result = 1;
                        }
                        else if (longResult < 0)
                        {
                            result = -1;
                        }
                        else
                        {
                            result = 0;
                        }
                    }
                    else
                    {
                        float fObjX; float fObjY;
                        if (float.TryParse(objX.ToString(), out fObjX) && float.TryParse(objY.ToString(), out fObjY))
                        {
                            result = fObjX.CompareTo(fObjY);
                        }
                        else
                        {
                            // 🔥 修复：当两个值都无法解析为数字时，比较字符串而不是总返回 -1
                            // 这确保了比较器的自反性（A == A）和传递性（A < B < C）
                            result = objX.ToString().CompareTo(objY.ToString());
                        }
                    }
                }
                
                //}
                //catch (FormatException)
                //{
                //    try
                //    {
                //        if (Math.Abs(double.Parse(objX.ToString()) - double.Parse(objY.ToString())) < 0.000001) return 0;
                //        result = double.Parse(objX.ToString()) > double.Parse(objY.ToString()) ? 1 : -1;
                //    }
                //    catch (FormatException)
                //    {
                //        result = -1;
                //    }
                //}
            }
            else
            {
                String xStr = objX.ToString();
                String yStr = objY.ToString();
                Match xMatch = RegexPatterns.SlashDatePattern().Match(xStr);
                Match yMatch = RegexPatterns.SlashDatePattern().Match(yStr);

                if (xMatch.Success && yMatch.Success)
                {
                    int xLeft = int.Parse(xMatch.Groups[1].ToString());
                    int xRight = int.Parse(xMatch.Groups[2].ToString());
                    int yLeft = int.Parse(yMatch.Groups[1].ToString());
                    int yRight = int.Parse(yMatch.Groups[2].ToString());
                    result = xRight == yRight ? xLeft - yLeft : xRight - yRight;
                }
                else if (xMatch.Success)
                {
                    result = -1;
                }
                else if (yMatch.Success)
                {
                    result = 1;
                }
                else
                {
                    xMatch = RegexPatterns.DatePattern().Match(xStr);
                    yMatch = RegexPatterns.DatePattern().Match(yStr);
                    if (xMatch.Success && yMatch.Success)
                    {
                        int xYear = int.Parse(xMatch.Groups[1].ToString());
                        int xMonth = int.Parse(xMatch.Groups[2].ToString());
                        int xDay = int.Parse(xMatch.Groups[3].ToString());
                        int yYear = int.Parse(yMatch.Groups[1].ToString());
                        int yMonth = int.Parse(yMatch.Groups[2].ToString());
                        int yDay = int.Parse(yMatch.Groups[3].ToString());
                        if (xYear == yYear)
                        {
                            if (xMonth == yMonth)
                            {
                                result = xDay - yDay;
                            }
                            else
                            {
                                result = xMonth - yMonth;
                            }
                        }
                        else
                        {
                            result = xYear - yYear;
                        }
                    }
                    else
                    {
                        xMatch = RegexPatterns.NumberFirstPattern().Match(xStr);
                        yMatch = RegexPatterns.NumberFirstPattern().Match(yStr);
                        if (xMatch.Success && yMatch.Success)
                        {
                            int xNum = int.Parse(xMatch.Groups[1].ToString());
                            int yNum = int.Parse(yMatch.Groups[1].ToString());
                            result = xNum - yNum;
                            if (result == 0)
                            {
                                result = xStr.CompareTo(yStr);
                            }
                        }
                        else
                        {
                            result = xStr.CompareTo(yStr);
                        }
                    }
                }
            }

            if (!this.SmallToBig)
            {
                result = -result;
            }

            return result;
        }
    }
}

